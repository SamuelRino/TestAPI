using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestAPI.Models;

namespace TestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly StudentsContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UsersController(StudentsContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: api/Users
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.Include(u => u.UserRoleNavigation).ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            await _context.Roles.LoadAsync();

            var user = await _context.Users.Include(u => u.UserRoleNavigation).FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> PutUser(byte id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest();
            }

            var oldUser = await _context.Users.Include(u => u.UserRoleNavigation).AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);

            if (oldUser == null) return NotFound();

            if (oldUser.UserRole == 1 && user.UserRole != 1)
            {
                var remainingAdmins = await _context.Users.CountAsync(u => u.UserRole == 1 && u.UserId != id);
                if (remainingAdmins == 0)
                {
                    return BadRequest(new { message = "Нельзя изменить роль последнего администратора" });
                }
            }

            if (oldUser.UserPassword != user.UserPassword)
            {
                var hashedPassword = _passwordHasher.HashPassword(user.UserPassword);

                user.UserPassword = hashedPassword;
            }

            if (oldUser.Username != user.Username)
            {
                var usernameExists = await _context.Users.AnyAsync(u => u.Username == user.Username);
                if (usernameExists)
                {
                    return BadRequest(new { message = "Такой username уже существует" });
                }
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Users
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            var usernameExists = await _context.Users.Include(u => u.UserRoleNavigation).AnyAsync(u => u.Username == user.Username);
            if (usernameExists)
            {
                return BadRequest(new { message = "Username уже существует" });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.UserId }, user);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteUser(byte id)
        {
            var user = await _context.Users.Include(u => u.UserRoleNavigation).FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            var remainingAdmins = await _context.Users.CountAsync(u => u.UserRole == 1 && u.UserId != id);
            if (remainingAdmins == 0)
            {
                return BadRequest(new { message = "Нельзя удалить последнего администратора" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(byte id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
