using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StokTakip.Data;
using StokTakip.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StokTakip.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SorumlusController : Controller
    {
        private readonly StokTakipBgcContext _context;

        public SorumlusController(StokTakipBgcContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, bool showOnlyActive = false)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ShowOnlyActive"] = showOnlyActive;
            var sorumlular = from s in _context.Sorumlus select s;
            
            if (showOnlyActive)
            {
                sorumlular = sorumlular.Where(s => s.Statu);
            }
            
            if (!String.IsNullOrEmpty(searchString))
            {
                sorumlular = sorumlular.Where(s => s.SorumluAdi.Contains(searchString));
            }
            return View(await sorumlular.AsNoTracking().ToListAsync());
        }

        // --- Details, Create, Edit, Delete metotları aynı kalıyor ---

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var sorumlu = await _context.Sorumlus
                .Include(s => s.OlusturanKullaniciNavigation)
                .Include(s => s.GuncelleyenKullaniciNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (sorumlu == null) return NotFound();
            return View(sorumlu);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SorumluAdi,Statu")] Sorumlu sorumlu)
        {
            if (ModelState.IsValid)
            {
                sorumlu.OlusturmaTarihi = DateTime.Now;
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdString))
                {
                    sorumlu.OlusturanKullanici = int.Parse(userIdString);
                }
                _context.Add(sorumlu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(sorumlu);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var sorumlu = await _context.Sorumlus.FindAsync(id);
            if (sorumlu == null) return NotFound();
            return View(sorumlu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SorumluAdi,Statu")] Sorumlu sorumluFromForm)
        {
            if (id != sorumluFromForm.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try
                {
                    var sorumluFromDb = await _context.Sorumlus.FindAsync(id);
                    if (sorumluFromDb == null) return NotFound();

                    sorumluFromDb.SorumluAdi = sorumluFromForm.SorumluAdi;
                    sorumluFromDb.Statu = sorumluFromForm.Statu;
                    sorumluFromDb.GuncellemeTarihi = DateTime.Now;

                    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userIdString))
                    {
                        sorumluFromDb.GuncelleyenKullanici = int.Parse(userIdString);
                    }
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SorumluExists(sorumluFromForm.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(sorumluFromForm);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var sorumlu = await _context.Sorumlus
                .Include(s => s.OlusturanKullaniciNavigation)
                .Include(s => s.GuncelleyenKullaniciNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (sorumlu == null) return NotFound();
            return View(sorumlu);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sorumlu = await _context.Sorumlus.FindAsync(id);
            if (sorumlu != null)
            {
                // Önce bu sorumluyu kullanan tüm stok hareketlerini sil
                var stokHareketleri = await _context.StokHarekets
                    .Where(sh => sh.SorumluId == id)
                    .ToListAsync();
                
                _context.StokHarekets.RemoveRange(stokHareketleri);
                
                // Son olarak sorumluyu sil
                _context.Sorumlus.Remove(sorumlu);
                
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SorumluExists(int id)
        {
            return _context.Sorumlus.Any(e => e.Id == id);
        }

        // --- YENİ EKLENEN EXCEL METODU ---
        public async Task<IActionResult> ExportToExcel(string searchString)
        {
            var sorumlularQuery = from s in _context.Sorumlus select s; // Tüm sorumluları export et (aktif ve pasif)
            
            if (!String.IsNullOrEmpty(searchString))
            {
                sorumlularQuery = sorumlularQuery.Where(s => s.SorumluAdi.Contains(searchString));
            }
            var sorumlular = await sorumlularQuery.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Sorumlular");
                var currentRow = 1;
                worksheet.Cell(currentRow, 1).Value = "Sorumlu Adı";
                worksheet.Cell(currentRow, 2).Value = "Statü";
                worksheet.Row(1).Style.Font.Bold = true;

                foreach (var sorumlu in sorumlular)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = sorumlu.SorumluAdi;
                    worksheet.Cell(currentRow, 2).Value = sorumlu.Statu ? "Aktif" : "Pasif";
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"Sorumlular_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}