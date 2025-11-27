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
    public class OlcuBirimisController : Controller
    {
        private readonly StokTakipBgcContext _context;

        public OlcuBirimisController(StokTakipBgcContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, bool showOnlyActive = false)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ShowOnlyActive"] = showOnlyActive;
            var olcuBirimleri = from o in _context.OlcuBirimis select o;

            if (showOnlyActive)
            {
                olcuBirimleri = olcuBirimleri.Where(o => o.Statu);
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                olcuBirimleri = olcuBirimleri.Where(s => s.OlcuBirimAdi.Contains(searchString));
            }

            return View(await olcuBirimleri.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var olcuBirimi = await _context.OlcuBirimis
                .Include(o => o.OlusturanKullaniciNavigation)
                .Include(o => o.GuncelleyenKullaniciNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (olcuBirimi == null) return NotFound();
            return View(olcuBirimi);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OlcuBirimAdi,Statu")] OlcuBirimi olcuBirimi)
        {
            if (ModelState.IsValid)
            {
                olcuBirimi.OlusturmaTarihi = DateTime.Now;
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdString))
                {
                    olcuBirimi.OlusturanKullanici = int.Parse(userIdString);
                }
                _context.Add(olcuBirimi);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(olcuBirimi);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var olcuBirimi = await _context.OlcuBirimis.FindAsync(id);
            if (olcuBirimi == null) return NotFound();
            return View(olcuBirimi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OlcuBirimAdi,Statu")] OlcuBirimi olcuBirimi)
        {
            if (id != olcuBirimi.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var olcuBirimiFromDb = await _context.OlcuBirimis.FindAsync(id);
                    if (olcuBirimiFromDb == null) return NotFound();

                    olcuBirimiFromDb.OlcuBirimAdi = olcuBirimi.OlcuBirimAdi;
                    olcuBirimiFromDb.Statu = olcuBirimi.Statu;
                    olcuBirimiFromDb.GuncellemeTarihi = DateTime.Now;
                    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userIdString))
                    {
                        olcuBirimiFromDb.GuncelleyenKullanici = int.Parse(userIdString);
                    }
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OlcuBirimiExists(olcuBirimi.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(olcuBirimi);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var olcuBirimi = await _context.OlcuBirimis.FirstOrDefaultAsync(m => m.Id == id);
            if (olcuBirimi == null) return NotFound();
            return View(olcuBirimi);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var olcuBirimi = await _context.OlcuBirimis.FindAsync(id);
            if (olcuBirimi != null)
            {
                // Önce bu ölçü birimini kullanan tüm stokları sil
                var stoklar = await _context.Stoks
                    .Where(s => s.StokOlcuBirim == id)
                    .ToListAsync();
                
                foreach (var stok in stoklar)
                {
                    // Stok ile ilgili tüm stok hareketlerini sil
                    var hareketler = await _context.StokHarekets
                        .Where(sh => sh.StokId == stok.Id)
                        .ToListAsync();
                    _context.StokHarekets.RemoveRange(hareketler);
                    
                    // Stok durumlarını sil
                    var stokDurumlari = await _context.StokDurums
                        .Where(sd => sd.StokId == stok.Id)
                        .ToListAsync();
                    _context.StokDurums.RemoveRange(stokDurumlari);
                    
                    // Stoku sil
                    _context.Stoks.Remove(stok);
                }
                
                // Son olarak ölçü birimini sil
                _context.OlcuBirimis.Remove(olcuBirimi);
                
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportToExcel(string searchString)
        {
            var olcuBirimleriQuery = _context.OlcuBirimis.AsQueryable(); // Tüm ölçü birimlerini export et (aktif ve pasif)
            if (!String.IsNullOrEmpty(searchString))
            {
                olcuBirimleriQuery = olcuBirimleriQuery.Where(s => s.OlcuBirimAdi.Contains(searchString));
            }
            var olcuBirimleri = await olcuBirimleriQuery.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Ölçü Birimleri");
                var currentRow = 1;
                worksheet.Cell(currentRow, 1).Value = "Birim Adı";
                worksheet.Cell(currentRow, 2).Value = "Durum";
                worksheet.Row(1).Style.Font.Bold = true;

                foreach (var item in olcuBirimleri)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = item.OlcuBirimAdi;
                    worksheet.Cell(currentRow, 2).Value = item.Statu ? "Aktif" : "Pasif";
                }
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"OlcuBirimleri_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        private bool OlcuBirimiExists(int id)
        {
            return _context.OlcuBirimis.Any(e => e.Id == id);
        }
    }
}