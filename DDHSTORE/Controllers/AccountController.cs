using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DDHSTORE.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== REGISTER =====================

        private void LoadProvinces()
        {
            var provinces = new List<ProvinceApiModel>();
            try
            {
                using (var wc = new System.Net.WebClient())
                {
                    var json = wc.DownloadString("https://provinces.open-api.vn/api/?depth=1");
                    provinces = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ProvinceApiModel>>(json) ?? new List<ProvinceApiModel>();
                }
            }
            catch { /* bỏ qua lỗi API */ }

            ViewBag.Provinces = provinces;
        }

        [HttpGet]
        public IActionResult Register()
        {
            LoadProvinces();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadProvinces();
                return View(model);
            }

            try
            {
                // 🔹 Tạo User
                var user = new User
                {
                    Username = model.Username,
                    Password = model.Password,
                    Email = model.Email,
                    Phone = model.Phone,
                    Status = 1,
                    RoleId = 2
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 🔹 Tạo Address
                var address = new Address
                {
                    UserId = user.UserId,
                    RecipientName = model.RecipientName,
                    Phone = model.Phone,
                    Province = model.Province,
                    District = model.District,
                    Ward = model.Ward,
                    Detail = model.Detail,
                    Status = 1
                };
                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đăng ký thành công!";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    msg += " | Inner: " + inner.Message;
                    inner = inner.InnerException;
                }
                ModelState.AddModelError("", "Lỗi: " + msg);
                LoadProvinces();
                return View(model);
            }
        }

        // 🔹 Model cho Province API
        public class ProvinceApiModel
        {
            public string code { get; set; }
            public string name { get; set; }
        }

     

        // ===================== LOGIN =====================
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Username và password không được để trống.");
                return View();
            }

            try
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == username && u.Status == 1);

                if (user == null)
                {
                    ModelState.AddModelError("", "Tài khoản không tồn tại hoặc bị khóa.");
                    return View();
                }

                using (var sha = SHA256.Create())
                {
                    var bytes = Encoding.UTF8.GetBytes(password);
                    var hashBytes = sha.ComputeHash(bytes);
                    var hashedPassword = BitConverter.ToString(hashBytes).Replace("-", "").ToUpper();

                    if (user.Password != hashedPassword)
                    {
                        ModelState.AddModelError("", "Mật khẩu không đúng.");
                        return View();
                    }
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role.RoleName.Trim()),
                    new Claim("UserId", user.UserId.ToString())
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return user.Role.RoleName.Trim() == "Admin"
                    ? RedirectToAction("Index", "Products")
                    : RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
                return View();
            }
        }

        // ===================== GOOGLE LOGIN =====================
        public IActionResult LoginGoogle()
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, "Google");
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal == null) return RedirectToAction("Login");

            var claims = result.Principal.Identities.First().Claims;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (email == null) return RedirectToAction("Login");

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                using var sha = SHA256.Create();
                var seed = $"{Guid.NewGuid():N}!{email}";
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
                var hashedPassword = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();

                user = new User
                {
                    Username = name ?? email,
                    Email = email,
                    Password = hashedPassword,
                    RoleId = 2,
                    Status = 1
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);
            }

            var claimsList = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.RoleName?.Trim() ?? "User"),
                new Claim("UserId", user.UserId.ToString())
            };

            var identity = new ClaimsIdentity(claimsList, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        // ===================== LOGOUT =====================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ===================== PROFILE =====================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Login");
            var userId = User.FindFirst("UserId")?.Value;
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));

            return user == null ? NotFound() : View(user);
        }

        // ===================== EDIT PROFILE =====================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            LoadProvinces();

            var userIdValue = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(userIdValue)) return RedirectToAction("Login");
            if (!int.TryParse(userIdValue, out var userId)) return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Status == 1);
            if (user == null) return NotFound();

            var addr = user.Addresses?.OrderBy(a => a.AddressId).FirstOrDefault();
            var model = new EditProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                Phone = user.Phone,
                RecipientName = addr?.RecipientName ?? user.Username,
                Province = addr?.Province ?? "",
                District = addr?.District ?? "",
                Ward = addr?.Ward ?? "",
                Detail = addr?.Detail
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadProvinces();
                return View(model);
            }

            var userIdValue = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrWhiteSpace(userIdValue)) return RedirectToAction("Login");
            if (!int.TryParse(userIdValue, out var userId)) return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Status == 1);
            if (user == null) return NotFound();

            // Update contact
            user.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            user.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            user.UpdatedAt = DateTime.Now;

            // Update first address (or create)
            var addr = user.Addresses?.OrderBy(a => a.AddressId).FirstOrDefault();
            if (addr == null)
            {
                addr = new Address
                {
                    UserId = user.UserId,
                    Status = 1
                };
                _context.Addresses.Add(addr);
            }

            addr.RecipientName = model.RecipientName.Trim();
            addr.Phone = (model.Phone ?? user.Phone ?? "").Trim();
            addr.Province = model.Province.Trim();
            addr.District = model.District.Trim();
            addr.Ward = model.Ward.Trim();
            addr.Detail = string.IsNullOrWhiteSpace(model.Detail) ? null : model.Detail.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        // ===================== CHANGE PASSWORD =====================
        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int userId, string oldPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            try
            {
                using (var sha = SHA256.Create())
                {
                    var bytes = Encoding.UTF8.GetBytes(oldPassword);
                    var hashBytes = sha.ComputeHash(bytes);
                    var hashedOld = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
                    if (user.Password != hashedOld)
                    {
                        ModelState.AddModelError("", "Mật khẩu cũ không đúng.");
                        return View();
                    }

                    user.Password = newPassword; // trigger hash tự động DB
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                HandleOracleException(ex);
            }

            return View();
        }

        public IActionResult AccessDenied() => View();

        // ===================== UTILITY =====================
        private void HandleOracleException(Exception ex)
        {
            if (ex == null) return;

            ModelState.AddModelError("", $"Exception type: {ex.GetType().FullName}");
            ModelState.AddModelError("", $"Message: {ex.Message}");

            if (ex is DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException;
                while (inner != null)
                {
                    if (inner is OracleException oraEx)
                        ModelState.AddModelError("", $"Oracle lỗi {oraEx.Number}: {oraEx.Message}");
                    else
                        ModelState.AddModelError("", "DbUpdateException Inner: " + inner.Message);
                    inner = inner.InnerException;
                }
            }

            if (ex is OracleException oraDirect)
                ModelState.AddModelError("", $"Oracle trực tiếp lỗi {oraDirect.Number}: {oraDirect.Message}");
        }
    }
}