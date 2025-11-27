using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokTakip.Data;
using StokTakip.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StokTakip.Controllers
{
    [Authorize(Roles = "Admin,Rapor Kullanıcısı")]
    public class RaporController : Controller
    {
        private readonly StokTakipBgcContext _context;

        public RaporController(StokTakipBgcContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var hareketlerQuery = _context.StokHarekets
                .Include(h => h.Stok)
                .Include(h => h.HareketTipNavigation)
                .Include(h => h.Sorumlu)
                .Include(h => h.OlusturanKullaniciNavigation)
                .Include(h => h.DepoEslestirme)
                    .ThenInclude(de => de.Depo)
                .Include(h => h.DepoEslestirme)
                    .ThenInclude(de => de.AltDepo)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                hareketlerQuery = hareketlerQuery.Where(h =>
                    (h.Stok != null && h.Stok.StokAd.Contains(searchString)) ||
                    (h.HareketTipNavigation != null && h.HareketTipNavigation.HareketTipAdi.Contains(searchString)) ||
                    (h.Sorumlu != null && h.Sorumlu.SorumluAdi.Contains(searchString)) ||
                    (h.DepoEslestirme != null && h.DepoEslestirme.Depo != null && h.DepoEslestirme.Depo.DepoAdi.Contains(searchString)) ||
                    (h.DepoEslestirme != null && h.DepoEslestirme.AltDepo != null && h.DepoEslestirme.AltDepo.AltDepoAdi.Contains(searchString)) ||
                    (h.OlusturanKullaniciNavigation != null && h.OlusturanKullaniciNavigation.KulUsername.Contains(searchString))
                );
            }

            var hareketler = await hareketlerQuery.OrderByDescending(h => h.HareketTarihi).ToListAsync();
            return View(hareketler);
        }

        public async Task<IActionResult> GrafikselRaporlar()
        {
            Console.WriteLine("GrafikselRaporlar metodu çağrıldı");
            
            try
            {
                // Her zaman örnek veri oluştur (test için)
                Console.WriteLine("Örnek veri oluşturuluyor...");
                var viewModel = GenerateSampleViewModel();
                
                Console.WriteLine($"ViewModel oluşturuldu - AylikCikislar: {viewModel.AylikCikislar?.Count ?? 0}");
                Console.WriteLine($"ViewModel oluşturuldu - AylikGirisler: {viewModel.AylikGirisler?.Count ?? 0}");
                Console.WriteLine($"ViewModel oluşturuldu - HareketTipiDagilimi: {viewModel.HareketTipiDagilimi?.Count ?? 0}");
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata oluştu: {ex.Message}");
                // Hata durumunda örnek veri oluştur
                var errorViewModel = GenerateSampleViewModel();
                return View(errorViewModel);
            }
        }

        // Örnek veri oluşturma metodları
        private List<ChartData> GenerateSampleMonthlyData(string type)
        {
            var random = new Random();
            var data = new List<ChartData>();
            var currentDate = DateTime.Now.AddMonths(-11);
            
            for (int i = 0; i < 12; i++)
            {
                var value = type switch
                {
                    "Giriş" => random.Next(50, 200),
                    "Çıkış" => random.Next(30, 150),
                    "Transfer" => random.Next(10, 80),
                    _ => random.Next(20, 100)
                };
                
                data.Add(new ChartData
                {
                    Label = currentDate.ToString("yyyy-MM"),
                    Value = value
                });
                currentDate = currentDate.AddMonths(1);
            }
            return data;
        }

        private List<ChartData> GenerateSampleProductData(string type)
        {
            var products = new[] { "Ürün A", "Ürün B", "Ürün C", "Ürün D", "Ürün E" };
            var random = new Random();
            return products.Select(p => new ChartData
            {
                Label = p,
                Value = random.Next(10, 100)
            }).ToList();
        }

        private List<ChartData> GenerateSampleDepotData()
        {
            var depots = new[] { "Ana Depo", "Yan Depo", "Acil Depo", "Yedek Depo" };
            var random = new Random();
            return depots.Select(d => new ChartData
            {
                Label = d,
                Value = random.Next(20, 150)
            }).ToList();
        }

        private List<ChartData> GenerateSampleMovementTypeData()
        {
            var types = new[] { "Giriş", "Çıkış", "Transfer", "Düzeltme" };
            var random = new Random();
            return types.Select(t => new ChartData
            {
                Label = t,
                Value = random.Next(5, 50)
            }).ToList();
        }

        private List<ChartData> GenerateSampleSubDepotData()
        {
            var subDepots = new[] { "Alt Depo 1", "Alt Depo 2", "Alt Depo 3", "Alt Depo 4" };
            var random = new Random();
            return subDepots.Select(sd => new ChartData
            {
                Label = sd,
                Value = random.Next(10, 80)
            }).ToList();
        }

        private List<ChartData> GenerateSampleDailyTrendData()
        {
            var data = new List<ChartData>();
            var random = new Random();
            var currentDate = DateTime.Now.AddDays(-29);
            
            for (int i = 0; i < 30; i++)
            {
                data.Add(new ChartData
                {
                    Label = currentDate.ToString("dd/MM"),
                    Value = random.Next(5, 50)
                });
                currentDate = currentDate.AddDays(1);
            }
            return data;
        }

        private List<ChartData> GenerateSampleCriticalStockData()
        {
            var products = new[] { "Kritik Ürün 1", "Kritik Ürün 2", "Kritik Ürün 3", "Kritik Ürün 4", "Kritik Ürün 5" };
            var random = new Random();
            return products.Select(p => new ChartData
            {
                Label = p,
                Value = random.Next(1, 20)
            }).ToList();
        }

        private List<ChartData> GenerateSampleUserData()
        {
            var users = new[] { "Kullanıcı 1", "Kullanıcı 2", "Kullanıcı 3", "Kullanıcı 4" };
            var random = new Random();
            return users.Select(u => new ChartData
            {
                Label = u,
                Value = random.Next(5, 30)
            }).ToList();
        }

        private List<ChartData> GenerateSampleLowStockData()
        {
            var products = new[] { "Düşük Stok 1", "Düşük Stok 2", "Düşük Stok 3", "Düşük Stok 4", "Düşük Stok 5" };
            var random = new Random();
            return products.Select(p => new ChartData
            {
                Label = p,
                Value = random.Next(1, 50)
            }).ToList();
        }

        private GrafikselRaporlarViewModel GenerateSampleViewModel()
        {
            return new GrafikselRaporlarViewModel
            {
                AylikCikislar = GenerateSampleMonthlyData("Çıkış"),
                AylikGirisler = GenerateSampleMonthlyData("Giriş"),
                AylikTransferler = GenerateSampleMonthlyData("Transfer"),
                EnCokCikisiYapilanUrunler = GenerateSampleProductData("Çıkış"),
                DepoBazliStokDurumu = GenerateSampleDepotData(),
                HareketTipiDagilimi = GenerateSampleMovementTypeData(),
                AltDepoStokDagilimi = GenerateSampleSubDepotData(),
                GunlukHareketTrendi = GenerateSampleDailyTrendData(),
                KritikStokSeviyeleri = GenerateSampleCriticalStockData(),
                EnAktifKullanicilar = GenerateSampleUserData(),
                EnAzStoguKalanUrunler = GenerateSampleLowStockData()
            };
        }

        // --- YENİ EKLENEN İSTATİSTİK METODU ---
        public async Task<IActionResult> Istatistikler()
        {
            var viewModel = new IstatistiklerViewModel();

            // Genel İstatistikler
            viewModel.ToplamUrunCesidi = await _context.Stoks.Where(s => s.Statu).CountAsync();
            viewModel.ToplamStokAdedi = await _context.StokDurums.SumAsync(s => s.DurumMiktar);
            viewModel.ToplamDepoSayisi = await _context.AltDepos.Where(a => a.Statu).CountAsync();
            viewModel.BuAykiHareketSayisi = await _context.StokHarekets
                .Where(h => h.HareketTarihi.Year == DateTime.Now.Year && h.HareketTarihi.Month == DateTime.Now.Month)
                .CountAsync();

            // En Çok Bulunan Ürün (Tüm depolardaki toplamına göre)
            var enCok = await _context.StokDurums
                .GroupBy(s => s.Stok.StokAd)
                .Select(g => new ChartData { Label = g.Key ?? "Bilinmeyen", Value = g.Sum(s => s.DurumMiktar) })
                .OrderByDescending(x => x.Value)
                .FirstOrDefaultAsync();
            viewModel.EnCokBulunanUrun = enCok ?? new ChartData { Label = "Veri Yok", Value = 0 };

            // En Az Bulunan Ürün (Tüm depolardaki toplamına göre)
            var enAz = await _context.StokDurums
                .GroupBy(s => s.Stok.StokAd)
                .Select(g => new ChartData { Label = g.Key ?? "Bilinmeyen", Value = g.Sum(s => s.DurumMiktar) })
                .OrderBy(x => x.Value)
                .FirstOrDefaultAsync();
            viewModel.EnAzBulunanUrun = enAz ?? new ChartData { Label = "Veri Yok", Value = 0 };

            return View(viewModel);
        }
    }
}