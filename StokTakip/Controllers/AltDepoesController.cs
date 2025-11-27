using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class AltDepoesController : Controller
    {
        private readonly StokTakipBgcContext _context;

        public AltDepoesController(StokTakipBgcContext context)
        {
            _context = context;
        }

        // ... Index, Details, Create ve diğer metotlar aynı kalıyor ...
        public async Task<IActionResult> Index(string searchString, bool showOnlyActive = false)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ShowOnlyActive"] = showOnlyActive;
            var eslestirmelerQuery = _context.DepoEslestirmes
                .Include(de => de.AltDepo)
                .Include(de => de.Depo)
                .AsQueryable();
            
            if (showOnlyActive)
            {
                eslestirmelerQuery = eslestirmelerQuery.Where(a => a.AltDepo.Statu);
            }
            
            if (!string.IsNullOrEmpty(searchString))
            {
                eslestirmelerQuery = eslestirmelerQuery.Where(a => a.AltDepo.AltDepoAdi.Contains(searchString) || a.Depo.DepoAdi.Contains(searchString));
            }
            return View(await eslestirmelerQuery.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var depoEslestirme = await _context.DepoEslestirmes.Include(d => d.AltDepo).ThenInclude(u => u.OlusturanKullaniciNavigation).Include(d => d.AltDepo).ThenInclude(u => u.GuncelleyenKullaniciNavigation).Include(d => d.Depo).Include(d => d.OlusturanKullaniciNavigation).Include(d => d.GuncelleyenKullaniciNavigation).FirstOrDefaultAsync(m => m.Id == id);
            if (depoEslestirme == null) return NotFound();
            return View(depoEslestirme);
        }

        public IActionResult Create()
        {
            ViewBag.DepoId = new SelectList(_context.Depos.Where(d => d.Statu), "Id", "DepoAdi");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AltDepoAdi")] AltDepo altDepo, int DepoId)
        {
            ModelState.Remove("DepoEslestirmes");
            if (ModelState.IsValid)
            {
                var userId = GetCurrentUserId();
                altDepo.Statu = true;
                altDepo.OlusturmaTarihi = DateTime.Now;
                altDepo.OlusturanKullanici = userId;
                _context.Add(altDepo);
                await _context.SaveChangesAsync();
                var yeniEslestirme = new DepoEslestirme { DepoId = DepoId, AltDepoId = altDepo.Id, Statu = altDepo.Statu, OlusturmaTarihi = DateTime.Now, OlusturanKullanici = userId };
                _context.Add(yeniEslestirme);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.DepoId = new SelectList(_context.Depos.Where(d => d.Statu), "Id", "DepoAdi", DepoId);
            return View(altDepo);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var depoEslestirme = await _context.DepoEslestirmes.Include(d => d.AltDepo).FirstOrDefaultAsync(de => de.Id == id);
            if (depoEslestirme == null || depoEslestirme.AltDepo == null) return NotFound();
            var viewModel = new AltDepoEditViewModel
            {
                AltDepo = depoEslestirme.AltDepo,
                SeciliAnaDepoId = depoEslestirme.DepoId,
                AnaDepoListesi = new SelectList(_context.Depos.Where(d => d.Statu), "Id", "DepoAdi", depoEslestirme.DepoId)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AltDepoEditViewModel viewModel)
        {
            if (id != viewModel.AltDepo.Id) return NotFound();

            // --- HATA DÜZELTMESİ: Eksik olan ModelState.Remove komutları eklendi ---
            ModelState.Remove("AltDepo.DepoEslestirmes");
            ModelState.Remove("AltDepo.OlusturanKullaniciNavigation");
            ModelState.Remove("AltDepo.GuncelleyenKullaniciNavigation");
            ModelState.Remove("AnaDepoListesi"); // En önemli ekleme

            if (ModelState.IsValid)
            {
                try
                {
                    var userId = GetCurrentUserId();
                    var altDepoFromDb = await _context.AltDepos.FindAsync(viewModel.AltDepo.Id);
                    if (altDepoFromDb == null) return NotFound();
                    altDepoFromDb.AltDepoAdi = viewModel.AltDepo.AltDepoAdi;
                    altDepoFromDb.Statu = viewModel.AltDepo.Statu;
                    altDepoFromDb.GuncellemeTarihi = DateTime.Now;
                    altDepoFromDb.GuncelleyenKullanici = userId;
                    var eslestirmeFromDb = await _context.DepoEslestirmes.FirstOrDefaultAsync(de => de.AltDepoId == viewModel.AltDepo.Id);
                    if (eslestirmeFromDb != null)
                    {
                        eslestirmeFromDb.DepoId = viewModel.SeciliAnaDepoId;
                        eslestirmeFromDb.Statu = viewModel.AltDepo.Statu; // Alt depo durumunu DepoEslestirme'ye de yansıt
                        eslestirmeFromDb.GuncellemeTarihi = DateTime.Now;
                        eslestirmeFromDb.GuncelleyenKullanici = userId;
                    }
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.AltDepos.Any(e => e.Id == viewModel.AltDepo.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            viewModel.AnaDepoListesi = new SelectList(_context.Depos.Where(d => d.Statu), "Id", "DepoAdi", viewModel.SeciliAnaDepoId);
            return View(viewModel);
        }

        // ... Delete ve ExportToExcel metotları aynı kalıyor ...
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var depoEslestirme = await _context.DepoEslestirmes.Include(d => d.Depo).Include(d => d.AltDepo).FirstOrDefaultAsync(m => m.Id == id);
            if (depoEslestirme == null) return NotFound();
            return View(depoEslestirme);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var eslestirme = await _context.DepoEslestirmes
                .Include(de => de.AltDepo)
                .Include(de => de.Depo)
                .FirstOrDefaultAsync(de => de.Id == id);
                
            if (eslestirme != null)
            {
                try
                {
                    // Foreign key constraint'leri geçici olarak devre dışı bırak
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE STOK_HAREKET NOCHECK CONSTRAINT ALL");
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE STOK_DURUM NOCHECK CONSTRAINT ALL");
                    
                    // Bağlı kayıtları sil
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM STOK_HAREKET WHERE depo_eslestirme_id = {0}", id);
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM STOK_DURUM WHERE depo_eslestirme_id = {0}", id);
                    
                    // DepoEslestirme'yi sil
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM DEPO_ESLESTIRME WHERE id = {0}", id);
                    
                    // AltDepo'yu sil
                    if (eslestirme.AltDepo != null)
                    {
                        await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM ALT_DEPO WHERE id = {0}", eslestirme.AltDepo.Id);
                    }
                    
                    // Constraint'leri tekrar aktif et
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE STOK_HAREKET WITH CHECK CHECK CONSTRAINT ALL");
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE STOK_DURUM WITH CHECK CHECK CONSTRAINT ALL");
                    
                    TempData["SuccessMessage"] = $"'{eslestirme.AltDepo?.AltDepoAdi}' alt depo ve tüm bağlı kayıtlar başarıyla silindi.";
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
                    }
                    catch { /* Constraint'leri tekrar aktif etmeye çalış */ }
                    
                    TempData["ErrorMessage"] = $"Silme işlemi sırasında hata oluştu: {ex.Message}";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportToExcel(string searchString)
        {
            var eslestirmelerQuery = _context.DepoEslestirmes
                .Include(de => de.AltDepo)
                .Include(de => de.Depo)
                .AsQueryable(); // Tüm alt depoları export et (aktif ve pasif)
            
            if (!string.IsNullOrEmpty(searchString))
            {
                eslestirmelerQuery = eslestirmelerQuery.Where(a => a.AltDepo.AltDepoAdi.Contains(searchString) || a.Depo.DepoAdi.Contains(searchString));
            }
            var data = await eslestirmelerQuery.AsNoTracking().ToListAsync();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Alt Depolar");
                worksheet.Cell(1, 1).Value = "Alt Depo Adı";
                worksheet.Cell(1, 2).Value = "Bağlı Olduğu Ana Depo";
                worksheet.Cell(1, 3).Value = "Durum";
                worksheet.Row(1).Style.Font.Bold = true;
                var currentRow = 2;
                foreach (var item in data)
                {
                    worksheet.Cell(currentRow, 1).Value = item.AltDepo.AltDepoAdi;
                    worksheet.Cell(currentRow, 2).Value = item.Depo.DepoAdi;
                    worksheet.Cell(currentRow, 3).Value = item.AltDepo.Statu ? "Aktif" : "Pasif";
                    currentRow++;
                }
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"AltDepolar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(userIdString) ? null : (int?)int.Parse(userIdString);
        }
    }
}