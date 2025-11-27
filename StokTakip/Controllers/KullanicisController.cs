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
    [Authorize(Roles = "Admin,Depo Yetkilisi,Rapor Kullanıcısı")]
    public class KullanicisController : Controller
    {
        private readonly StokTakipBgcContext _context;

        public KullanicisController(StokTakipBgcContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, int? kullaniciTipId, bool showOnlyActive = false)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["ShowOnlyActive"] = showOnlyActive;
            var kullanicilarQuery = _context.Kullanicis
                .Include(k => k.KulTipNavigation)
                .AsQueryable();
            
            if (showOnlyActive)
            {
                kullanicilarQuery = kullanicilarQuery.Where(k => k.Statu);
            }
            
            if (!string.IsNullOrEmpty(searchString))
            {
                kullanicilarQuery = kullanicilarQuery.Where(k => k.KulUsername.Contains(searchString) ||
                                                                k.KulAd.Contains(searchString) ||
                                                                k.KulSoyad.Contains(searchString));
            }
            
            if (kullaniciTipId.HasValue)
            {
                kullanicilarQuery = kullanicilarQuery.Where(k => k.KulTip == kullaniciTipId.Value);
            }
            
            // ViewBag'e gerekli verileri ekle
            ViewBag.KullaniciTips = await _context.KullaniciTips.Where(kt => kt.Statu).ToListAsync();
            
            return View(await kullanicilarQuery.AsNoTracking().ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var kullanici = await _context.Kullanicis.Include(k => k.KulTipNavigation).Include(k => k.OlusturanKullaniciNavigation).Include(k => k.GuncelleyenKullaniciNavigation).FirstOrDefaultAsync(m => m.Id == id);
            if (kullanici == null) return NotFound();
            return View(kullanici);
        }

        public IActionResult Create()
        {
            ViewData["KulTip"] = new SelectList(_context.KullaniciTips.Where(kt => kt.Statu), "Id", "KultipAdi");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("KulUsername,KulSifre,KulAd,KulSoyad,KulTip,Statu")] Kullanici kullanici)
        {
            ModelState.Remove("KulTipNavigation");
            if (ModelState.IsValid)
            {
                kullanici.OlusturmaTarihi = DateTime.Now;
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdString))
                {
                    kullanici.OlusturanKullanici = int.Parse(userIdString);
                }
                _context.Add(kullanici);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["KulTip"] = new SelectList(_context.KullaniciTips.Where(kt => kt.Statu), "Id", "KultipAdi", kullanici.KulTip);
            return View(kullanici);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var kullanici = await _context.Kullanicis.FindAsync(id);
            if (kullanici == null) return NotFound();
            ViewData["KulTip"] = new SelectList(_context.KullaniciTips.Where(kt => kt.Statu), "Id", "KultipAdi", kullanici.KulTip);
            return View(kullanici);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,KulUsername,KulAd,KulSoyad,KulTip,Statu")] Kullanici kullaniciFromForm)
        {
            if (id != kullaniciFromForm.Id) return NotFound();
            ModelState.Remove("KulSifre");
            ModelState.Remove("KulTipNavigation");
            if (ModelState.IsValid)
            {
                try
                {
                    var userFromDb = await _context.Kullanicis.FindAsync(id);
                    if (userFromDb == null) return NotFound();
                    userFromDb.KulUsername = kullaniciFromForm.KulUsername;
                    userFromDb.KulAd = kullaniciFromForm.KulAd;
                    userFromDb.KulSoyad = kullaniciFromForm.KulSoyad;
                    userFromDb.KulTip = kullaniciFromForm.KulTip;
                    userFromDb.Statu = kullaniciFromForm.Statu;
                    userFromDb.GuncellemeTarihi = DateTime.Now;
                    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userIdString))
                    {
                        userFromDb.GuncelleyenKullanici = int.Parse(userIdString);
                    }
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KullaniciExists(kullaniciFromForm.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["KulTip"] = new SelectList(_context.KullaniciTips.Where(kt => kt.Statu), "Id", "KultipAdi", kullaniciFromForm.KulTip);
            return View(kullaniciFromForm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int Id, string YeniSifre)
        {
            var userFromDb = await _context.Kullanicis.FindAsync(Id);
            if (userFromDb == null) return NotFound();
            if (string.IsNullOrWhiteSpace(YeniSifre))
            {
                TempData["PasswordError"] = "Yeni şifre boş olamaz.";
                return RedirectToAction("Edit", new { id = Id });
            }
            userFromDb.KulSifre = YeniSifre;
            userFromDb.GuncellemeTarihi = DateTime.Now;
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdString))
            {
                userFromDb.GuncelleyenKullanici = int.Parse(userIdString);
            }
            await _context.SaveChangesAsync();
            TempData["PasswordSuccess"] = "Şifre başarıyla güncellendi.";
            return RedirectToAction("Edit", new { id = Id });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var kullanici = await _context.Kullanicis.Include(k => k.KulTipNavigation).FirstOrDefaultAsync(m => m.Id == id);
            if (kullanici == null) return NotFound();
            return View(kullanici);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kullanici = await _context.Kullanicis.FindAsync(id);
            if (kullanici != null)
            {
                // Önce bu kullanıcıyı kullanan tüm stok hareketlerini sil
                var stokHareketleri = await _context.StokHarekets
                    .Where(sh => sh.OlusturanKullanici == id || sh.GuncelleyenKullanici == id)
                    .ToListAsync();
                _context.StokHarekets.RemoveRange(stokHareketleri);
                
                // Bu kullanıcıyı kullanan tüm stok durumlarını sil
                var stokDurumlari = await _context.StokDurums
                    .Where(sd => sd.OlusturanKullanici == id || sd.GuncelleyenKullanici == id)
                    .ToListAsync();
                _context.StokDurums.RemoveRange(stokDurumlari);
                
                // Bu kullanıcıyı kullanan tüm stokları sil
                var stoklar = await _context.Stoks
                    .Where(s => s.OlusturanKullanici == id || s.GuncelleyenKullanici == id)
                    .ToListAsync();
                
                foreach (var stok in stoklar)
                {
                    // Stok ile ilgili tüm stok hareketlerini sil
                    var stokHareketleri2 = await _context.StokHarekets
                        .Where(sh => sh.StokId == stok.Id)
                        .ToListAsync();
                    _context.StokHarekets.RemoveRange(stokHareketleri2);
                    
                    // Stok durumlarını sil
                    var stokDurumlar = await _context.StokDurums
                        .Where(sd => sd.StokId == stok.Id)
                        .ToListAsync();
                    _context.StokDurums.RemoveRange(stokDurumlar);
                    
                    // Stoku sil
                    _context.Stoks.Remove(stok);
                }
                
                // Bu kullanıcıyı kullanan tüm alt depoları sil
                var altDepolar = await _context.AltDepos
                    .Where(ad => ad.OlusturanKullanici == id || ad.GuncelleyenKullanici == id)
                    .ToListAsync();
                
                foreach (var altDepo in altDepolar)
                {
                    // Alt depo ile ilgili tüm eşleştirmeleri sil
                    var eslestirmeler = await _context.DepoEslestirmes
                        .Where(de => de.AltDepoId == altDepo.Id)
                        .ToListAsync();
                    _context.DepoEslestirmes.RemoveRange(eslestirmeler);
                    
                    // Alt depoyu sil
                    _context.AltDepos.Remove(altDepo);
                }
                
                // Bu kullanıcıyı kullanan tüm depoları sil
                var depolar = await _context.Depos
                    .Where(d => d.OlusturanKullanici == id || d.GuncelleyenKullanici == id)
                    .ToListAsync();
                
                foreach (var depo in depolar)
                {
                    // Depo ile ilgili tüm eşleştirmeleri sil
                    var eslestirmeler = await _context.DepoEslestirmes
                        .Where(de => de.DepoId == depo.Id)
                        .ToListAsync();
                    _context.DepoEslestirmes.RemoveRange(eslestirmeler);
                    
                    // Depoyu sil
                    _context.Depos.Remove(depo);
                }
                
                // Bu kullanıcıyı kullanan tüm hareket tiplerini sil
                var hareketTipleri = await _context.HareketTips
                    .Where(ht => ht.OlusturanKullanici == id || ht.GuncelleyenKullanici == id)
                    .ToListAsync();
                
                foreach (var hareketTip in hareketTipleri)
                {
                    // Hareket tipi ile ilgili tüm stok hareketlerini sil
                    var stokHareketleri3 = await _context.StokHarekets
                        .Where(sh => sh.HareketTip == hareketTip.Id)
                        .ToListAsync();
                    _context.StokHarekets.RemoveRange(stokHareketleri3);
                    
                    // Hareket tipini sil
                    _context.HareketTips.Remove(hareketTip);
                }
                
                // Bu kullanıcıyı kullanan tüm kullanıcı tiplerini sil
                var kullaniciTipleri = await _context.KullaniciTips
                    .Where(kt => kt.OlusturanKullanici == id || kt.GuncelleyenKullanici == id)
                    .ToListAsync();
                
                foreach (var kullaniciTip in kullaniciTipleri)
                {
                    // Kullanıcı tipi ile ilgili tüm kullanıcıları sil
                    var kullanicilar = await _context.Kullanicis
                        .Where(k => k.KulTip == kullaniciTip.Id)
                        .ToListAsync();
                    
                    foreach (var k in kullanicilar)
                    {
                        // Kullanıcı ile ilgili tüm kayıtları sil (recursive)
                        var stokHareketleri4 = await _context.StokHarekets
                            .Where(sh => sh.OlusturanKullanici == k.Id || sh.GuncelleyenKullanici == k.Id)
                            .ToListAsync();
                        _context.StokHarekets.RemoveRange(stokHareketleri4);
                        
                        var stokDurumlari2 = await _context.StokDurums
                            .Where(sd => sd.OlusturanKullanici == k.Id || sd.GuncelleyenKullanici == k.Id)
                            .ToListAsync();
                        _context.StokDurums.RemoveRange(stokDurumlari2);
                        
                        _context.Kullanicis.Remove(k);
                    }
                    
                    // Kullanıcı tipini sil
                    _context.KullaniciTips.Remove(kullaniciTip);
                }
                
                // Bu kullanıcıyı kullanan tüm ölçü birimlerini sil
                var olcuBirimleri = await _context.OlcuBirimis
                    .Where(ob => ob.OlusturanKullanici == id || ob.GuncelleyenKullanici == id)
                    .ToListAsync();
                
                foreach (var olcuBirimi in olcuBirimleri)
                {
                    // Ölçü birimi ile ilgili tüm stokları sil
                    var stoklar2 = await _context.Stoks
                        .Where(s => s.StokOlcuBirim == olcuBirimi.Id)
                        .ToListAsync();
                    
                    foreach (var stok in stoklar2)
                    {
                        var stokHareketleri5 = await _context.StokHarekets
                            .Where(sh => sh.StokId == stok.Id)
                            .ToListAsync();
                        _context.StokHarekets.RemoveRange(stokHareketleri5);
                        
                        var stokDurumlari3 = await _context.StokDurums
                            .Where(sd => sd.StokId == stok.Id)
                            .ToListAsync();
                        _context.StokDurums.RemoveRange(stokDurumlari3);
                        
                        _context.Stoks.Remove(stok);
                    }
                    
                    // Ölçü birimini sil
                    _context.OlcuBirimis.Remove(olcuBirimi);
                }
                
                // Bu kullanıcıyı kullanan tüm sorumluları sil
                var sorumlular = await _context.Sorumlus
                    .Where(s => s.OlusturanKullanici == id || s.GuncelleyenKullanici == id)
                    .ToListAsync();
                
                foreach (var sorumlu in sorumlular)
                {
                    // Sorumlu ile ilgili tüm stok hareketlerini sil
                    var stokHareketleri6 = await _context.StokHarekets
                        .Where(sh => sh.SorumluId == sorumlu.Id)
                        .ToListAsync();
                    _context.StokHarekets.RemoveRange(stokHareketleri6);
                    
                    // Sorumluyu sil
                    _context.Sorumlus.Remove(sorumlu);
                }
                
                // Son olarak kullanıcıyı sil
                _context.Kullanicis.Remove(kullanici);
                
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ExportToExcel(string searchString, int? kullaniciTipId)
        {
            var kullanicilarQuery = _context.Kullanicis
                .Include(k => k.KulTipNavigation)
                .AsQueryable(); // Tüm kullanıcıları export et (aktif ve pasif)
            
            if (!string.IsNullOrEmpty(searchString))
            {
                kullanicilarQuery = kullanicilarQuery.Where(k => k.KulUsername.Contains(searchString) || k.KulAd.Contains(searchString) || k.KulSoyad.Contains(searchString));
            }
            
            if (kullaniciTipId.HasValue)
            {
                kullanicilarQuery = kullanicilarQuery.Where(k => k.KulTip == kullaniciTipId.Value);
            }
            var data = await kullanicilarQuery.AsNoTracking().ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Kullanıcılar");
                worksheet.Cell(1, 1).Value = "Kullanıcı Adı";
                worksheet.Cell(1, 2).Value = "Şifre";
                worksheet.Cell(1, 3).Value = "Adı";
                worksheet.Cell(1, 4).Value = "Soyadı";
                worksheet.Cell(1, 5).Value = "Kullanıcı Tipi";
                worksheet.Cell(1, 6).Value = "Durum";
                worksheet.Row(1).Style.Font.Bold = true;

                var currentRow = 2;
                foreach (var item in data)
                {
                    worksheet.Cell(currentRow, 1).Value = item.KulUsername;
                    worksheet.Cell(currentRow, 2).Value = item.KulSifre;
                    worksheet.Cell(currentRow, 3).Value = item.KulAd;
                    worksheet.Cell(currentRow, 4).Value = item.KulSoyad;
                    worksheet.Cell(currentRow, 5).Value = item.KulTipNavigation?.KultipAdi;
                    worksheet.Cell(currentRow, 6).Value = item.Statu ? "Aktif" : "Pasif";
                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"Kullanicilar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        private bool KullaniciExists(int id)
        {
            return _context.Kullanicis.Any(e => e.Id == id);
        }


        public async Task<IActionResult> Hareketler()
        {
            var stokHareketleri = await _context.StokHarekets
                                                .Include(sh => sh.OlusturanKullaniciNavigation)
                                                .Include(sh => sh.Stok)
                                                .Include(sh => sh.HareketTipNavigation)
                                                .OrderByDescending(sh => sh.HareketTarihi)
                                                .ToListAsync();
            return View(stokHareketleri);
        }
    }
}