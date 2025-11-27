using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Data;
using StokTakip.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StokTakip.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HareketTipsController : Controller
    {
        private readonly StokTakipBgcContext _context;

        public HareketTipsController(StokTakipBgcContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, bool showOnlyActive = false)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ShowOnlyActive"] = showOnlyActive;
            var hareketTipleri = from h in _context.HareketTips select h;
            
            if (showOnlyActive)
            {
                hareketTipleri = hareketTipleri.Where(h => h.Statu);
            }
            
            if (!String.IsNullOrEmpty(searchString))
            {
                hareketTipleri = hareketTipleri.Where(h => h.HareketTipAdi.Contains(searchString));
            }
            return View(await hareketTipleri.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var hareketTip = await _context.HareketTips
                .Include(h => h.OlusturanKullaniciNavigation)
                .Include(h => h.GuncelleyenKullaniciNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (hareketTip == null) return NotFound();
            return View(hareketTip);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("HareketTipAdi,IslemGostergesi")] HareketTip hareketTip)
        {
            ModelState.Remove("GuncelleyenKullaniciNavigation");
            ModelState.Remove("OlusturanKullaniciNavigation");
            ModelState.Remove("StokHarekets");

            if (ModelState.IsValid)
            {
                hareketTip.Statu = true;
                hareketTip.OlusturmaTarihi = DateTime.Now;

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdString))
                {
                    hareketTip.OlusturanKullanici = int.Parse(userIdString);
                }

                _context.Add(hareketTip);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(hareketTip);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var hareketTip = await _context.HareketTips.FindAsync(id);
            if (hareketTip == null) return NotFound();
            return View(hareketTip);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,HareketTipAdi,IslemGostergesi,Statu")] HareketTip hareketTipFromForm)
        {
            if (id != hareketTipFromForm.Id) return NotFound();

            ModelState.Remove("GuncelleyenKullaniciNavigation");
            ModelState.Remove("OlusturanKullaniciNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    var hareketTipFromDb = await _context.HareketTips.FindAsync(id);
                    if (hareketTipFromDb == null) return NotFound();

                    hareketTipFromDb.HareketTipAdi = hareketTipFromForm.HareketTipAdi;
                    hareketTipFromDb.IslemGostergesi = hareketTipFromForm.IslemGostergesi;
                    hareketTipFromDb.Statu = hareketTipFromForm.Statu;
                    hareketTipFromDb.GuncellemeTarihi = DateTime.Now;

                    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userIdString))
                    {
                        hareketTipFromDb.GuncelleyenKullanici = int.Parse(userIdString);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HareketTipExists(hareketTipFromForm.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(hareketTipFromForm);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var hareketTip = await _context.HareketTips.FirstOrDefaultAsync(m => m.Id == id);
            if (hareketTip == null) return NotFound();
            return View(hareketTip);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hareketTip = await _context.HareketTips.FindAsync(id);
            if (hareketTip != null)
            {
                // Önce bu hareket tipini kullanan tüm stok hareketlerini sil
                var stokHareketleri = await _context.StokHarekets
                    .Where(sh => sh.HareketTip == id)
                    .ToListAsync();
                
                _context.StokHarekets.RemoveRange(stokHareketleri);
                
                // Son olarak hareket tipini sil
                _context.HareketTips.Remove(hareketTip);
                
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool HareketTipExists(int id)
        {
            return _context.HareketTips.Any(e => e.Id == id);
        }

        public async Task<IActionResult> ExportToExcel(string searchString)
        {
            var hareketTipleriQuery = from h in _context.HareketTips select h; // Tüm hareket tiplerini export et (aktif ve pasif)
            
            if (!String.IsNullOrEmpty(searchString))
            {
                hareketTipleriQuery = hareketTipleriQuery.Where(h => h.HareketTipAdi.Contains(searchString));
            }
            var hareketTipleri = await hareketTipleriQuery.ToListAsync();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Hareket Tipleri");
                var currentRow = 1;
                worksheet.Cell(currentRow, 1).Value = "Hareket Tip Adı";
                worksheet.Cell(currentRow, 2).Value = "İşlem Yönü";
                worksheet.Cell(currentRow, 3).Value = "Durum";
                worksheet.Row(1).Style.Font.Bold = true;
                foreach (var tip in hareketTipleri)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = tip.HareketTipAdi;
                    worksheet.Cell(currentRow, 2).Value = tip.IslemGostergesi ? "Stok Artışı (+)" : "Stok Azalışı (-)";
                    worksheet.Cell(currentRow, 3).Value = tip.Statu ? "Aktif" : "Pasif";
                }
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"HareketTipleri_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}