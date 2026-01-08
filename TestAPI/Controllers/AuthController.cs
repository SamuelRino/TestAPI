using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using NuGet.Common;
using TestAPI.Models;

namespace TestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly StudentsContext _context;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;

        public AuthController(StudentsContext context, ITokenService tokenService, IPasswordHasher passwordHasher)
        {
            _context = context;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.Include(u => u.UserRoleNavigation).FirstOrDefaultAsync(u => u.UserLogin == request.Login);

            if (user == null)
            {
                return Unauthorized(new { message = "Неверный логин или пароль" });
            }

            if (!_passwordHasher.VerifyPassword(user.UserPassword, request.Password))
            {
                return Unauthorized(new { message = "Неверный логин или пароль" });
            }

            var token = _tokenService.GenerateToken(user, user.UserRoleNavigation);

            var response = new AuthResponse
            {
                Token = token,
                Username = user.Username,
                Role = user.UserRoleNavigation.RoleName,
                Expiration = DateTime.UtcNow.AddMinutes(60)
            };

            return Ok(response);
        }


        //POST api/Auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserLogin == request.Login);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Пользователь с таким логином уже существует" });
            }

            var hashedPassword = _passwordHasher.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                UserLogin = request.Login,
                UserPassword = hashedPassword,
                UserRole = 2
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            var role = await _context.Roles.FindAsync(user.UserRole);

            var token = _tokenService.GenerateToken(user, role);

            var response = new AuthResponse
            {
                Token = token,
                Username = user.Username,
                Role = role.RoleName,
                Expiration = DateTime.UtcNow.AddMinutes(60)
            };

            return Ok(response);
        }
    }
}
