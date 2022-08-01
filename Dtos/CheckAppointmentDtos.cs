using Azure;
using Microsoft.Azure.Cosmos.Table;
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

    public class CheckDto : ITableEntity
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }
        [JsonProperty("cityCode")]
        public string CityCode { get; set; }
        [JsonProperty("personCount")]
        public int PersonCount { get; set; } = 1;
        [JsonProperty("maxDays")]
        public int MaxDays { get; set; } = 45;
        [JsonProperty("notificationMail")]
        public string NotificationMail { get; set; }
        [JsonProperty("expireDate")]
        public DateTime ExpireDate { get; set; }
        // TODO : This quota will be reduced on each email sent
        [JsonProperty("mailQuota")]
        public int MailQuota { get; set; }
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        DateTimeOffset ITableEntity.Timestamp { get; set; }
        string ITableEntity.ETag { get; set; }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            UserId = properties["userId"].StringValue;
            CityCode = properties["cityCode"].StringValue;
            PersonCount = properties["personCount"].Int32Value ?? 1;
            MaxDays = properties["maxDays"].Int32Value ?? 45;
            NotificationMail = properties["notificationMail"].StringValue;
            ExpireDate = properties["expireDate"].DateTime ?? DateTime.Now.AddDays(30);
            MailQuota = properties["mailQuota"].Int32Value ?? 10;
            Enabled = properties["enabled"].BooleanValue ?? true;
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            throw new NotImplementedException();
        }
    }
}
