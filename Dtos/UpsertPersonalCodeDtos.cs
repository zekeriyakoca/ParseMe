using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ParseMe.Dtos
{
    internal class UpsertPersonalCodeRequestDto
    {
        public string Code { get; set; }
        public string Email { get; set; }
        public DateTime ExpireDate { get; set; } = DateTime.Now.AddDays(14);

        public bool IsValid()
        {
            if (ExpireDate < DateTime.Now && ExpireDate > DateTime.Now.AddMonths(1))
            {
                return false;
            }
            try
            {
                _ = new MailAddress(Email);
            }
            catch (FormatException)
            {
                return false;
            }

            return true;
        }
    }
}
