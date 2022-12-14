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

namespace ParseMe.Functions
{
    public class UpsertAppointment
    {
        private readonly ILogger<UpsertAppointment> _logger;

        public UpsertAppointment(ILogger<UpsertAppointment> log)
        {
            _logger = log;
        }

        [FunctionName("UpsertAppointment")]
        [OpenApiOperation(operationId: "Run")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "personalCode", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "Personal Code provided by authorized user")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(UpsertAppointmentRequestDto), Description = "Model", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "BadRequestResult response with body")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = "BadRequestResult response without body")]
        public async Task<IActionResult> Run(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           [Table(tableName: "IndRecords", Connection = "AzureStorageConnectionString")] CloudTable indRecordsTable,
           [Table(tableName: "IndPersonalCodes", Connection = "AzureStorageConnectionString")] CloudTable personalCodesTable
        )
        {
            _logger.LogInformation("UpsertAppointment function processed a request.");

            string personalCode = req.Query["personalCode"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonConvert.DeserializeObject<UpsertAppointmentRequestDto>(requestBody);

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
            if (!await ValidateCodeAsync(dto, personalCode, personalCodesTable))
            {
                _logger.LogError($"Mail address and personal Code couldn't be validated! Email : {dto.NotificationMail}, Personal Code : {personalCode}");
                return new UnauthorizedResult();
            }

            var updatedRecord = await UpsertRecordAsync(indRecordsTable, dto.ToCheckDto());

            _logger.LogInformation($"Record upserted properly. RowKey : {updatedRecord.RowKey}");

            return new OkObjectResult("Success");
        }

        private async Task<bool> ValidateCodeAsync(UpsertAppointmentRequestDto dto, string personalCode, CloudTable table)
        {
            if (String.IsNullOrWhiteSpace(personalCode))
                return false;

            await table.CreateIfNotExistsAsync();
            var mailFilter = TableQuery.GenerateFilterCondition("Code", QueryComparisons.Equal, personalCode);
            var query = new TableQuery<PersonalCodeDto>().Where(mailFilter);

            var codeRecord = (await table.ExecuteQuerySegmentedAsync<PersonalCodeDto>(query, default)).FirstOrDefault();
            if (codeRecord == null)
            {
                return false;
            }
            if (codeRecord.Email != dto.NotificationMail && codeRecord.Email != Constants.AdminCode)
            {
                return false;
            }
            if (codeRecord.ExpireDate <= DateTime.Now)
            {
                await table.ExecuteAsync(TableOperation.Delete(codeRecord));
                return false;
            }
            return true;
        }

        private async Task<CheckDto> UpsertRecordAsync(CloudTable table, CheckDto dto)
        {
            try
            {
                await table.CreateIfNotExistsAsync();

                var mailFilter = TableQuery.GenerateFilterCondition("NotificationMail", QueryComparisons.Equal, dto.NotificationMail);
                var query = new TableQuery<CheckDto>().Where(mailFilter);

                var currentRecord = (await table.ExecuteQuerySegmentedAsync<CheckDto>(query, default)).FirstOrDefault();

                //If we have an existing record with same mail. We're setting values to override it.
                if (currentRecord != null)
                {
                    _logger.LogInformation($"We've found and existing error with email ({dto.NotificationMail}). Record will be overridden");
                    dto.PartitionKey = currentRecord.PartitionKey;
                    dto.RowKey = currentRecord.RowKey;
                }

                //var query = new TableQuery<CheckDto>();
                //var partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "IndAppointmentRequest");
                //var idFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, dto.RowKey);
                //query = query.Where(TableQuery.CombineFilters(partitionFilter, TableOperators.And, idFilter));

                //var currentRecord = (await table.ExecuteQuerySegmentedAsync<CheckDto>(query, default)).SingleOrDefault();

                await table.ExecuteAsync(TableOperation.InsertOrMerge(dto));
                _logger.LogInformation($"A record upserted with email :{dto.NotificationMail}");

            }
            catch (Exception ex)
            {
                _logger.LogError("Error occured while upserting a record!", ex);
                throw;
            }

            return dto;

        }
    }
}

