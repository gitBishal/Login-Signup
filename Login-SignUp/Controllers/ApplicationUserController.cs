﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Login_SignUp.Models;
using Login_SignUp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Login_SignUp
{
    [Route("api/[controller]")]
    public class ApplicationUserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSenderService _emailService;
        private readonly ApplicationSettings _appSettings;

        public ApplicationUserController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSenderService emailService,
            IOptions<ApplicationSettings> appSettings)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _appSettings = appSettings.Value;
        }
        
        [HttpGet,Route("registertest")]
        public IEnumerable<string> Register()
        {
            return new string[] { "value3", "value4" };
        }

        [HttpPost, Route("register")]
        public async Task<IActionResult> Register(RegistrationModel model)
        {
            //register functionality
            

            var user = new ApplicationUser
            {
                Email = model.Email,
                UserName = model.Email

            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                //generation of the email token
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var link = $"{_appSettings.Host_Url}applicationuser/email-verification/userId={user.Id}/code={code}";
                _emailService.SendEmailVerification(link, model.Email);
                return Ok(new { msg = "Verfication email has been sent" });
            }
            return BadRequest(new { error = "Please try again later" });
        }

        [HttpGet, Route("email-verification/{userId}/{code}")]
        public async Task<IActionResult> VerifyEmail(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return BadRequest();

            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                return Ok(new { msg = "Your email has been verified" });
            }

            return BadRequest(new { error = "Please try again later" });
        }
        [HttpPost]
        [Route("Login")]
        //POST : /api/ApplicationUser/Login
        public async Task<IActionResult> Login(RegistrationModel model)
        {
           
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                //Get role assigned to the user
                var role = await _userManager.GetRolesAsync(user);
                IdentityOptions _options = new IdentityOptions();

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim("UserID",user.Id.ToString()),
                     //   new Claim(_options.ClaimsIdentity.RoleClaimType,role.FirstOrDefault())
                    }),
                    Expires = DateTime.UtcNow.AddDays(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.JWT_Secret)), SecurityAlgorithms.HmacSha256Signature)
                };
                var tokenHandler = new JwtSecurityTokenHandler();
                var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                var token = tokenHandler.WriteToken(securityToken);
                return Ok(new { token });
            }
            else
                return BadRequest(new { message = "Username or password is incorrect." });
        }


    }
}