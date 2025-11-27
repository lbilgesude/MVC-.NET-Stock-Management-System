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
    [Authorize(Roles = "Admin,Depo Yetkilisi")]
    public class DepoesController : Controller
    {
        private readonly StokTakipBgcContext _context;

        public DepoesController(StokTakipBgcContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, bool showOnlyActive = false)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ShowOnlyActive"] = showOnlyActive;

            var depolarQuery = _context.Depos
                .Include(d => d.DepoEslestirmes)
                .ThenInclude(de => de.AltDepo)
                .AsQueryable();

            if (showOnlyActive)
            {
                depolarQuery = depolarQuery.Where(d => d.Statu);
            }

            if (!String.IsNullOrEmpty(searchString))
            {
                depolarQuery = depolarQuery.Where(d => d.DepoAdi.Contains(searchString));
            }

            return View(await depolarQuery.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var depo = await _context.Depos
                .Include(d => d.GuncelleyenKullaniciNavigation)
                .Include(d => d.OlusturanKullaniciNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (depo == null) return NotFound();
            return View(depo);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DepoAdi,Statu")] Depo depo)
        {
            ModelState.Remove("DepoEslestirmes");
            ModelState.Remove("GuncelleyenKullaniciNavigation");
            ModelState.Remove("OlusturanKullaniciNavigation");

            if (ModelState.IsValid)
            {
                depo.OlusturmaTarihi = DateTime.Now;
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdString))
                {
                    depo.OlusturanKullanici = int.Parse(userIdString);
                }

                _context.Add(depo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(depo);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var depo = await _context.Depos.FindAsync(id);
            if (depo == null) return NotFound();
            return View(depo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DepoAdi,Statu")] Depo depo)
        {
            if (id != depo.Id) return NotFound();

            ModelState.Remove("DepoEslestirmes");
            ModelState.Remove("GuncelleyenKullaniciNavigation");
            ModelState.Remove("OlusturanKullaniciNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    var depoFromDb = await _context.Depos.FindAsync(id);
                    if (depoFromDb == null) return NotFound();

                    depoFromDb.DepoAdi = depo.DepoAdi;
                    depoFromDb.Statu = depo.Statu;
                    depoFromDb.GuncellemeTarihi = DateTime.Now;
                    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userIdString))
                    {
                        depoFromDb.GuncelleyenKullanici = int.Parse(userIdString);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepoExists(depo.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(depo);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var depo = await _context.Depos.FirstOrDefaultAsync(m => m.Id == id);
            if (depo == null) return NotFound();
            return View(depo);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var depo = await _context.Depos.FindAsync(id);
            if (depo != null)
            {
                try
                {
                    // Foreign key constraint'leri geçici olarak devre dışı bırak
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE STOK_HAREKET NOCHECK CONSTRAINT ALL");
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE STOK_DURUM NOCHECK CONSTRAINT ALL");
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE DEPO_ESLESTIRME NOCHECK CONSTRAINT ALL");
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE ALT_DEPO NOCHECK CONSTRAINT ALL");
                    
                    // Bu depoya bağlı tüm stok hareketlerini sil
                    await _context.Database.ExecuteSqlRawAsync(@"
                        DELETE sh FROM STOK_HAREKET sh
                        INNER JOIN STOK_DURUM sd ON sh.stok_id = sd.stok_id
                        INNER JOIN DEPO_ESLESTIRME de ON sd.depo_eslestirme_id = de.id
                        WHERE de.depo_id = {0}", id);
                    
                    // Bu depoya ait tüm stok durumlarını sil
                    await _context.Database.ExecuteSqlRawAsync(@"
                        DELETE sd FROM STOK_DURUM sd
                        INNER JOIN DEPO_ESLESTIRME de ON sd.depo_eslestirme_id = de.id
                        WHERE de.depo_id = {0}", id);
                    
                    // Bu depoya bağlı tüm alt depoları sil
                    await _context.Database.ExecuteSqlRawAsync(@"
                        DELETE ad FROM ALT_DEPO ad
                        INNER JOIN DEPO_ESLESTIRME de ON ad.id = de.alt_depo_id
                        WHERE de.depo_id = {0}", id);
                    
                    // Bu depoya ait tüm eşleştirmeleri sil
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM DEPO_ESLESTIRME WHERE depo_id = {0}", id);
                    
                    // Son olarak depoyu sil
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM DEPO WHERE id = {0}", id);
                    
                    // Constraint'leri tekrar aktif et
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE STOK_HAREKET WITH CHECK CHECK CONSTRAINT ALL");
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE STOK_DURUM WITH CHECK CHECK CONSTRAINT ALL");
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE DEPO_ESLESTIRME WITH CHECK CHECK CONSTRAINT ALL");
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE ALT_DEPO WITH CHECK CHECK CONSTRAINT ALL");
                    
                    TempData["SuccessMessage"] = $"'{depo.DepoAdi}' depo ve tüm bağlı kayıtlar başarıyla silindi.";
                }
                catch (Exception ex)
                {
                    // Hata durumunda constraint'leri tekrar aktif etmeye çalış
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "ALTER TABLE STOK_HAREKET WITH CHECK CHECK CONSTRAINT ALL");
                        await _context.Database.ExecuteSqlRawAsync(
                            "ALTER TABLE STOK_DURUM WITH CHECK CHECK CONSTRAINT ALL");
                        await _context.Database.ExecuteSqlRawAsync(
                            "ALTER TABLE DEPO_ESLESTIRME WITH CHECK CHECK CONSTRAINT ALL");
                        await _context.Database.ExecuteSqlRawAsync(
                            "ALTER TABLE ALT_DEPO WITH CHECK CHECK CONSTRAINT ALL");
                    }
                    catch { /* Constraint'leri tekrar aktif etmeye çalış */ }
                    
                    TempData["ErrorMessage"] = $"Silme işlemi sırasında hata oluştu: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportToExcel(string searchString)
        {
            var depolarQuery = _context.Depos.AsQueryable();
            
            // Tüm depoları export et (aktif ve pasif)
            if (!String.IsNullOrEmpty(searchString))
            {
                depolarQuery = depolarQuery.Where(d => d.DepoAdi.Contains(searchString));
            }
            var data = await depolarQuery.AsNoTracking().ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Depolar");
                worksheet.Cell(1, 1).Value = "Depo Adı";
                worksheet.Cell(1, 2).Value = "Durum";
                worksheet.Row(1).Style.Font.Bold = true;

                var currentRow = 2;
                foreach (var item in data)
                {
                    worksheet.Cell(currentRow, 1).Value = item.DepoAdi;
                    worksheet.Cell(currentRow, 2).Value = item.Statu ? "Aktif" : "Pasif";
                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"Depolar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        private bool DepoExists(int id)
        {
            return _context.Depos.Any(e => e.Id == id);
        }
    }
}