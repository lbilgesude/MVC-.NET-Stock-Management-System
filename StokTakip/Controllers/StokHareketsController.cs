 using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StokTakip.Data;
using StokTakip.Models;
using StokTakip.Services;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace StokTakip.Controllers
{
    [Authorize(Roles = "Admin,Depo Yetkilisi")]
    public class StokHareketsController : Controller
    {
        private readonly StokTakipBgcContext _context;
        private readonly IStokDurumService _stokDurumService;
        private readonly IUyariService _uyariService;

        public StokHareketsController(StokTakipBgcContext context, IStokDurumService stokDurumService, IUyariService uyariService)
        {
            _context = context;
            _stokDurumService = stokDurumService;
            _uyariService = uyariService;
        }

        // --- Standard CRUD Metotları ---
        public async Task<IActionResult> Index()
        {
            var stokTakipBgcContext = _context.StokHarekets
                .Include(s => s.DepoEslestirme)
                .ThenInclude(de => de.Depo)
                .Include(s => s.DepoEslestirme)
                .ThenInclude(de => de.AltDepo)
                .Include(s => s.GuncelleyenKullaniciNavigation)
                .Include(s => s.HareketTipNavigation)
                .Include(s => s.OlusturanKullaniciNavigation)
                .Include(s => s.Sorumlu)
                .Include(s => s.Stok)
                .OrderByDescending(s => s.HareketTarihi); // En son hareketler önce görünsün
            
            // ViewBag'e gerekli verileri ekle
            ViewBag.HareketTips = await _context.HareketTips.Where(ht => ht.Statu).ToListAsync();
            ViewBag.Depoes = await _context.Depos.Where(d => d.Statu).ToListAsync();
            
            return View(await stokTakipBgcContext.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var stokHareket = await _context.StokHarekets
                .Include(s => s.DepoEslestirme)
                .Include(s => s.GuncelleyenKullaniciNavigation)
                .Include(s => s.HareketTipNavigation)
                .Include(s => s.OlusturanKullaniciNavigation)
                .Include(s => s.Sorumlu)
                .Include(s => s.Stok)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stokHareket == null) return NotFound();
            
            // Pasif ürünlerin detaylarına erişimi engelle
            if (stokHareket.Stok != null && !stokHareket.Stok.Statu)
            {
                return NotFound("Bu ürün pasif durumda olduğu için görüntülenemez.");
            }
            
            // Pasif depoların detaylarına erişimi engelle
            if (stokHareket.DepoEslestirme != null && 
                (stokHareket.DepoEslestirme.Depo != null && !stokHareket.DepoEslestirme.Depo.Statu) ||
                (stokHareket.DepoEslestirme.AltDepo != null && !stokHareket.DepoEslestirme.AltDepo.Statu))
            {
                return NotFound("Bu depo pasif durumda olduğu için görüntülenemez.");
            }
            
            return View(stokHareket);
        }

        // --- STOK GİRİŞİ BÖLÜMÜ ---
        public IActionResult Giris()
        {
            PopulateGirisDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Giris([Bind("DepoEslestirmeId,HareketMiktar,Aciklama,SorumluId,HareketTip")] StokHareket stokHareket, string urunAdi, string urunMarka, int? olcuBirimiId)
        {
            try
            {
                // ModelState'den gereksiz alanları kaldır
                ModelState.Remove("Stok");
                ModelState.Remove("StokId");
                ModelState.Remove("Sorumlu");
                ModelState.Remove("HareketTipNavigation");
                ModelState.Remove("DepoEslestirme");

                // Validasyon kontrolü
                if (string.IsNullOrEmpty(urunAdi))
                {
                    ModelState.AddModelError("", "Ürün adı gereklidir.");
                    PopulateGirisDropdowns(stokHareket);
                    return View(stokHareket);
                }

                if (stokHareket.DepoEslestirmeId <= 0)
                {
                    ModelState.AddModelError("", "Depo seçimi gereklidir.");
                    PopulateGirisDropdowns(stokHareket);
                    return View(stokHareket);
                }

                if (stokHareket.HareketMiktar <= 0)
                {
                    ModelState.AddModelError("", "Miktar 0'dan büyük olmalıdır.");
                    PopulateGirisDropdowns(stokHareket);
                    return View(stokHareket);
                }

                // Transaction ile tüm işlemleri yap
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                                         // Önce ürünü bul veya oluştur - AYNI İSİMLİ ÜRÜNLER TEK KARTTA
                     var stok = await _context.Stoks.FirstOrDefaultAsync(s => s.StokAd == urunAdi && s.StokMarka == urunMarka);
                     if (stok == null)
                     {
                         // Seçilen ölçü birimini kontrol et
                         OlcuBirimi secilenOlcuBirim = null;
                         if (olcuBirimiId.HasValue && olcuBirimiId.Value > 0)
                         {
                             secilenOlcuBirim = await _context.OlcuBirimis.FirstOrDefaultAsync(ob => ob.Id == olcuBirimiId.Value && ob.Statu);
                         }
                         
                         // Eğer seçilen ölçü birimi yoksa varsayılan ölçü birimini bul (Adet olarak)
                         if (secilenOlcuBirim == null)
                         {
                             secilenOlcuBirim = await _context.OlcuBirimis.FirstOrDefaultAsync(ob => ob.OlcuBirimAdi == "Adet");
                             if (secilenOlcuBirim == null)
                             {
                                 // Eğer "Adet" yoksa ilk aktif ölçü birimini al
                                 secilenOlcuBirim = await _context.OlcuBirimis.Where(ob => ob.Statu).FirstOrDefaultAsync();
                                 if (secilenOlcuBirim == null)
                                 {
                                     ModelState.AddModelError("", "Sistemde hiç ölçü birimi tanımlı değil. Lütfen önce ölçü birimi ekleyin.");
                                     PopulateGirisDropdowns(stokHareket);
                                     return View(stokHareket);
                                 }
                             }
                         }

                         // Yeni ürün oluştur - AYNI İSİMLİ ÜRÜNLER TEK KARTTA TUTULACAK
                         stok = new Stok
                         {
                             StokAd = urunAdi,
                             StokMarka = urunMarka ?? "",
                             StokDetay = "",
                             StokOlcuBirim = secilenOlcuBirim.Id, // Seçilen ölçü birimi
                             KayitMiktar = 0, // Başlangıçta 0 - toplam miktar StokDurum'da tutulacak
                             Statu = true,
                             KayitTarihi = DateTime.Now,
                             OlusturmaTarihi = DateTime.Now
                         };
                         _context.Add(stok);
                         await _context.SaveChangesAsync(); // StokId'yi almak için
                         Console.WriteLine($"✅ Yeni ürün oluşturuldu: {stok.StokAd} (ID: {stok.Id}) - Ölçü Birimi: {secilenOlcuBirim.OlcuBirimAdi}");
                     }
                     else
                     {
                         Console.WriteLine($"✅ Mevcut ürün bulundu: {stok.StokAd} (ID: {stok.Id})");
                     }

                    // StokHareket'i hazırla
                    stokHareket.StokId = stok.Id;
                    stokHareket.HareketTarihi = DateTime.Now;
                    stokHareket.OlusturmaTarihi = DateTime.Now;
                    
                    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userIdString))
                    {
                        stokHareket.OlusturanKullanici = int.Parse(userIdString);
                    }
                    else
                    {
                        stokHareket.OlusturanKullanici = 0; // Varsayılan değer
                    }

                    if (string.IsNullOrEmpty(stokHareket.Aciklama))
                    {
                        stokHareket.Aciklama = "";
                    }

                    // SorumluId null ise 0 olarak ayarla
                    if (stokHareket.SorumluId <= 0)
                    {
                        stokHareket.SorumluId = 0;
                    }

                    // StokHareket'i ekle
                    _context.Add(stokHareket);

                    // StokDurum'u güncelle veya oluştur
                    var stokDurum = await _context.StokDurums.FirstOrDefaultAsync(s => s.StokId == stok.Id && s.DepoEslestirmeId == stokHareket.DepoEslestirmeId);
                    if (stokDurum != null)
                    {
                        stokDurum.DurumMiktar += stokHareket.HareketMiktar;
                        stokDurum.GuncellemeTarihi = DateTime.Now;
                        _context.Update(stokDurum);
                    }
                    else
                    {
                        var yeniStokDurum = new StokDurum 
                        { 
                            StokId = stok.Id, 
                            DepoEslestirmeId = stokHareket.DepoEslestirmeId, 
                            DurumMiktar = stokHareket.HareketMiktar, 
                            OlusturmaTarihi = DateTime.Now, 
                            OlusturanKullanici = stokHareket.OlusturanKullanici ?? 0
                        };
                        _context.Add(yeniStokDurum);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    TempData["SuccessMessage"] = $"'{urunAdi}' ürünü başarıyla stok girişi yapıldı. Miktar: {stokHareket.HareketMiktar}";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw; // Üst catch bloğuna gönder
                }
            }
            catch (Exception ex)
            {
                // Detaylı hata bilgisi
                var errorMessage = "Stok girişi sırasında hata oluştu.";
                
                if (ex.InnerException != null)
                {
                    errorMessage += $" Detay: {ex.InnerException.Message}";
                }
                else
                {
                    errorMessage += $" Hata: {ex.Message}";
                }

                ModelState.AddModelError("", errorMessage);
                PopulateGirisDropdowns(stokHareket);
                return View(stokHareket);
            }
        }

        private void PopulateGirisDropdowns(StokHareket stokHareket = null)
        {
            ViewData["HareketTip"] = new SelectList(_context.HareketTips.Where(ht => ht.IslemGostergesi == true && ht.Statu), "Id", "HareketTipAdi", stokHareket?.HareketTip ?? 0);
            
            var depoListesi = _context.DepoEslestirmes
                .Include(d => d.Depo)
                .Include(a => a.AltDepo)
                .Where(d => d.Statu && d.Depo.Statu == true && d.AltDepo.Statu == true)
                .Select(d => new { Id = d.Id, Ad = d.Depo.DepoAdi + " - " + d.AltDepo.AltDepoAdi })
                .ToList();
            ViewData["DepoEslestirmeId"] = new SelectList(depoListesi, "Id", "Ad", stokHareket?.DepoEslestirmeId ?? 0);
            ViewData["SorumluId"] = new SelectList(_context.Sorumlus.Where(s => s.Statu), "Id", "SorumluAdi", stokHareket?.SorumluId ?? 0);
            ViewData["OlcuBirimiId"] = new SelectList(_context.OlcuBirimis.Where(ob => ob.Statu), "Id", "OlcuBirimAdi", stokHareket?.Stok?.StokOlcuBirim ?? 0);
        }


        // --- STOK ÇIKIŞI BÖLÜMÜ ---
        public IActionResult Cikis()
        {
            PopulateCikisDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cikis([Bind("DepoEslestirmeId,HareketMiktar,Aciklama,SorumluId,HareketTip")] StokHareket stokHareket, string urunAdi, string urunMarka)
        {
            Console.WriteLine($"🚀🚀🚀 STOK ÇIKIŞI BAŞLADI: Ürün={urunAdi}, Marka={urunMarka}, Miktar={stokHareket.HareketMiktar}");
            try
            {
                ModelState.Remove("Stok");
                ModelState.Remove("StokId");
                ModelState.Remove("Sorumlu");
                ModelState.Remove("HareketTipNavigation");
                ModelState.Remove("DepoEslestirme");

                                 Console.WriteLine($"🔍 ModelState.IsValid: {ModelState.IsValid}, urunAdi: '{urunAdi}'");
                                 if (ModelState.IsValid && !string.IsNullOrEmpty(urunAdi))
                 {
                     Console.WriteLine($"✅ Validation geçti, işlem devam ediyor");
                     // Önce ürünü bul
                     var stok = await _context.Stoks.FirstOrDefaultAsync(s => s.StokAd == urunAdi && s.StokMarka == urunMarka);
                     if (stok == null)
                     {
                         Console.WriteLine($"❌ Ürün bulunamadı: {urunAdi} - {urunMarka}");
                         ModelState.AddModelError("", "Bu ürün bulunamadı. Lütfen doğru ürün adını ve markasını girin.");
                         PopulateCikisDropdowns(stokHareket);
                         return View(stokHareket);
                     }
                     
                     Console.WriteLine($"✅ Ürün bulundu: {stok.StokAd} (ID: {stok.Id})");

                     // Ürünün durumu kontrolü - Pasif ürünlerden çıkış yapılamaz
                     if (!stok.Statu)
                     {
                         Console.WriteLine($"❌ Ürün pasif durumda: {stok.StokAd}");
                         ModelState.AddModelError("", "Bu ürün pasif durumda olduğu için çıkış işlemi yapılamaz. Lütfen ürünü aktif hale getirin.");
                         PopulateCikisDropdowns(stokHareket);
                         return View(stokHareket);
                     }

                     // Ürünün ölçü birimi kontrolü
                     if (stok.StokOlcuBirim <= 0)
                     {
                         ModelState.AddModelError("", "Bu ürünün ölçü birimi tanımlı değil. Lütfen ürünü tekrar ekleyin.");
                         PopulateCikisDropdowns(stokHareket);
                         return View(stokHareket);
                     }

                                         // FIFO ile stok çıkışı yap ve detayları al
                     Console.WriteLine($"🔄 FIFO işlemi başlatılıyor: StokId={stok.Id}, DepoEslestirmeId={stokHareket.DepoEslestirmeId}, Miktar={stokHareket.HareketMiktar}");
                     var (fifoBasarili, fifoDetaylar) = await UygulaFifoStokCikisi(stok.Id, stokHareket.DepoEslestirmeId, stokHareket.HareketMiktar);
                     if (!fifoBasarili)
                     {
                         Console.WriteLine($"❌ FIFO işlemi başarısız!");
                         ModelState.AddModelError("", "Seçilen depoda bu üründen yeterli stok bulunmamaktadır.");
                         PopulateCikisDropdowns(stokHareket);
                         return View(stokHareket);
                     }
                     
                     Console.WriteLine($"✅ FIFO işlemi başarılı, {fifoDetaylar.Count} detay");

                    stokHareket.StokId = stok.Id;
                    stokHareket.HareketTarihi = DateTime.Now;
                    stokHareket.OlusturmaTarihi = DateTime.Now;
                    
                    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userIdString))
                    {
                        stokHareket.OlusturanKullanici = int.Parse(userIdString);
                    }
                    else
                    {
                        stokHareket.OlusturanKullanici = 0; // Varsayılan değer
                    }

                    if (string.IsNullOrEmpty(stokHareket.Aciklama))
                    {
                        stokHareket.Aciklama = "";
                    }

                    // SorumluId null ise 0 olarak ayarla
                    if (stokHareket.SorumluId <= 0)
                    {
                        stokHareket.SorumluId = 0;
                    }

                                         // FIFO detaylarını kullanarak açıklama oluştur
                     var fifoAciklama = "FIFO Detayları: ";
                     foreach (var detay in fifoDetaylar)
                     {
                         fifoAciklama += $"{detay.GirisTarihi:dd.MM.yyyy HH:mm}'de girişi yapılan {detay.KullanilanMiktar} adet, ";
                     }
                     fifoAciklama = fifoAciklama.TrimEnd(',', ' ');
                     
                     // FIFO detaylarını açıklamaya ekle
                     stokHareket.Aciklama = $"{stokHareket.Aciklama} {fifoAciklama}".Trim();

                     _context.Add(stokHareket);
                     await _context.SaveChangesAsync();
                     Console.WriteLine($"💾 Stok hareketi kaydedildi");

                     // Minimum stok kontrolü için TempData ekle
                    var altDepo = await _context.DepoEslestirmes
                        .Include(de => de.AltDepo)
                        .Where(de => de.Id == stokHareket.DepoEslestirmeId)
                        .Select(de => de.AltDepo)
                        .FirstOrDefaultAsync();

                    if (altDepo != null)
                    {
                        var mevcutStok = await _context.StokDurums
                            .Where(sd => sd.StokId == stok.Id && sd.DepoEslestirmeId == stokHareket.DepoEslestirmeId)
                            .SumAsync(sd => sd.DurumMiktar);

                        // Sabit minimum stok miktarı kontrolü
                        const decimal MINIMUM_STOK_MIKTARI = 10;
                        Console.WriteLine($"🔍🔍🔍 STOK KONTROLÜ: Mevcut={mevcutStok}, Minimum={MINIMUM_STOK_MIKTARI}, AltDepo={altDepo.AltDepoAdi}");
                        if (mevcutStok < MINIMUM_STOK_MIKTARI)
                        {
                            Console.WriteLine($"⚠️⚠️⚠️ MİNİMUM STOK UYARISI TETİKLENDİ!");
                            TempData["MinimumStokUyarisi"] = $"Alt depo '{altDepo.AltDepoAdi}' için stok miktarı ({mevcutStok}) minimum değerin ({MINIMUM_STOK_MIKTARI}) altına düştü!";
                            TempData["AltDepoId"] = altDepo.Id;
                            
                            // Kalıcı tespit transfer uyarısı oluştur - AYNI BİLDİRİM
                            await KaliciMinimumStokUyarisiOlustur(stok, altDepo, mevcutStok, MINIMUM_STOK_MIKTARI);
                        }
                        else
                        {
                            Console.WriteLine($"✅ Stok yeterli, minimum uyarısı yok.");
                        }
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    Console.WriteLine($"❌ Validation başarısız!");
                    foreach (var error in ModelState)
                    {
                        if (error.Value.Errors.Any())
                        {
                            Console.WriteLine($"❌ {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                        }
                    }
                }

                PopulateCikisDropdowns(stokHareket);
                return View(stokHareket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Exception oluştu: {ex.Message}");
                Console.WriteLine($"💥 Stack trace: {ex.StackTrace}");
                // Detaylı hata bilgisi
                var errorMessage = "Stok çıkışı sırasında hata oluştu.";
                
                if (ex.InnerException != null)
                {
                    errorMessage += $" Detay: {ex.InnerException.Message}";
                }
                else
                {
                    errorMessage += $" Hata: {ex.Message}";
                }

                ModelState.AddModelError("", errorMessage);
                PopulateCikisDropdowns(stokHareket);
                return View(stokHareket);
            }
        }

        private void PopulateCikisDropdowns(StokHareket stokHareket = null)
        {
            ViewData["HareketTip"] = new SelectList(_context.HareketTips.Where(ht => ht.IslemGostergesi == false && ht.Statu), "Id", "HareketTipAdi", stokHareket?.HareketTip ?? 0);
            
            var depoListesi = _context.DepoEslestirmes
                .Include(d => d.Depo)
                .Include(a => a.AltDepo)
                .Where(d => d.Statu && d.Depo.Statu == true && d.AltDepo.Statu == true)
                .Select(d => new { Id = d.Id, Ad = d.Depo.DepoAdi + " - " + d.AltDepo.AltDepoAdi })
                .ToList();
            ViewData["DepoEslestirmeId"] = new SelectList(depoListesi, "Id", "Ad", stokHareket?.DepoEslestirmeId ?? 0);
            ViewData["SorumluId"] = new SelectList(_context.Sorumlus.Where(s => s.Statu), "Id", "SorumluAdi", stokHareket?.SorumluId ?? 0);
        }


        // --- STOK TRANSFERİ BÖLÜMÜ (GÜNCELLENDİ) ---
        public IActionResult Transfer()
        {
            PopulateTransferDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(string urunAdi, string urunMarka, int KaynakDepoId, int HedefDepoId, decimal HareketMiktar, int SorumluId, string Aciklama)
        {
            try
            {
                // Transfer hareket tiplerini isimlerine göre veritabanından bul
                var transferCikisTip = await _context.HareketTips.FirstOrDefaultAsync(h => h.HareketTipAdi == "Transfer Çıkış");
                var transferGirisTip = await _context.HareketTips.FirstOrDefaultAsync(h => h.HareketTipAdi == "Transfer Giriş");

                // Eğer bu tipler veritabanında tanımlı değilse, işlemi durdur ve hata ver
                if (transferCikisTip == null || transferGirisTip == null)
                {
                    ModelState.AddModelError("", "Sistemde 'Transfer Çıkış' ve 'Transfer Giriş' hareket tipleri tanımlı değil. Lütfen Tanımlamalar menüsünden bu tipleri oluşturun.");
                    PopulateTransferDropdowns(null, KaynakDepoId, HedefDepoId, SorumluId);
                    return View();
                }

                if (string.IsNullOrEmpty(urunAdi))
                {
                    ModelState.AddModelError("", "Ürün adı gereklidir.");
                    PopulateTransferDropdowns(null, KaynakDepoId, HedefDepoId, SorumluId);
                    return View();
                }

                if (KaynakDepoId == HedefDepoId)
                {
                    ModelState.AddModelError("", "Kaynak depo ile hedef depo aynı olamaz.");
                    PopulateTransferDropdowns(null, KaynakDepoId, HedefDepoId, SorumluId);
                    return View();
                }

                                 // Önce ürünü bul - AYNI İSİMLİ ÜRÜNLER TEK KARTTA
                 var stok = await _context.Stoks.FirstOrDefaultAsync(s => s.StokAd == urunAdi && s.StokMarka == urunMarka);
                 if (stok == null)
                 {
                     ModelState.AddModelError("", "Bu ürün bulunamadı. Lütfen doğru ürün adını ve markasını girin.");
                     PopulateTransferDropdowns(null, KaynakDepoId, HedefDepoId, SorumluId);
                     return View();
                 }
                 Console.WriteLine($"✅ Transfer için ürün bulundu: {stok.StokAd} (ID: {stok.Id})");

                 // Ürünün durumu kontrolü - Pasif ürünlerden transfer yapılamaz
                 if (!stok.Statu)
                 {
                     Console.WriteLine($"❌ Transfer için ürün pasif durumda: {stok.StokAd}");
                     ModelState.AddModelError("", "Bu ürün pasif durumda olduğu için transfer işlemi yapılamaz. Lütfen ürünü aktif hale getirin.");
                     PopulateTransferDropdowns(null, KaynakDepoId, HedefDepoId, SorumluId);
                     return View();
                 }

                 // Ürünün ölçü birimi kontrolü
                 if (stok.StokOlcuBirim <= 0)
                 {
                     ModelState.AddModelError("", "Bu ürünün ölçü birimi tanımlı değil. Lütfen ürünü tekrar ekleyin.");
                     PopulateTransferDropdowns(null, KaynakDepoId, HedefDepoId, SorumluId);
                     return View();
                 }

                                 // FIFO ile kaynak depodan çıkış yap ve detayları al
                 var (fifoBasarili, fifoDetaylar) = await UygulaFifoStokCikisi(stok.Id, KaynakDepoId, HareketMiktar);
                 if (!fifoBasarili)
                 {
                     ModelState.AddModelError("", "Kaynak depoda yeterli stok bulunmamaktadır.");
                     PopulateTransferDropdowns(null, KaynakDepoId, HedefDepoId, SorumluId);
                     return View();
                 }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                        int? olusturanKullaniciId = string.IsNullOrEmpty(userId) ? null : (int?)int.Parse(userId);

                        if (string.IsNullOrEmpty(Aciklama))
                        {
                            Aciklama = "";
                        }

                                                 // FIFO detaylarını kullanarak açıklama oluştur
                         var fifoAciklama = "FIFO Detayları: ";
                         foreach (var detay in fifoDetaylar)
                         {
                             fifoAciklama += $"{detay.GirisTarihi:dd.MM.yyyy HH:mm}'de girişi yapılan {detay.KullanilanMiktar} adet, ";
                         }
                         fifoAciklama = fifoAciklama.TrimEnd(',', ' ');

                         // Kaynak depodan çıkış hareketi
                         if (transferCikisTip != null)
                         {
                             _context.Add(new StokHareket 
                             { 
                                 StokId = stok.Id, 
                                 DepoEslestirmeId = KaynakDepoId, 
                                 HareketTip = transferCikisTip.Id, 
                                 HareketMiktar = HareketMiktar, 
                                 SorumluId = SorumluId, 
                                 Aciklama = $"Transfer -> {HedefDepoId}. Not: {Aciklama}. {fifoAciklama}", 
                                 HareketTarihi = DateTime.Now, 
                                 OlusturmaTarihi = DateTime.Now, 
                                 OlusturanKullanici = olusturanKullaniciId 
                             });
                         }

                                                 // Hedef depoya giriş hareketi
                         if (transferGirisTip != null)
                         {
                             _context.Add(new StokHareket 
                             { 
                                 StokId = stok.Id, 
                                 DepoEslestirmeId = HedefDepoId, 
                                 HareketTip = transferGirisTip.Id, 
                                 HareketMiktar = HareketMiktar, 
                                 SorumluId = SorumluId, 
                                 Aciklama = $"Transfer <- {KaynakDepoId}. Not: {Aciklama}. {fifoAciklama}", 
                                 HareketTarihi = DateTime.Now, 
                                 OlusturmaTarihi = DateTime.Now, 
                                 OlusturanKullanici = olusturanKullaniciId 
                             });
                         }

                        // Hedef depoya stok ekle
                        var hedefStokDurum = await _context.StokDurums.FirstOrDefaultAsync(s => s.StokId == stok.Id && s.DepoEslestirmeId == HedefDepoId);
                        if (hedefStokDurum != null)
                        {
                            hedefStokDurum.DurumMiktar += HareketMiktar;
                            _context.StokDurums.Update(hedefStokDurum);
                        }
                        else
                        {
                            _context.Add(new StokDurum 
                            { 
                                StokId = stok.Id, 
                                DepoEslestirmeId = HedefDepoId, 
                                DurumMiktar = HareketMiktar, 
                                OlusturmaTarihi = DateTime.Now, 
                                OlusturanKullanici = olusturanKullaniciId 
                            });
                        }

                        // Transfer sonrası minimum stok kontrolü - HEDEF DEPO (Transaction içinde)
                        Console.WriteLine($"🔍🔍🔍 TRANSFER HEDEF DEPO KONTROLÜ: HedefDepoId={HedefDepoId}");
                        var hedefAltDepo = await _context.DepoEslestirmes
                            .Include(de => de.AltDepo)
                            .Where(de => de.Id == HedefDepoId)
                            .Select(de => de.AltDepo)
                            .FirstOrDefaultAsync();

                        if (hedefAltDepo != null)
                        {
                            Console.WriteLine($"✅ Hedef alt depo bulundu: {hedefAltDepo.AltDepoAdi}");
                            var hedefMevcutStok = await _context.StokDurums
                                .Where(sd => sd.StokId == stok.Id && sd.DepoEslestirmeId == HedefDepoId)
                                .SumAsync(sd => sd.DurumMiktar);

                            // Sabit minimum stok miktarı kontrolü
                            const decimal MINIMUM_STOK_MIKTARI = 10;
                            Console.WriteLine($"🔍🔍🔍 HEDEF DEPO STOK KONTROLÜ: Mevcut={hedefMevcutStok}, Minimum={MINIMUM_STOK_MIKTARI}");
                            if (hedefMevcutStok < MINIMUM_STOK_MIKTARI)
                            {
                                Console.WriteLine($"⚠️⚠️⚠️ HEDEF DEPO MİNİMUM STOK UYARISI TETİKLENDİ!");
                                TempData["MinimumStokUyarisi"] = $"Alt depo '{hedefAltDepo.AltDepoAdi}' için stok miktarı ({hedefMevcutStok}) minimum değerin ({MINIMUM_STOK_MIKTARI}) altına düştü!";
                                TempData["AltDepoId"] = hedefAltDepo.Id;
                                
                                // Kalıcı tespit transfer uyarısı oluştur
                                Console.WriteLine($"🔔🔔🔔 HEDEF DEPO KALİCİ UYARI OLUŞTURULUYOR...");
                                await KaliciMinimumStokUyarisiOlustur(stok, hedefAltDepo, hedefMevcutStok, MINIMUM_STOK_MIKTARI);
                            }
                            else
                            {
                                Console.WriteLine($"✅ Hedef depo stok yeterli, minimum uyarısı yok.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"❌ Hedef alt depo bulunamadı: HedefDepoId={HedefDepoId}");
                        }
                        
                        // Transfer sonrası minimum stok kontrolü - KAYNAK DEPO (Transaction içinde)
                        Console.WriteLine($"🔍🔍🔍 TRANSFER KAYNAK DEPO KONTROLÜ: KaynakDepoId={KaynakDepoId}");
                        var kaynakAltDepo = await _context.DepoEslestirmes
                            .Include(de => de.AltDepo)
                            .Where(de => de.Id == KaynakDepoId)
                            .Select(de => de.AltDepo)
                            .FirstOrDefaultAsync();

                        if (kaynakAltDepo != null)
                        {
                            Console.WriteLine($"✅ Kaynak alt depo bulundu: {kaynakAltDepo.AltDepoAdi}");
                            var kaynakMevcutStok = await _context.StokDurums
                                .Where(sd => sd.StokId == stok.Id && sd.DepoEslestirmeId == KaynakDepoId)
                                .SumAsync(sd => sd.DurumMiktar);

                            // Sabit minimum stok miktarı kontrolü
                            const decimal MINIMUM_STOK_MIKTARI = 10;
                            Console.WriteLine($"🔍🔍🔍 KAYNAK DEPO STOK KONTROLÜ: Mevcut={kaynakMevcutStok}, Minimum={MINIMUM_STOK_MIKTARI}");
                            if (kaynakMevcutStok < MINIMUM_STOK_MIKTARI)
                            {
                                Console.WriteLine($"⚠️⚠️⚠️ KAYNAK DEPO MİNİMUM STOK UYARISI TETİKLENDİ!");
                                // Eğer hedef depoda da uyarı varsa, kaynak depo uyarısını öncelikle göster
                                if (TempData["MinimumStokUyarisi"] == null)
                                {
                                    TempData["MinimumStokUyarisi"] = $"Alt depo '{kaynakAltDepo.AltDepoAdi}' için stok miktarı ({kaynakMevcutStok}) minimum değerin ({MINIMUM_STOK_MIKTARI}) altına düştü!";
                                    TempData["AltDepoId"] = kaynakAltDepo.Id;
                                }
                                
                                // Kalıcı tespit transfer uyarısı oluştur (her durumda)
                                Console.WriteLine($"🔔🔔🔔 KAYNAK DEPO KALİCİ UYARI OLUŞTURULUYOR...");
                                await KaliciMinimumStokUyarisiOlustur(stok, kaynakAltDepo, kaynakMevcutStok, MINIMUM_STOK_MIKTARI);
                            }
                            else
                            {
                                Console.WriteLine($"✅ Kaynak depo stok yeterli, minimum uyarısı yok.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"❌ Kaynak alt depo bulunamadı: KaynakDepoId={KaynakDepoId}");
                        }

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        return RedirectToAction("Index", "Home");
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError("", "Transfer sırasında bir hata oluştu. İşlem geri alındı.");
                    }
                }


                PopulateTransferDropdowns(null, KaynakDepoId, HedefDepoId, SorumluId);
                return View();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Transfer sırasında bir hata oluştu: {ex.Message}");
                PopulateTransferDropdowns(null, KaynakDepoId, HedefDepoId, SorumluId);
                return View();
            }
        }

        private void PopulateTransferDropdowns(int? stokId = null, int? kaynakDepoId = null, int? hedefDepoId = null, int? sorumluId = null)
        {
            var depoListesi = _context.DepoEslestirmes
                .Include(d => d.Depo)
                .Include(a => a.AltDepo)
                .Where(d => d.Statu && d.Depo.Statu == true && d.AltDepo.Statu == true)
                .Select(d => new { Id = d.Id, Ad = d.Depo.DepoAdi + " - " + d.AltDepo.AltDepoAdi })
                .ToList();
            ViewData["KaynakDepoId"] = new SelectList(depoListesi, "Id", "Ad", kaynakDepoId ?? 0);
            ViewData["HedefDepoId"] = new SelectList(depoListesi, "Id", "Ad", hedefDepoId ?? 0);
            ViewData["SorumluId"] = new SelectList(_context.Sorumlus.Where(s => s.Statu), "Id", "SorumluAdi", sorumluId ?? 0);
        }

        public async Task<IActionResult> ExportToExcel()
        {
            var stokHarekets = await _context.StokHarekets
                .Include(s => s.DepoEslestirme)
                .ThenInclude(de => de.Depo)
                .Include(s => s.DepoEslestirme)
                .ThenInclude(de => de.AltDepo)
                .Include(s => s.HareketTipNavigation)
                .Include(s => s.Sorumlu)
                .Include(s => s.Stok)
                .OrderByDescending(s => s.HareketTarihi)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Stok Hareketleri");
                worksheet.Cell(1, 1).Value = "Stok Adı";
                worksheet.Cell(1, 2).Value = "Hareket Tipi";
                worksheet.Cell(1, 3).Value = "Miktar";
                worksheet.Cell(1, 4).Value = "Depo";
                worksheet.Cell(1, 5).Value = "Alt Depo";
                worksheet.Cell(1, 6).Value = "Sorumlu";
                worksheet.Cell(1, 7).Value = "Tarih";
                worksheet.Cell(1, 8).Value = "Açıklama";
                worksheet.Row(1).Style.Font.Bold = true;

                var currentRow = 2;
                foreach (var item in stokHarekets)
                {
                    worksheet.Cell(currentRow, 1).Value = item.Stok?.StokAd ?? "Belirtilmemiş";
                    worksheet.Cell(currentRow, 2).Value = item.HareketTipNavigation?.HareketTipAdi ?? "Belirtilmemiş";
                    worksheet.Cell(currentRow, 3).Value = item.HareketMiktar;
                    worksheet.Cell(currentRow, 4).Value = item.DepoEslestirme?.Depo?.DepoAdi ?? "Belirtilmemiş";
                    worksheet.Cell(currentRow, 5).Value = item.DepoEslestirme?.AltDepo?.AltDepoAdi ?? "Belirtilmemiş";
                    worksheet.Cell(currentRow, 6).Value = item.Sorumlu?.SorumluAdi ?? "Belirtilmemiş";
                    worksheet.Cell(currentRow, 7).Value = item.HareketTarihi.ToString("dd.MM.yyyy HH:mm");
                    worksheet.Cell(currentRow, 8).Value = item.Aciklama ?? "";
                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"StokHareketleri_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        private bool StokHareketExists(int id)
        {
            return _context.StokHarekets.Any(e => e.Id == id);
        }

        // --- CRUD METODLARI ---
        
        // Edit GET
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            
            var stokHareket = await _context.StokHarekets
                .Include(s => s.Stok)
                .Include(s => s.DepoEslestirme)
                .ThenInclude(de => de.Depo)
                .Include(s => s.DepoEslestirme)
                .ThenInclude(de => de.AltDepo)
                .Include(s => s.HareketTipNavigation)
                .Include(s => s.Sorumlu)
                .Include(s => s.OlusturanKullaniciNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (stokHareket == null) return NotFound();
            
            PopulateEditDropdowns(stokHareket);
            return View(stokHareket);
        }

        // Edit POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StokId,DepoEslestirmeId,SorumluId,HareketTip,Aciklama,HareketMiktar,HareketTarihi")] StokHareket stokHareket)
        {
            if (id != stokHareket.Id) return NotFound();
            
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    stokHareket.GuncelleyenKullanici = string.IsNullOrEmpty(userId) ? null : (int?)int.Parse(userId);
                    stokHareket.GuncellemeTarihi = DateTime.Now;
                    
                    _context.Update(stokHareket);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Stok hareketi başarıyla güncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StokHareketExists(stokHareket.Id))
                        return NotFound();
                    else
                        throw;
                }
            }
            
            PopulateEditDropdowns(stokHareket);
            return View(stokHareket);
        }

        // Delete GET
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            
            var stokHareket = await _context.StokHarekets
                .Include(s => s.Stok)
                .Include(s => s.DepoEslestirme)
                .ThenInclude(de => de.Depo)
                .Include(s => s.DepoEslestirme)
                .ThenInclude(de => de.AltDepo)
                .Include(s => s.HareketTipNavigation)
                .Include(s => s.Sorumlu)
                .Include(s => s.OlusturanKullaniciNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (stokHareket == null) return NotFound();
            
            return View(stokHareket);
        }

        // Delete POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var stokHareket = await _context.StokHarekets.FindAsync(id);
            if (stokHareket == null) return NotFound();
            
            _context.StokHarekets.Remove(stokHareket);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Stok hareketi başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateEditDropdowns(StokHareket stokHareket)
        {
            ViewData["StokId"] = new SelectList(_context.Stoks.Where(s => s.Statu), "Id", "StokAd", stokHareket.StokId);
            
            var depoListesi = _context.DepoEslestirmes
                .Include(d => d.Depo)
                .Include(a => a.AltDepo)
                .Where(d => d.Statu && d.Depo.Statu == true && d.AltDepo.Statu == true)
                .Select(d => new { Id = d.Id, Ad = d.Depo.DepoAdi + " - " + d.AltDepo.AltDepoAdi })
                .ToList();
            ViewData["DepoEslestirmeId"] = new SelectList(depoListesi, "Id", "Ad", stokHareket.DepoEslestirmeId);
            
            ViewData["SorumluId"] = new SelectList(_context.Sorumlus.Where(s => s.Statu), "Id", "SorumluAdi", stokHareket.SorumluId);
            ViewData["HareketTip"] = new SelectList(_context.HareketTips.Where(ht => ht.Statu), "Id", "HareketTipAdi", stokHareket.HareketTip);
            ViewData["OlusturanKullanici"] = new SelectList(_context.Kullanicis.Where(k => k.Statu), "Id", "KullaniciAdi", stokHareket.OlusturanKullanici);
            ViewData["GuncelleyenKullanici"] = new SelectList(_context.Kullanicis.Where(k => k.Statu), "Id", "KullaniciAdi", stokHareket.GuncelleyenKullanici);
        }

        // --- MİNİMUM STOK KONTROLÜ VE TRANSFER METODLARI ---

        // Minimum stok uyarılarını getir
        [HttpGet]
        public async Task<IActionResult> GetMinimumStokUyarilari()
        {
            var uyarilar = await _stokDurumService.GetMinimumStokUyarilariAsync();
            
            var uyariListesi = uyarilar.Select(u => new
            {
                stokAd = u.Stok?.StokAd ?? "N/A",
                altDepoAdi = u.DepoEslestirme?.AltDepo?.AltDepoAdi ?? "N/A",
                durumMiktar = u.DurumMiktar,
                minimumMiktar = 10,
                altDepoId = u.DepoEslestirme?.AltDepo?.Id ?? 0,
                guncellemeTarihi = u.GuncellemeTarihi?.ToString("dd.MM.yyyy HH:mm") ?? "N/A"
            }).ToList();
            
            return Json(new { success = true, uyarilar = uyariListesi });
        }

        // En fazla stoklu alt depoyu tespit et
        [HttpGet]
        public async Task<IActionResult> TespitEtAltDepo(int mevcutAltDepoId)
        {
            var enFazlaStokluAltDepo = await _stokDurumService.GetEnFazlaStokluAltDepoAsync(mevcutAltDepoId);
            
            if (enFazlaStokluAltDepo == null)
            {
                return Json(new { success = false, message = "Uygun alt depo bulunamadı." });
            }

            return Json(new { 
                success = true, 
                altDepo = new { 
                    id = enFazlaStokluAltDepo.Id, 
                    adi = enFazlaStokluAltDepo.AltDepoAdi 
                } 
            });
        }

        // Transfer sayfasını göster
        [HttpGet]
        public async Task<IActionResult> TespitTransfer(int kaynakAltDepoId, int hedefAltDepoId)
        {
            // Alt depo eşleştirmelerini bul
            var kaynakDepoEslestirme = await _context.DepoEslestirmes
                .Include(de => de.AltDepo)
                .FirstOrDefaultAsync(de => de.AltDepo.Id == kaynakAltDepoId);

            var hedefDepoEslestirme = await _context.DepoEslestirmes
                .Include(de => de.AltDepo)
                .FirstOrDefaultAsync(de => de.AltDepo.Id == hedefAltDepoId);

            if (kaynakDepoEslestirme == null || hedefDepoEslestirme == null)
            {
                return NotFound();
            }

            // Kaynak depodaki ürünleri getir
            var urunler = await _context.StokDurums
                .Include(sd => sd.Stok)
                .Where(sd => sd.DepoEslestirmeId == kaynakDepoEslestirme.Id && sd.DurumMiktar > 0)
                .ToListAsync();

            ViewBag.KaynakDepoEslestirme = kaynakDepoEslestirme;
            ViewBag.HedefDepoEslestirme = hedefDepoEslestirme;
            ViewBag.Urunler = urunler;

            return View();
        }

        // FIFO ile transfer yap
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TespitTransferYap(int kaynakDepoEslestirmeId, int hedefDepoEslestirmeId, int stokId, decimal miktar)
        {
            try
            {
                if (miktar <= 0)
                {
                    return Json(new { success = false, message = "Geçersiz miktar." });
                }

                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString))
                {
                    return Json(new { success = false, message = "Kullanıcı bilgisi bulunamadı." });
                }

                var kullaniciId = int.Parse(userIdString);

                // Ürünün durumunu kontrol et - Pasif ürünlerden tespit transfer yapılamaz
                var stok = await _context.Stoks.FindAsync(stokId);
                if (stok == null)
                {
                    return Json(new { success = false, message = "Ürün bulunamadı." });
                }

                if (!stok.Statu)
                {
                    Console.WriteLine($"❌ Tespit Transfer için ürün pasif durumda: {stok.StokAd}");
                    return Json(new { success = false, message = "Bu ürün pasif durumda olduğu için tespit transfer işlemi yapılamaz. Lütfen ürünü aktif hale getirin." });
                }

                // Önce kaynak depoda yeterli stok var mı kontrol et
                var kaynakStokDurum = await _context.StokDurums
                    .Where(sd => sd.StokId == stokId && sd.DepoEslestirmeId == kaynakDepoEslestirmeId)
                    .SumAsync(sd => sd.DurumMiktar);

                if (kaynakStokDurum < miktar)
                {
                    return Json(new { success = false, message = $"Kaynak depoda yeterli stok bulunmuyor. Mevcut: {kaynakStokDurum}, İstenen: {miktar}" });
                }

                // Transfer hareket tiplerini kontrol et
                var transferCikisTip = await _context.HareketTips.FirstOrDefaultAsync(h => h.HareketTipAdi == "Transfer Çıkış");
                var transferGirisTip = await _context.HareketTips.FirstOrDefaultAsync(h => h.HareketTipAdi == "Transfer Giriş");

                if (transferCikisTip == null || transferGirisTip == null)
                {
                    return Json(new { success = false, message = "Transfer hareket tipleri bulunamadı. Lütfen 'Transfer Çıkış' ve 'Transfer Giriş' tiplerini oluşturun." });
                }

                // Transaction ile tüm işlemleri yap
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                                         // FIFO ile transfer yap
                     var (basarili, fifoDetaylar) = await _stokDurumService.TransferStokFifoAsync(
                         kaynakDepoEslestirmeId, 
                         hedefDepoEslestirmeId, 
                         stokId, 
                         miktar, 
                         kullaniciId
                     );

                     if (!basarili)
                     {
                         await transaction.RollbackAsync();
                         return Json(new { success = false, message = "FIFO transfer işlemi başarısız oldu." });
                     }

                                            // İlk aktif sorumluyu bul
                        var ilkSorumlu = await _context.Sorumlus.Where(s => s.Statu).FirstOrDefaultAsync();
                        if (ilkSorumlu == null)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = "Sistemde hiç sorumlu tanımlı değil. Lütfen önce sorumlu ekleyin." });
                        }

                                             // FIFO detaylarını kullanarak açıklama oluştur
                     var fifoAciklama = "FIFO Detayları: ";
                     foreach (var detay in fifoDetaylar)
                     {
                         fifoAciklama += $"{detay.GirisTarihi:dd.MM.yyyy HH:mm}'de girişi yapılan {detay.KullanilanMiktar} adet, ";
                     }
                     fifoAciklama = fifoAciklama.TrimEnd(',', ' ');

                     // Transfer hareket kaydı oluştur
                     var kaynakHareket = new StokHareket
                     {
                         StokId = stokId,
                         DepoEslestirmeId = kaynakDepoEslestirmeId,
                         HareketTip = transferCikisTip.Id,
                         HareketMiktar = miktar,
                         Aciklama = $"Tespit Transfer -> {hedefDepoEslestirmeId}. {fifoAciklama}",
                         HareketTarihi = DateTime.Now,
                         OlusturmaTarihi = DateTime.Now,
                         OlusturanKullanici = kullaniciId,
                         SorumluId = ilkSorumlu.Id // İlk aktif sorumluyu kullan
                     };

                     var hedefHareket = new StokHareket
                     {
                         StokId = stokId,
                         DepoEslestirmeId = hedefDepoEslestirmeId,
                         HareketTip = transferGirisTip.Id,
                         HareketMiktar = miktar,
                         Aciklama = $"Tespit Transfer <- {kaynakDepoEslestirmeId}. {fifoAciklama}",
                         HareketTarihi = DateTime.Now,
                         OlusturmaTarihi = DateTime.Now,
                         OlusturanKullanici = kullaniciId,
                         SorumluId = ilkSorumlu.Id // İlk aktif sorumluyu kullan
                     };

                                         _context.Add(kaynakHareket);
                     _context.Add(hedefHareket);

                     // StokDurum tablosunda miktarları güncelle
                     // Kaynak depodan çıkış zaten FIFO ile yapıldı, sadece hedef depoya ekleme yap
                     var hedefStokDurum = await _context.StokDurums
                         .FirstOrDefaultAsync(sd => sd.StokId == stokId && sd.DepoEslestirmeId == hedefDepoEslestirmeId);

                     if (hedefStokDurum != null)
                     {
                         hedefStokDurum.DurumMiktar += miktar;
                         hedefStokDurum.GuncellemeTarihi = DateTime.Now;
                         _context.Update(hedefStokDurum);
                     }
                     else
                     {
                         var yeniStokDurum = new StokDurum
                         {
                             StokId = stokId,
                             DepoEslestirmeId = hedefDepoEslestirmeId,
                             DurumMiktar = miktar,
                             OlusturmaTarihi = DateTime.Now,
                             OlusturanKullanici = kullaniciId
                         };
                         _context.Add(yeniStokDurum);
                     }

                     await _context.SaveChangesAsync();
                     await transaction.CommitAsync();

                    // Transfer sonrası minimum stok kontrolü - HEDEF DEPO
                    var hedefAltDepo = await _context.DepoEslestirmes
                        .Include(de => de.AltDepo)
                        .Where(de => de.Id == hedefDepoEslestirmeId)
                        .Select(de => de.AltDepo)
                        .FirstOrDefaultAsync();

                    if (hedefAltDepo != null)
                    {
                        var hedefMevcutStok = await _context.StokDurums
                            .Where(sd => sd.StokId == stokId && sd.DepoEslestirmeId == hedefDepoEslestirmeId)
                            .SumAsync(sd => sd.DurumMiktar);

                        // Sabit minimum stok miktarı kontrolü
                        const decimal MINIMUM_STOK_MIKTARI = 10;
                        if (hedefMevcutStok < MINIMUM_STOK_MIKTARI)
                        {
                            // Kalıcı tespit transfer uyarısı oluştur
                            var hedefStok = await _context.Stoks.FindAsync(stokId);
                            if (hedefStok != null)
                            {
                                await KaliciMinimumStokUyarisiOlustur(hedefStok, hedefAltDepo, hedefMevcutStok, MINIMUM_STOK_MIKTARI);
                            }
                            
                            return Json(new { 
                                success = true, 
                                message = "Transfer başarıyla tamamlandı.",
                                uyari = $"Alt depo '{hedefAltDepo.AltDepoAdi}' için stok miktarı ({hedefMevcutStok}) minimum değerin ({MINIMUM_STOK_MIKTARI}) altına düştü!",
                                altDepoId = hedefAltDepo.Id
                            });
                        }
                    }

                    // Transfer sonrası minimum stok kontrolü - KAYNAK DEPO
                    var kaynakAltDepo = await _context.DepoEslestirmes
                        .Include(de => de.AltDepo)
                        .Where(de => de.Id == kaynakDepoEslestirmeId)
                        .Select(de => de.AltDepo)
                        .FirstOrDefaultAsync();

                    if (kaynakAltDepo != null)
                    {
                        var kaynakMevcutStok = await _context.StokDurums
                            .Where(sd => sd.StokId == stokId && sd.DepoEslestirmeId == kaynakDepoEslestirmeId)
                            .SumAsync(sd => sd.DurumMiktar);

                        // Sabit minimum stok miktarı kontrolü
                        const decimal MINIMUM_STOK_MIKTARI = 10;
                        if (kaynakMevcutStok < MINIMUM_STOK_MIKTARI)
                        {
                            // Kalıcı tespit transfer uyarısı oluştur
                            var kaynakStok = await _context.Stoks.FindAsync(stokId);
                            if (kaynakStok != null)
                            {
                                await KaliciMinimumStokUyarisiOlustur(kaynakStok, kaynakAltDepo, kaynakMevcutStok, MINIMUM_STOK_MIKTARI);
                            }
                            
                            return Json(new { 
                                success = true, 
                                message = "Transfer başarıyla tamamlandı.",
                                uyari = $"Alt depo '{kaynakAltDepo.AltDepoAdi}' için stok miktarı ({kaynakMevcutStok}) minimum değerin ({MINIMUM_STOK_MIKTARI}) altına düştü!",
                                altDepoId = kaynakAltDepo.Id
                            });
                        }
                    }

                    return Json(new { success = true, message = "Transfer başarıyla tamamlandı." });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw; // Üst catch bloğuna gönder
                }
            }
            catch (Exception ex)
            {
                // Detaylı hata bilgisi
                var errorMessage = "Transfer sırasında hata oluştu.";
                
                if (ex.InnerException != null)
                {
                    errorMessage += $" Detay: {ex.InnerException.Message}";
                }
                else
                {
                    errorMessage += $" Hata: {ex.Message}";
                }

                return Json(new { success = false, message = errorMessage });
            }
        }

        // Stok çıkış ve transfer işlemlerinde FIFO uygula
        private async Task<(bool success, List<FifoDetay> fifoDetaylar)> UygulaFifoStokCikisi(int stokId, int depoEslestirmeId, decimal miktar)
        {
            var fifoDetaylar = new List<FifoDetay>();
            var fifoStokListesi = await _stokDurumService.GetFifoStokListesiAsync(stokId, depoEslestirmeId, miktar);
            
            if (fifoStokListesi.Sum(sd => sd.DurumMiktar) < miktar)
            {
                return (false, fifoDetaylar); // Yeterli stok yok
            }

            decimal kalanMiktar = miktar;

            foreach (var fifoStok in fifoStokListesi)
            {
                if (kalanMiktar <= 0) break;

                var stokDurum = await _context.StokDurums.FindAsync(fifoStok.Id);
                if (stokDurum == null) continue;

                var kullanilacakMiktar = Math.Min(stokDurum.DurumMiktar, kalanMiktar);
                
                // FIFO detayını kaydet
                fifoDetaylar.Add(new FifoDetay
                {
                    GirisTarihi = stokDurum.OlusturmaTarihi ?? DateTime.Now,
                    KullanilanMiktar = kullanilacakMiktar
                });
                
                stokDurum.DurumMiktar -= kullanilacakMiktar;
                kalanMiktar -= kullanilacakMiktar;

                if (stokDurum.DurumMiktar <= 0)
                {
                    _context.StokDurums.Remove(stokDurum);
                }
                else
                {
                    _context.StokDurums.Update(stokDurum);
                }
            }

            return (true, fifoDetaylar);
        }

        // Kalıcı minimum stok uyarısı oluştur - YUKARIYA DÜŞEN BİLDİRİMİN AYNISI
        private async Task KaliciMinimumStokUyarisiOlustur(Stok stok, AltDepo altDepo, decimal mevcutStok, decimal minimumStok)
        {
            try
            {
                Console.WriteLine($"🔔🔔🔔 KALİCİ UYARI OLUŞTURULUYOR: {altDepo.AltDepoAdi}");
                // AYNI BİLDİRİMİ oluştur - Alt depo ID'si ile
                var uyari = new UyariModel
                {
                    Baslik = "⚠️ Minimum Stok Uyarısı!",
                    Mesaj = $"Alt depo '{altDepo.AltDepoAdi}' için stok miktarı ({mevcutStok}) minimum değerin ({minimumStok}) altına düştü! ALTDEPO_ID:{altDepo.Id}",
                    Tip = UyariTipi.Uyari,
                    AksiyonUrl = null, // Özel aksiyon butonları JavaScript ile eklenecek
                    AksiyonText = null
                };
                
                await _uyariService.AddUyariAsync(uyari);
                Console.WriteLine($"✅✅✅ KALİCİ MİNİMUM STOK UYARISI OLUŞTURULDU: {uyari.Baslik}");
            }
            catch (Exception ex)
            {
                // Hata durumunda log'la ama işlemi durdurma
                Console.WriteLine($"❌❌❌ Kalıcı minimum stok uyarısı oluşturulurken hata: {ex.Message}");
            }
        }
    }
}