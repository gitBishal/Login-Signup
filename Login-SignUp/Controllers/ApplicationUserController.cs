using Login_SignUp.Models;
using Login_SignUp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly TokenValidationParameters _tokenValidationParameters;

        public ApplicationUserController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSenderService emailService,
            IOptions<ApplicationSettings> appSettings,
            AppDbContext context,
            TokenValidationParameters tokenValidationParameters
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _appSettings = appSettings.Value;
            _roleManager = roleManager;
            _context = context;
            _tokenValidationParameters = tokenValidationParameters;
        }

        [Authorize(Roles = "Basic")]
        [HttpGet, Route("registertest")]
        public IEnumerable<string> Register()
        {
            return new string[] { "value3", "value4" };
        }

        [HttpPost, Route("register")]
        public async Task<IActionResult> Register([FromBody]RegistrationModel model)
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
                //Give basic role to the user
                await _userManager.AddToRoleAsync(user, "Basic");
                //generation of the email token
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var link = Url.Action(nameof(VerifyEmail), "ApplicationUser", new { userId = user.Id, code }, Request.Scheme, Request.Host.ToString());

                // var link = $"{_appSettings.Host_Url}applicationuser/email-verification/userId={user.Id}/code={code}";
                _emailService.SendEmailVerification(link, model.Email);
                return Ok(new { msg = "Verfication email has been sent" });
            }
            return BadRequest(new { error = "Please try again later" });
        }

        [HttpGet]
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
        public async Task<IActionResult> Login([FromBody] RegistrationModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                //Remove all the refresh token previously issue for this user
                var responseTokensIssuedForThisUser = await _context.RefreshTokens.Where(x => x.UserId == user.Id).ToListAsync();
                if (responseTokensIssuedForThisUser != null || responseTokensIssuedForThisUser.Count > 0)
                {
                    _context.RefreshTokens.RemoveRange(responseTokensIssuedForThisUser);
                    await _context.SaveChangesAsync();
                }
                return await GenerateTokenForThisUser(user);
            }
            return BadRequest(new { Error = "Username or password invalid" });

            //Get role assigned to the user
        }

        [HttpPost,Route("refreshtoken")]
        public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest request)
        {
            var validatedToken = GetPrincipalFromToken(request.Token);

            if (validatedToken == null)
            {
                return BadRequest(new { Errors = new[] { "Invalid Token" } });
            }

            var expiryDateUnix =
                long.Parse(validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

            var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(expiryDateUnix);

            if (expiryDateTimeUtc > DateTime.UtcNow)
            {
                return BadRequest(new { Errors = new[] { "This token hasn't expired yet" } });
            }

            var user = await _userManager.FindByIdAsync(validatedToken.Claims.Single(x => x.Type == "UserID").Value);

            var storedRefreshToken = await _context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == request.RefreshToken && x.UserId == user.Id);

            if (storedRefreshToken == null)
            {
                return BadRequest(new { Errors = new[] { "This refresh token does not exist" } });
            }

            if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
            {
                return BadRequest(new { Errors = new[] { "This refresh token has expired" } });
            }

            if (storedRefreshToken.Invalidated)
            {
                return BadRequest(new { Errors = new[] { "This refresh token has been invalidated" } });
            }

            if (storedRefreshToken.Used)
            {
                return BadRequest(new { Errors = new[] { "This refresh token has been used" } });
            }

            storedRefreshToken.Used = true;
            _context.RefreshTokens.Update(storedRefreshToken);
            await _context.SaveChangesAsync();

            return await GenerateTokenForThisUser(user,true);
        }

        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var tokenValidationParameters = _tokenValidationParameters.Clone();
                tokenValidationParameters.ValidateLifetime = false;
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);
                if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
        {
            return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
                   jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                       StringComparison.InvariantCultureIgnoreCase);
        }

        private async Task<IActionResult> GenerateTokenForThisUser(ApplicationUser user,bool isRefreshToken=false)
        {
            try
            {
                var roles = await _userManager.GetRolesAsync(user);

                // IdentityOptions _options = new IdentityOptions();
                var claims = new List<Claim>
                {
                      new Claim("UserID",user.Id.ToString()),
                      new Claim("UserName",user.UserName.ToString())
                };
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims
                    ),
                    Expires = isRefreshToken ? DateTime.UtcNow.AddDays(1) : DateTime.UtcNow.AddSeconds(2),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.JWT_Secret)), SecurityAlgorithms.HmacSha256Signature)
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                var token = tokenHandler.WriteToken(securityToken);

                //Add refresh token for this user to the database and send it along with the token
                var refreshToken = new RefreshToken
                {
                    //  JwtId = token.Id,
                    UserId = user.Id,
                    CreationDate = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddMonths(6)
                };

                await _context.RefreshTokens.AddAsync(refreshToken);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Token = token,
                    RefreshToken = refreshToken.Token
                });
            }
            catch (Exception ex)
            {
                return null;
            }

        }
    }
}