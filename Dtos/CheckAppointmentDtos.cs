using Azure;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using ParseMe.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParseMe.Dtos
{
    public class IndResponseDto
    {
        [JsonProperty("Status")]
        public string Status { get; set; }
        [JsonProperty("Data")]
        public IEnumerable<AppointmentDto> Data { get; set; }
    }

    public class AppointmentDto
    {
        [JsonProperty("Key")]
        public string Key { get; set; }
        [JsonProperty("Date")]
        public DateTime Date { get; set; }
        [JsonProperty("StartTime")]
        public TimeSpan StartTime { get; set; }
        [JsonProperty("EndTime")]
        public TimeSpan Endtime { get; set; }
        [JsonProperty("Parts")]
        public int Parts { get; set; }
    }

    public class CheckDto : ITableEntity
    {
        [JsonProperty("UserId")]
        public string UserId { get; set; }
        [JsonProperty("CityCode")]
        public string CityCode { get; set; }
        [JsonProperty("ProductKey")]
        public string ProductKey { get; set; }
        [JsonProperty("PersonCount")]
        public int PersonCount { get; set; } = 1;
        [JsonProperty("MaxDays")]
        public int MaxDays { get; set; } = 45;
        [JsonProperty("NotificationMail")]
        public string NotificationMail { get; set; }
        [JsonProperty("ExpireDate")]
        public DateTime ExpireDate { get; set; }
        // TODO : This quota will be reduced on each email sent
        [JsonProperty("MailQuota")]
        public int MailQuota { get; set; }
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; } = true;

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        DateTimeOffset ITableEntity.Timestamp { get; set; }
        string ITableEntity.ETag { get; set; }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            UserId = properties["UserId"].StringValue;
            CityCode = properties["CityCode"].StringValue;
            ProductKey = properties["ProductKey"].StringValue;
            PersonCount = properties["PersonCount"].Int32Value ?? 1;
            MaxDays = properties["MaxDays"].Int32Value ?? 45;
            NotificationMail = properties["NotificationMail"].StringValue;
            ExpireDate = properties["ExpireDate"].DateTime ?? DateTime.Now.AddDays(30);
            MailQuota = properties["MailQuota"].Int32Value ?? 10;
            Enabled = properties["Enabled"].BooleanValue ?? true;
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            var result = PropertyHelper.WriteEntity(this, operationContext);
            return result;
        }

        public bool Validate()
        {
            // TODO : Complete
            return true;
        }

    }
}
