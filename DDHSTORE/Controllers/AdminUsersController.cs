using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DDHSTORE.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly AppDbContext _context;

        public AdminUsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminUsers
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderByDescending(u => u.UserId)
                .ToListAsync();

            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View(users);
        }

        // POST: AdminUsers/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Toggle between 1 (Active) and 0 (Blocked)
            user.Status = user.Status == 1 ? 0 : 1;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái cho người dùng {user.Username}.";
            
            return RedirectToAction(nameof(Index));
        }

        // POST: AdminUsers/ChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int userId, int roleId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var role = await _context.Roles.FindAsync(roleId);
            if (role == null) return BadRequest("Role không tồn tại.");

            user.RoleId = roleId;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã thay đổi quyền của {user.Username} thành {role.RoleName}.";

            return RedirectToAction(nameof(Index));
        }
    }
}
