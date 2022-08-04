using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParseMe.Dtos
{
    internal class UpsertAppointmentRequestDto
    {
        public string CityCode { get; set; }
        public string ProductKey { get; set; }
        public string NotificationMail { get; set; }
        public DateTime ExpireDate { get; set; }
        public int PersonCount { get; set; } = 1;
        public int MaxDays { get; set; } = 30;
        public int MailQuota { get; set; } = 10;
        public bool Enabled { get; set; } = true;

    }

}
