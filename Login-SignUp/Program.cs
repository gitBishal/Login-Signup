using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Login_SignUp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Login_SignUp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
           var host =  CreateHostBuilder(args).Build();
            using (var serviceScope = host.Services.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

                await dbContext.Database.MigrateAsync();

                var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    var adminRole = new IdentityRole("Admin");
                    await roleManager.CreateAsync(adminRole);
                }

                if (!await roleManager.RoleExistsAsync("Basic"))
                {
                    var basicRole = new IdentityRole("Basic");
                    await roleManager.CreateAsync(basicRole);
                }
                if (!await roleManager.RoleExistsAsync("Paid"))
                {
                    var paidRole = new IdentityRole("Paid");
                    await roleManager.CreateAsync(paidRole);
                }

                var user1 = userManager.FindByEmailAsync("admin@matri.com").Result;
                if(user1 == null)
                {
                    var applicationUser = new ApplicationUser
                    {
                        Email = "admin@matri.com",
                        EmailConfirmed = true,
                        UserName = "admin@matri.com"
                    };

                    await userManager.CreateAsync(applicationUser, "P@77w0rd");
                   await userManager.AddToRoleAsync(applicationUser, "Admin");
                }
                var user2 = userManager.FindByEmailAsync("basic@matri.com").Result;
                if (user2 == null)
                {
                    var applicationUser = new ApplicationUser
                    {
                        Email = "basic@matri.com",
                        EmailConfirmed = true,
                        UserName = "basic@matri.com"
                    };

                    await userManager.CreateAsync(applicationUser, "P@77w0rd");
                    await userManager.AddToRoleAsync(applicationUser, "Basic");
                }
                var user3 = userManager.FindByEmailAsync("paid@matri.com").Result;
                if (user3 == null)
                {

                    var applicationUser = new ApplicationUser
                    {
                        Email = "paid@matri.com",
                        EmailConfirmed = true,
                        UserName = "paid@matri.com"
                    };

                    await userManager.CreateAsync(applicationUser, "P@77w0rd");
                    await userManager.AddToRoleAsync(applicationUser, "Paid");
                }
            }
            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
