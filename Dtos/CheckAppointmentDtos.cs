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
}
