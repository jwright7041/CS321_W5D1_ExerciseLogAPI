﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CS321_W5D1_ExerciseLogAPI.Core.Models;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using CS321_W5D1_ExerciseLogAPI.ApiModels;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CS321_W5D1_ExerciseLogAPI.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<User> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        // POST api/auth/register
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegistrationModel registration)
        {
            // map registration model into a new AppUser domain object
            var newUser = new User
            {
                UserName = registration.Email,
                Email = registration.Email,
                FirstName = registration.FirstName,
                LastName = registration.LastName
            };
            // call UserManager to create new user and hash password
            var result = await _userManager.CreateAsync(newUser, registration.Password);
            if (result.Succeeded)
            {
                return Ok(newUser.ToApiModel());
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return BadRequest(ModelState);
        }

        // POST api/auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel login)
        {
            IActionResult response = Unauthorized();
            // try to authenticate the user
            var user = await AuthenticateUserAsync(login.Email, login.Password);

            if (user != null)
            {
                // generate the JWT
                var tokenString = GenerateJSONWebToken(user);
                response = Ok(new { token = tokenString });
            }

            return response;
        }
        private string GenerateJSONWebToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            // retrieve the secret key from configuration
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            // create signing credentials based on secret key
            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);
            var claims = new Claim[]
            {
                // add Email to the token payload
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            };
            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7), // make the token valid for 7 days
                signingCredentials: credentials);
            // return the string representation of the token
            return tokenHandler.WriteToken(token);
        }

        private async Task<User> AuthenticateUserAsync(string userName, string password)
        {
            // retrieve the user by username and then check password
            var user = await _userManager.FindByNameAsync(userName);
            if (user != null && await _userManager.CheckPasswordAsync(user, password))
            {
                return user;
            }
            return null;
        }
    }
}
