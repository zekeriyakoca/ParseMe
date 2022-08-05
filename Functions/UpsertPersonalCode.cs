using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using ParseMe.Binders;
using ParseMe.Dtos;
using SendGrid.Helpers.Mail;

namespace ParseMe.Functions
{
    public class UpsertPersonalCode
    {
        private readonly ILogger<UpsertPersonalCode> _logger;

        public UpsertPersonalCode(ILogger<UpsertPersonalCode> log)
        {
            _logger = log;
        }

        [FunctionName("UpsertPersonalCode")]
        [OpenApiOperation(operationId: "Run")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpsertAppointmentRequestDto), Description = "Model", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "BadRequestResult response without body")]
        public async Task<IActionResult> Run(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           [Table(tableName: "IndPersonalCodes", Connection = "AzureStorageConnectionString")] CloudTable personalCodesTable,
           [SendGrid(ApiKey = "CustomSendGridKeyAppSettingName")] IAsyncCollector<SendGridMessage> messageCollector
        )
        {
            _logger.LogInformation("UpsertPersonalCode function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonConvert.DeserializeObject<UpsertPersonalCodeRequestDto>(requestBody);

            if (dto == default)
            {
                _logger.LogError($"Request body couldn't be serialized properly! Email : {requestBody}");
                return new BadRequestResult();
            }
            if (!dto.IsValid())
            {
                _logger.LogWarning($"Request body couldn't be validated! Body : {requestBody}");
                return new BadRequestObjectResult("Body couldn't be validated! Please check your data on request body.");
            }
            var personalCodeDto = dto.ToPersonalCodeDto();
            var updatedRecord = await UpsertRecordAsync(personalCodesTable, personalCodeDto);

            _logger.LogInformation($"Record upserted properly. RowKey : {updatedRecord.RowKey}");

            var message = GenerateMessage(personalCodeDto);

            await messageCollector.AddAsync(message);
            _logger.LogInformation($"Invitation sent to {dto.Email}");

            return new OkObjectResult("Success");
        }

        private async Task<PersonalCodeDto> UpsertRecordAsync(CloudTable table, PersonalCodeDto dto)
        {
            try
            {
                await table.CreateIfNotExistsAsync();

                var mailFilter = TableQuery.GenerateFilterCondition("Email", QueryComparisons.Equal, dto.Email);
                var query = new TableQuery<PersonalCodeDto>().Where(mailFilter);

                var currentRecord = (await table.ExecuteQuerySegmentedAsync<PersonalCodeDto>(query, default)).FirstOrDefault();

                //If we have an existing record with same mail. We're setting values to override it.
                if (currentRecord != null)
                {
                    _logger.LogInformation($"We've found and existing error with email ({dto.Email}). Record will be overridden");
                    dto.PartitionKey = currentRecord.PartitionKey;
                    dto.RowKey = currentRecord.RowKey;
                }

                await table.ExecuteAsync(TableOperation.InsertOrMerge(dto));
                _logger.LogInformation($"A personal code record upserted with email :{dto.Email}");

            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while upserting a personal code record!", ex);
                throw;
            }

            return dto;
        }


        private SendGridMessage GenerateMessage(PersonalCodeDto dto)
        {
            var mailBody = GenerateMailBody(dto);

            var message = new SendGridMessage();
            message.AddTo(dto.Email);
            message.AddContent("text/html", mailBody);
            message.SetFrom(new EmailAddress(Environment.GetEnvironmentVariable("EmailFrom")));
            message.SetSubject("Invitation for Ind Appointment Service!");
            return message;
        }

        private string GenerateMailBody(PersonalCodeDto dto)
        {
            return $"You have been invited to use Ind Appointment Service. Please click the following link to access service: {GetAccessLink(dto.Code)}. This is a non-profit service!";
        }
        private string GetAccessLink(string code)
        {
            return String.Format(Environment.GetEnvironmentVariable("AccessLinkTemplate"), code);
        }
    }
}

