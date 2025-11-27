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
    public class StoksController : Controller
    {
        private readonly StokTakipBgcContext _context;

        public StoksController(StokTakipBgcContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index(string stokAdi, int? depoId, bool showOnlyActive = false)
        {
            ViewData["StokAdi"] = stokAdi;
            ViewData["DepoId"] = depoId;
            ViewData["ShowOnlyActive"] = showOnlyActive;
            
            // TempData mesajlarını ViewBag'e ekle
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }
            
            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }

            // Depoları ViewBag'e ekle
            ViewBag.Depoes = await _context.Depos.Where(d => d.Statu).ToListAsync();

            // Stokları birleştirilmiş şekilde getir - AYNI İSİMLİ ÜRÜNLER TEK KARTTA
            var stokDurumlari = _context.StokDurums
                .Include(sd => sd.Stok).ThenInclude(s => s.StokOlcuBirimNavigation)
                .Include(sd => sd.DepoEslestirme).ThenInclude(de => de.Depo)
                .Include(sd => sd.DepoEslestirme).ThenInclude(de => de.AltDepo)
                .AsQueryable();

            if (showOnlyActive)
            {
                stokDurumlari = stokDurumlari.Where(s => s.Stok.Statu);
            }

            // Stok adına göre arama
            if (!string.IsNullOrEmpty(stokAdi))
            {
                stokDurumlari = stokDurumlari.Where(s =>
                    s.Stok.StokAd.Contains(stokAdi) ||
                    s.Stok.StokMarka.Contains(stokAdi)
                );
            }

            // Depo ID'sine göre arama
            if (depoId.HasValue)
            {
                stokDurumlari = stokDurumlari.Where(s => s.DepoEslestirme.DepoId == depoId.Value);
            }

            // Aynı isimli ürünleri grupla ve birleştir
            var gruplanmisStoklar = await stokDurumlari
                .AsNoTracking()
                .ToListAsync();

            // Aynı isimli, markalı ve aynı depodaki ürünleri grupla ve miktarları topla
            var birlestirilmisStoklar = gruplanmisStoklar
                .GroupBy(sd => new { 
                    sd.Stok.StokAd, 
                    sd.Stok.StokMarka, 
                    sd.Stok.StokOlcuBirimNavigation,
                    sd.DepoEslestirmeId,
                    sd.DepoEslestirme.DepoId,
                    sd.DepoEslestirme.Depo.DepoAdi,
                    sd.DepoEslestirme.AltDepoId,
                    sd.DepoEslestirme.AltDepo.AltDepoAdi
                })
                .Select(g => new StokDurum
                {
                    Id = g.First().Id,
                    StokId = g.First().StokId, // İlk stok ID'sini al
                    DepoEslestirmeId = g.Key.DepoEslestirmeId,
                    DurumMiktar = g.Sum(sd => sd.DurumMiktar), // Aynı depodaki aynı ürünlerin miktarlarını topla
                    OlusturmaTarihi = g.First().OlusturmaTarihi,
                    OlusturanKullanici = g.First().OlusturanKullanici,
                    GuncellemeTarihi = g.First().GuncellemeTarihi,
                    GuncelleyenKullanici = g.First().GuncelleyenKullanici,
                    Stok = g.First().Stok, // İlk stok bilgisini al
                    DepoEslestirme = g.First().DepoEslestirme
                })
                .OrderBy(sd => sd.Stok.StokAd)
                .ThenBy(sd => sd.Stok.StokMarka)
                .ThenBy(sd => sd.DepoEslestirme.Depo.DepoAdi)
                .ThenBy(sd => sd.DepoEslestirme.AltDepo.AltDepoAdi)
                .ToList();

            return View(birlestirilmisStoklar);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            
            // StokDurum ile birlikte stok bilgilerini getir
            var stokDurum = await _context.StokDurums
                .Include(sd => sd.Stok)
                    .ThenInclude(s => s.StokOlcuBirimNavigation)
                .Include(sd => sd.Stok)
                    .ThenInclude(s => s.OlusturanKullaniciNavigation)
                .Include(sd => sd.Stok)
                    .ThenInclude(s => s.GuncelleyenKullaniciNavigation)
                .Include(sd => sd.DepoEslestirme)
                    .ThenInclude(de => de.Depo)
                .Include(sd => sd.DepoEslestirme)
                    .ThenInclude(de => de.AltDepo)
                .FirstOrDefaultAsync(sd => sd.StokId == id);
                
            if (stokDurum?.Stok == null) return NotFound();
            
            // ViewBag'e gerçek miktarı ekle
            ViewBag.GerçekMiktar = stokDurum.DurumMiktar;
            
            return View(stokDurum.Stok);
        }



        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            
            // StokDurum ile birlikte stok bilgilerini getir
            var stokDurum = await _context.StokDurums
                .Include(sd => sd.Stok)
                    .ThenInclude(s => s.StokOlcuBirimNavigation)
                .FirstOrDefaultAsync(sd => sd.StokId == id);
                
            if (stokDurum?.Stok == null) return NotFound();
            
            var stok = stokDurum.Stok;
            
            // Kayıt tarihi için geçerli bir tarih kontrolü
            if (stok.KayitTarihi < new DateTime(1753, 1, 1))
            {
                stok.KayitTarihi = DateTime.Today;
            }
            
            // ViewBag'e gerçek miktarı ekle
            ViewBag.GerçekMiktar = stokDurum.DurumMiktar;
            
            // Stok modelindeki KayitMiktar'ı gerçek miktar ile güncelle
            stok.KayitMiktar = stokDurum.DurumMiktar;
            
            ViewBag.StokOlcuBirim = new SelectList(_context.OlcuBirimis.Where(ob => ob.Statu), "Id", "OlcuBirimAdi", stok.StokOlcuBirim);
            return View(stok);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Stok stok)
        {
            if (id != stok.Id) return NotFound();

            // Kayıt tarihi için geçerli bir tarih kontrolü
            if (stok.KayitTarihi < new DateTime(1753, 1, 1) || stok.KayitTarihi > new DateTime(9999, 12, 31))
            {
                stok.KayitTarihi = DateTime.Today;
            }

            // Navigation property'leri yükle
            var stokDurum = await _context.StokDurums
                .Include(sd => sd.Stok)
                    .ThenInclude(s => s.StokOlcuBirimNavigation)
                .FirstOrDefaultAsync(sd => sd.StokId == id);
            
            // StokOlcuBirimNavigation'ı doğru şekilde yükle
            if (stok.StokOlcuBirim > 0)
            {
                stok.StokOlcuBirimNavigation = await _context.OlcuBirimis
                    .FirstOrDefaultAsync(ob => ob.Id == stok.StokOlcuBirim);
            }

            // ViewBag'i her durumda doldur
            ViewBag.StokOlcuBirim = new SelectList(_context.OlcuBirimis.Where(ob => ob.Statu), "Id", "OlcuBirimAdi", stok.StokOlcuBirim);

            // StokOlcuBirimNavigation hatasını ModelState'den kaldır
            ModelState.Remove("StokOlcuBirimNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    // Mevcut stok kaydını bul
                    var existingStok = await _context.Stoks.FindAsync(id);
                    if (existingStok == null)
                    {
                        return NotFound();
                    }

                    // Sadece değiştirilebilir alanları güncelle
                    existingStok.StokAd = string.IsNullOrEmpty(stok.StokAd) ? "" : stok.StokAd;
                    existingStok.StokOlcuBirim = stok.StokOlcuBirim;
                    existingStok.StokMarka = string.IsNullOrEmpty(stok.StokMarka) ? "" : stok.StokMarka;
                    existingStok.StokDetay = string.IsNullOrEmpty(stok.StokDetay) ? "" : stok.StokDetay;
                    existingStok.Statu = stok.Statu;
                    existingStok.KayitMiktar = stok.KayitMiktar;
                    existingStok.KayitTarihi = stok.KayitTarihi;
                    existingStok.GuncellemeTarihi = DateTime.Now;
                    
                    // StokDurum tablosundaki DurumMiktar'ı da güncelle
                    if (stokDurum != null)
                    {
                        stokDurum.DurumMiktar = stok.KayitMiktar;
                        stokDurum.GuncellemeTarihi = DateTime.Now;
                        _context.StokDurums.Update(stokDurum);
                    }
                    
                    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userIdString))
                    {
                        existingStok.GuncelleyenKullanici = int.Parse(userIdString);
                    }

                    // Entity Framework'e değişiklikleri bildir
                    _context.Stoks.Update(existingStok);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Hata durumunda detaylı bilgi göster
                    string errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += " İç hata: " + ex.InnerException.Message;
                    }
                    ModelState.AddModelError("", $"Güncelleme sırasında hata oluştu: {errorMessage}");
                    return View(stok);
                }
            }
            else
            {
                // Model geçersizse view'ı döndür
                return View(stok);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var stok = await _context.Stoks
                .Include(s => s.StokOlcuBirimNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stok == null) return NotFound();
            return View(stok);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var stok = await _context.Stoks.FindAsync(id);
                if (stok == null)
                {
                    TempData["ErrorMessage"] = "Silinecek stok bulunamadı.";
                    return RedirectToAction(nameof(Index));
                }

                // İlişkili kayıtları kontrol et
                var hareketler = await _context.StokHarekets
                                              .Where(sh => sh.StokId == id)
                                              .ToListAsync();
                
                var stokDurumlari = await _context.StokDurums
                                                 .Where(sd => sd.StokId == id)
                                                 .ToListAsync();

                // Cascade delete ile otomatik silinecek, manuel silmeye gerek yok
                // _context.StokHarekets.RemoveRange(hareketler);
                // _context.StokDurums.RemoveRange(stokDurumlari);

                _context.Stoks.Remove(stok);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"'{stok.StokAd}' stok kaydı başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Hata detaylarını log'la
                System.Diagnostics.Debug.WriteLine($"Stok silme hatası: {ex.Message}");
                
                TempData["ErrorMessage"] = "Stok silinirken bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> ExportToExcel(string searchString)
        {
            var stokDurumlariQuery = _context.StokDurums
                .Include(sd => sd.Stok).ThenInclude(s => s.StokOlcuBirimNavigation)
                .Include(sd => sd.DepoEslestirme).ThenInclude(de => de.Depo)
                .Include(sd => sd.DepoEslestirme).ThenInclude(de => de.AltDepo)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                stokDurumlariQuery = stokDurumlariQuery.Where(s => s.Stok.StokAd.Contains(searchString) || s.Stok.StokMarka.Contains(searchString));
            }
            var data = await stokDurumlariQuery.AsNoTracking().ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Stok Durum Listesi");
                worksheet.Cell(1, 1).Value = "Ürün Adı";
                worksheet.Cell(1, 2).Value = "Marka";
                worksheet.Cell(1, 3).Value = "Ana Depo";
                worksheet.Cell(1, 4).Value = "Alt Depo";
                worksheet.Cell(1, 5).Value = "Miktar";
                worksheet.Cell(1, 6).Value = "Ölçü Birimi";
                worksheet.Cell(1, 7).Value = "Ürün Kartı Durumu";
                worksheet.Row(1).Style.Font.Bold = true;

                var currentRow = 2;
                foreach (var item in data)
                {
                    worksheet.Cell(currentRow, 1).Value = item.Stok.StokAd;
                    worksheet.Cell(currentRow, 2).Value = item.Stok.StokMarka;
                    worksheet.Cell(currentRow, 3).Value = item.DepoEslestirme.Depo.DepoAdi;
                    worksheet.Cell(currentRow, 4).Value = item.DepoEslestirme.AltDepo.AltDepoAdi;
                    worksheet.Cell(currentRow, 5).Value = item.DurumMiktar;
                    worksheet.Cell(currentRow, 6).Value = item.Stok.StokOlcuBirimNavigation.OlcuBirimAdi;
                    worksheet.Cell(currentRow, 7).Value = item.Stok.Statu ? "Aktif" : "Pasif";
                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"StokDurumListesi_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        private bool StokExists(int id)
        {
            return _context.Stoks.Any(e => e.Id == id);
        }

    }
}