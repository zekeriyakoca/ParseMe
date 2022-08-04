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
        public async Task<IActionResult> Run(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           [Table(tableName: "IndRecords", Connection = "AzureStorageConnectionString")] CloudTable cloudTable
        )
        {
            _logger.LogInformation("UpsertAppointment function processed a request.");

            string personalCode = req.Query["personalCode"]; // TODO : Implement personalCode validation

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var dto = JsonConvert.DeserializeObject<UpsertAppointmentRequestDto>(requestBody);

            if (dto == default)
            {
                _logger.LogError($"Request body couldn't be serialized properly! Body : {requestBody}");
                return new BadRequestResult();
            }

            var updatedRecord = await UpsertRecordAsync(cloudTable, dto.ToCheckDto());

            if (updatedRecord == default)
            {
                _logger.LogError($"Model in request body couldn't be validated! Body : {requestBody}");
                return new BadRequestObjectResult("Please check your parameters. Parameters couldn't be validated.");
            }

            _logger.LogInformation($"Record upserted properly. RowKey : {updatedRecord.RowKey}");

            return new OkObjectResult("Success");
        }

        private async Task<CheckDto> UpsertRecordAsync(CloudTable table, CheckDto dto)
        {
            if (!dto.Validate())
            {
                return null;
            }

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

