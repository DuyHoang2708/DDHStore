using DDHSTORE.Data;
using DDHSTORE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace DDHSTORE.Controllers
{
    public class BrandController : Controller
    {
        private readonly AppDbContext _context;

        public BrandController(AppDbContext context)
        {
            _context = context;
        }

        // ===================== Index =====================
        public async Task<IActionResult> Index()
        {
            var brands = await _context.Brands.ToListAsync();
            return View(brands);
        }

        // ===================== Create =====================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Kiểm tra tên thương hiệu đã tồn tại chưa
                var exists = await _context.Brands.AnyAsync(b => b.BrandName == model.BrandName);
                if (exists)
                {
                    ModelState.AddModelError("BrandName", "Tên thương hiệu đã tồn tại!");
                    return View(model);
                }

                _context.Brands.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thêm thương hiệu thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException;
                string oracleMessage = inner?.Message ?? dbEx.Message;
                TempData["ErrorMessage"] = "Lỗi: " + oracleMessage;
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
                return View(model);
            }
        }

        // ===================== Edit =====================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand model)
        {
            if (id != model.BrandId) return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // Kiểm tra tên đã tồn tại chưa (trừ chính nó)
                var exists = await _context.Brands.AnyAsync(b => b.BrandName == model.BrandName && b.BrandId != id);
                if (exists)
                {
                    ModelState.AddModelError("BrandName", "Tên thương hiệu đã tồn tại!");
                    return View(model);
                }

                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thương hiệu thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException;
                while (inner != null)
                {
                    if (inner is OracleException oraEx)
                        ModelState.AddModelError("", $"Oracle lỗi {oraEx.Number}: {oraEx.Message}");
                    else
                        ModelState.AddModelError("", "DbUpdateException: " + inner.Message);
                    inner = inner.InnerException;
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
            }

            return View(model);
        }

        // ===================== Delete =====================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();

            // Kiểm tra xem có sản phẩm nào thuộc thương hiệu này không
            var productCount = await _context.Products.CountAsync(p => p.BrandId == id);
            ViewBag.ProductCount = productCount;

            return View(brand);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var brand = await _context.Brands.FindAsync(id);
                if (brand == null) return NotFound();

                // Kiểm tra lại lần nữa trước khi xóa
                var productCount = await _context.Products.CountAsync(p => p.BrandId == id);
                if (productCount > 0)
                {
                    TempData["ErrorMessage"] = $"Không thể xóa thương hiệu vì có {productCount} sản phẩm đang thuộc thương hiệu này!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa thương hiệu thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException;
                if (inner is OracleException oraEx && oraEx.Number == 2292) // ORA-02292: integrity constraint violated
                {
                    TempData["ErrorMessage"] = "Không thể xóa thương hiệu vì đang có sản phẩm liên quan!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Lỗi: " + (inner?.Message ?? dbEx.Message);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // ===================== ToggleStatus (Ẩn/Hiện) =====================
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
                return NotFound();

            try
            {
                // 1 -> 0 hoặc 0 -> 1
                brand.Status = brand.Status == 1 ? 0 : 1;

                _context.Update(brand);
                await _context.SaveChangesAsync();

                TempData["Success"] = brand.Status == 1 ? "Hiện thương hiệu thành công!" : "Ẩn thương hiệu thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException;
                TempData["ErrorMessage"] = "Lỗi: " + (inner?.Message ?? dbEx.Message);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}