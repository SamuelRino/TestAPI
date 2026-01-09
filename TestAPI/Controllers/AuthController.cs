using Azure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using NuGet.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
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

        [HttpPost("change-role")]
        [AllowAnonymous]
        public async Task<IActionResult> ChangeRole([FromBody] ChangeRoleRequest request)
        {
            if (request.NewRole != 1 && request.NewRole != 2)
            {
                return BadRequest(new { message = "Роль должна быть 1 (админ) или 2 (пользователь)" });
            }

            var currentUserLogin = User.Identity?.Name;

            if (currentUserLogin == request.Login)
            {
                return BadRequest(new { message = "Нельзя изменить роль самому себе" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserLogin == request.Login);

            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            if (user.UserRole == 1 && request.NewRole == 2)
            {
                var remainingAdmins = await _context.Users.CountAsync(u => u.UserRole == 1 && u.UserLogin != request.Login);

                if (remainingAdmins == 0)
                {
                    return BadRequest(new { message = "Нельзя удалить последнего администратора" });
                }
            }

            user.UserRole = (byte)request.NewRole;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Роль успешно изменена",
                newRole = request.NewRole
            });
        }
    }

    public class ChangeRoleRequest
    {
        [Required(ErrorMessage = "Логин обязателен")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Роль обязательна")]
        [Range(1, 2, ErrorMessage = "Роль должна быть 1 или 2")]
        public int NewRole { get; set; }
    }
}
