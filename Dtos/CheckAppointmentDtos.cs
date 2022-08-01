using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParseMe.Dtos
{
    public class IndResponseDto
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("data")]
        public IEnumerable<AppointmentDto> Data { get; set; }
    }

    public class AppointmentDto
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("date")]
        public DateTime Date { get; set; }
        [JsonProperty("startTime")]
        public TimeSpan StartTime { get; set; }
        [JsonProperty("endTime")]
        public TimeSpan Endtime { get; set; }
        [JsonProperty("parts")]
        public int Parts { get; set; }
    }

    public class CheckDto
    {
        public string UserId { get; set; }
        public string CityCode { get; set; }
        public int PersonCount { get; set; } = 1;
        public int MaxDays { get; set; } = 45;
        public string NotificationMail { get; set; }
        public DateTime ExpireDate { get; set; }
        public int MailQuota { get; set; }
    }
}
