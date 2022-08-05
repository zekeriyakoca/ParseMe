using ParseMe.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParseMe.Binders
{
    internal static class ModelBinders
    {
        public static CheckDto ToCheckDto(this UpsertAppointmentRequestDto requestDto)
        {
            return new CheckDto
            {
                CityCode = requestDto.CityCode,
                ProductKey = requestDto.ProductKey,
                PersonCount = requestDto.PersonCount,
                MaxDays = requestDto.MaxDays,
                NotificationMail = requestDto.NotificationMail,
                ExpireDate = requestDto.ExpireDate,
                MailQuota = requestDto.MailQuota,
                Enabled = requestDto.Enabled,
                PartitionKey = Constants.IndAppointmentPartitionKey,
                RowKey = Guid.NewGuid().ToString(),
                UserId = "Default User"
            };
        }
        public static PersonalCodeDto ToPersonalCodeDto(this UpsertPersonalCodeRequestDto requestDto)
        {
            return new PersonalCodeDto
            {
                Code = Guid.NewGuid().ToString(),
                Email = requestDto.Email,
                ExpireDate = requestDto.ExpireDate,
                PartitionKey = Constants.IndPersonalCodePartitionKey,
                RowKey = Guid.NewGuid().ToString()
            };
        }
    }
}
