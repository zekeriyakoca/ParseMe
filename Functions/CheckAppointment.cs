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

namespace ParseMe
{
    public class CheckAppointment
    {
        private HttpClient client { get; } // TODO : If interval reduce, concider making client static and initialize in static constructer

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
                [SendGrid(ApiKey = "CustomSendGridKeyAppSettingName")] IAsyncCollector<SendGridMessage> messageCollector,
                ILogger log
            )
        {
            log.LogInformation($"CheckAppointment function executed at: {DateTime.Now}");
            var cleanText = "";
            try
            {
                var response = await client.GetAsync(BuildUrl());
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

            var dto = JsonConvert.DeserializeObject<IndResponseDto>(cleanText);
            var maxDays = Environment.GetEnvironmentVariable("MaxDays");
            if (String.IsNullOrWhiteSpace(maxDays))
                maxDays = "45";
            var foundAppointment = FindClosestAppointmentWithin(dto?.Data, int.Parse(maxDays));
            if (foundAppointment == default)
            {
                log.LogInformation($"No appointment found!");
                return;
            }
            log.LogInformation($"Appointment found! {foundAppointment.Date.ToShortDateString()}");
            var message = GenerateMessage(foundAppointment);
            await messageCollector.AddAsync(message);
            log.LogInformation($"Notification sent! {foundAppointment.Date.ToShortDateString()}");
        }

        private string BuildUrl(string localtion = "DH", int personCount = 1)
        {
            string urlPattern = "https://oap.ind.nl/oap/api/desks/{0}/slots/?productKey=DOC&persons={1}";
            return String.Format(urlPattern, localtion, personCount);
        }

        private AppointmentDto FindClosestAppointmentWithin(IEnumerable<AppointmentDto> appointments, int maxDays = 30)
        {
            if (appointments == default && appointments.Count() == 0)
                return default;
            return appointments.Where(a => a.Date <= DateTime.Now.AddDays(maxDays)).OrderBy(a => a.Date).FirstOrDefault();
        }

        private SendGridMessage GenerateMessage(AppointmentDto foundAppointment)
        {
            var mailBody = GenerateMailBody(foundAppointment);

            var message = new SendGridMessage();
            message.AddTo(Environment.GetEnvironmentVariable("EmailTo"));
            message.AddContent("text/html", mailBody);
            message.SetFrom(new EmailAddress(Environment.GetEnvironmentVariable("EmailFrom")));
            message.SetSubject("New Appointment Alert!");
            return message;
        }

        private string GenerateMailBody(AppointmentDto appointment)
        {
            return $"Appointment found at {appointment.Date.ToString("dd/MMM/yyyy")} (after {appointment.Date.Subtract(DateTime.Now).Days} days)";
        }
    }

}
