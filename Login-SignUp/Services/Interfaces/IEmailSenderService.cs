using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Login_SignUp.Services.Interfaces
{
   public interface IEmailSenderService
    {
        void SendEmailVerification(string link, string receiver);
    }
}
