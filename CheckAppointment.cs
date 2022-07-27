using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ParseMe
{
    public class Function1
    {
        [FunctionName("CheckAppointment")]
        public void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
