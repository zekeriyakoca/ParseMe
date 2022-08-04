using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ParseMe.Dtos;
using SendGrid.Helpers.Mail;
using Microsoft.Azure.Cosmos.Table;

namespace ParseMe
{
    public class CheckAppointment
    {
        private HttpClient client { get; } // TODO : If interval reduce, concider making client static and initialize in static constructer
        private ILogger log { get; set; }
        public CheckAppointment()
        {
            client = new HttpClient();
        }
        /// <summary>
        /// Check appotintments from IND webservice and send notification in case of new appointment slot
        /// wihtin defined interval of day
        /// </summary>
        [FunctionName("CheckAppointment")]
        public async Task Run(
                [TimerTrigger("0 */10 * * * *", RunOnStartup = true)] TimerInfo myTimer,
                [Table(tableName: "IndRecords", Connection = "AzureStorageConnectionString")] CloudTable cloudTable,
                [SendGrid(ApiKey = "CustomSendGridKeyAppSettingName")] IAsyncCollector<SendGridMessage> messageCollector,
                ILogger log
            )
        {
            this.log = log;
            log.LogInformation($"CheckAppointment function executed at: {DateTime.Now}");

            var records = await GetCheckRecordsAsync(cloudTable);

            foreach (var record in records)
            {
                await FindAndNotifyAppointmentAsync(record, async (message) =>
                {
                    await messageCollector.AddAsync(message);
                    log.LogInformation($"Notification sent! Content : {message.Contents.FirstOrDefault().Value}");
                });
            }

            log.LogInformation($"CheckAppointment function execution finnished at: {DateTime.Now}");
        }

        private async Task<IEnumerable<CheckDto>> GetCheckRecordsAsync(CloudTable table)
        {
            await table.CreateIfNotExistsAsync();
            var query = new TableQuery<CheckDto>();

            var maxRecordsAtaTime = Environment.GetEnvironmentVariable("MaxRecordSlice");
            if (String.IsNullOrWhiteSpace(maxRecordsAtaTime))
                maxRecordsAtaTime = "30";

            var partitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Constants.PartitionKey);
            var enabledFilter = TableQuery.GenerateFilterConditionForBool("enabled", QueryComparisons.Equal, true);
            query = query.Where(TableQuery.CombineFilters(partitionFilter, TableOperators.And, enabledFilter))
                         .Take(int.Parse(maxRecordsAtaTime));

            var resp = await table.ExecuteQuerySegmentedAsync<CheckDto>(query, default);

            log.LogInformation($"{resp.Results.Count} record/s found to check");

            (var validResults, var invalidResults) = GetValidatedResults(resp.Results);

            if (invalidResults.Count > 0)
            {
                log.LogInformation($"{invalidResults} expired record/s found! They're being deleted");
                await DeleteRecordsAsync(table, invalidResults);
            }
            return validResults.AsEnumerable();
        }

        private (List<CheckDto>, List<CheckDto>) GetValidatedResults(List<CheckDto> results)
        {
            var expiredResults = results.Where(r => r.ExpireDate <= DateTime.Now || r.MailQuota < 1).ToList();

            return (valid: results.Except(expiredResults).ToList(), invalid: expiredResults);
        }

        private async Task DeleteRecordsAsync(CloudTable table, List<CheckDto> results)
        {
            var tableOperations = new TableBatchOperation();
            foreach (var item in results)
            {
                tableOperations.Add(TableOperation.Delete(item));
            }
            await table.ExecuteBatchAsync(tableOperations);
        }

        private async Task FindAndNotifyAppointmentAsync(CheckDto checkDto, Func<SendGridMessage, Task> sendMessageAction)
        {
            var cleanText = "";
            try
            {
                var response = await client.GetAsync(BuildUrl(checkDto.CityCode, checkDto.ProductKey, checkDto.PersonCount));
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                cleanText = result.Replace(")]}',\n", String.Empty); // Remove dirty string within response
            }
            catch (Exception ex)
            {
                log.LogError($"Error occured while calling IND Service", ex);
                throw;
            }

            if (String.IsNullOrWhiteSpace(cleanText))
            {
                log.LogError($"Response of IND Service is empty!");
                throw new Exception("Bad formed response!");
            }

            var responseDto = JsonConvert.DeserializeObject<IndResponseDto>(cleanText);
            var foundAppointment = FindClosestAppointmentWithin(responseDto?.Data, checkDto.MaxDays);
            if (foundAppointment == default)
            {
                log.LogInformation($"No appointment found!");
                return;
            }
            log.LogInformation($"Appointment found! {foundAppointment.Date.ToShortDateString()}");
            var message = GenerateMessage(foundAppointment, checkDto.NotificationMail, checkDto.CityCode);
            await sendMessageAction(message);
        }

        private string BuildUrl(string localtion = "DH", string productKey = "DOC", int personCount = 1)
        {
            string urlPattern = "https://oap.ind.nl/oap/api/desks/{0}/slots/?productKey={1}&persons={2}";
            return String.Format(urlPattern, localtion, productKey, personCount);
        }

        private AppointmentDto FindClosestAppointmentWithin(IEnumerable<AppointmentDto> appointments, int maxDays = 30)
        {
            if (appointments == default && appointments.Count() == 0)
                return default;
            return appointments.Where(a => a.Date <= DateTime.Now.AddDays(maxDays)).OrderBy(a => a.Date).FirstOrDefault();
        }

        private SendGridMessage GenerateMessage(AppointmentDto foundAppointment, string emailTo, string cityCode)
        {
            var mailBody = GenerateMailBody(foundAppointment, cityCode);

            var message = new SendGridMessage();
            message.AddTo(emailTo);
            message.AddContent("text/html", mailBody);
            message.SetFrom(new EmailAddress(Environment.GetEnvironmentVariable("EmailFrom")));
            message.SetSubject("New Appointment Alert!");
            return message;
        }

        private string GenerateMailBody(AppointmentDto appointment, string cityCode)
        {
            return $"Appointment found at {appointment.Date.ToString("dd/MMM/yyyy")} (after {appointment.Date.Subtract(DateTime.Now).Days} days) [City Code : {cityCode}]";
        }
    }

}
