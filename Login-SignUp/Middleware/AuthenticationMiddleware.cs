using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using Login_SignUp.Constants;

namespace Login_SignUp.Middleware
{
    public static class AuthenticationExtension
    {
        public static IServiceCollection AddTokenAuthentication(this IServiceCollection services, IConfiguration config)
        {
           // var secret = config.GetSection("JwtConfig").GetSection("secret").Value;
            var key = Encoding.UTF8.GetBytes(config["ApplicationSettings:JWT_Secret"].ToString());


            // var key = Encoding.ASCII.GetBytes(secret);
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };

            services.AddSingleton(tokenValidationParameters);

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
                x.TokenValidationParameters = tokenValidationParameters;


            });

            services.AddAuthorization(options =>
            {
                foreach (var item in Permissions.GetAllModuleNames())
                {
                    var allClaimsOfThisModule = Permissions.GeneratePermissionsForModule(item);

                    foreach (var claim in allClaimsOfThisModule)
                    {
                        options.AddPolicy(claim, policy =>
                       policy.RequireClaim("Permission", claim));

                    }

                }
               
               
            });

            return services;
        }
    }
}
