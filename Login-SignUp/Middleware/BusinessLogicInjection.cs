using Login_SignUp.Models;
using Login_SignUp.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Login_SignUp.Middleware
{
    public static class BusinessLogicExtension
    {
        public static IServiceCollection AddBusiness( this IServiceCollection services, IConfiguration config)
        {
            services.AddScoped<IEmailSenderService, EmailSenderService>();
            services.Configure<ApplicationSettings>(config.GetSection("ApplicationSettings"));
            services.Configure<EmailSettings>(config.GetSection("EmailSettings"));

            return services;
        }
    }
}
