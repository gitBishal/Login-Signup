using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;

namespace Login_SignUp.Middleware
{
    public static class AuthenticationExtension
    {
        public static IServiceCollection AddTokenAuthentication(this IServiceCollection services, IConfiguration config)
        {
           // var secret = config.GetSection("JwtConfig").GetSection("secret").Value;
            var key = System.Text.Encoding.UTF8.GetBytes(config["ApplicationSettings:JWT_Secret"].ToString());


           // var key = Encoding.ASCII.GetBytes(secret);
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = false;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true
                    
                };
            });

            return services;
        }
    }
}
