using Login_SignUp.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Login_SignUp.Services.Interfaces
{
    public class EmailSenderService : IEmailSenderService
    {
        private readonly EmailSettings _emailSettings;

        public EmailSenderService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public void SendEmailVerification(string link, string receiver)
        {
            MailMessage mail = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromMailAddress),
                Subject = "Request For Email Verification",
                Body = $"Please reset your password by clicking here: " +
                             $"<a href='{link}'>link</a>",
                IsBodyHtml = true
            };
            mail.To.Add(receiver);

            using (SmtpClient smtp = new SmtpClient()
            {
                Host = _emailSettings.Host,
                Port = _emailSettings.Port,
                EnableSsl = _emailSettings.EnableSsl,
                UseDefaultCredentials = _emailSettings.UseDefaultCredentials,
                Credentials = new NetworkCredential(_emailSettings.FromMailAddress, _emailSettings.Password)
            })
            {
                smtp.Send(mail);
            };
        }
    }
    
}
