using Microsoft.EntityFrameworkCore;
using StokTakip.Data;
using StokTakip.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StokTakip.Services
{
    // FIFO detay bilgilerini tutan sınıf
    public class FifoDetay
    {
        public DateTime GirisTarihi { get; set; }
        public decimal KullanilanMiktar { get; set; }
    }
    public interface IStokDurumService
    {
        Task<List<StokDurum>> GetMinimumStokUyarilariAsync();
        Task<AltDepo> GetEnFazlaStokluAltDepoAsync(int mevcutAltDepoId);
        Task<List<StokDurum>> GetFifoStokListesiAsync(int stokId, int depoEslestirmeId, decimal miktar);
        Task<(bool success, List<FifoDetay> fifoDetaylar)> TransferStokFifoAsync(int kaynakDepoEslestirmeId, int hedefDepoEslestirmeId, int stokId, decimal miktar, int kullaniciId);
    }

    public class StokDurumService : IStokDurumService
    {
        private readonly StokTakipBgcContext _context;

        public StokDurumService(StokTakipBgcContext context)
        {
            _context = context;
        }

        // Minimum stok uyarılarını getir
        public async Task<List<StokDurum>> GetMinimumStokUyarilariAsync()
        {
            // Sabit minimum stok miktarı
            const decimal MINIMUM_STOK_MIKTARI = 10;
            
            var uyarilar = await _context.StokDurums
                .Include(sd => sd.Stok)
                .Include(sd => sd.DepoEslestirme)
                .ThenInclude(de => de.AltDepo)
                .Where(sd => sd.Stok.Statu && 
                           sd.DepoEslestirme.AltDepo.Statu && 
                           sd.DurumMiktar < MINIMUM_STOK_MIKTARI)
                .ToListAsync();

            return uyarilar;
        }

        // En fazla stoklu alt depoyu bul
        public async Task<AltDepo> GetEnFazlaStokluAltDepoAsync(int mevcutAltDepoId)
        {
            var enFazlaStokluAltDepo = await _context.StokDurums
                .Include(sd => sd.DepoEslestirme)
                .ThenInclude(de => de.AltDepo)
                .Where(sd => sd.DepoEslestirme.AltDepo.Id != mevcutAltDepoId &&
                           sd.DepoEslestirme.AltDepo.Statu &&
                           sd.Stok.Statu &&
                           sd.DurumMiktar > 0)
                .GroupBy(sd => sd.DepoEslestirme.AltDepo.Id)
                .Select(g => new
                {
                    AltDepo = g.First().DepoEslestirme.AltDepo,
                    ToplamStok = g.Sum(sd => sd.DurumMiktar)
                })
                .OrderByDescending(x => x.ToplamStok)
                .FirstOrDefaultAsync();

            return enFazlaStokluAltDepo?.AltDepo;
        }

        // FIFO algoritması ile stok listesi getir
        public async Task<List<StokDurum>> GetFifoStokListesiAsync(int stokId, int depoEslestirmeId, decimal miktar)
        {
            var stokDurumlar = await _context.StokDurums
                .Include(sd => sd.Stok)
                .Where(sd => sd.StokId == stokId && 
                           sd.DepoEslestirmeId == depoEslestirmeId &&
                           sd.DurumMiktar > 0)
                .OrderBy(sd => sd.OlusturmaTarihi) // FIFO: En eski tarihli önce
                .ToListAsync();

            var fifoListe = new List<StokDurum>();
            decimal kalanMiktar = miktar;

            foreach (var stokDurum in stokDurumlar)
            {
                if (kalanMiktar <= 0) break;

                var kullanilacakMiktar = Math.Min(stokDurum.DurumMiktar, kalanMiktar);
                var fifoStokDurum = new StokDurum
                {
                    Id = stokDurum.Id,
                    StokId = stokDurum.StokId,
                    DepoEslestirmeId = stokDurum.DepoEslestirmeId,
                    DurumMiktar = kullanilacakMiktar,
                    OlusturmaTarihi = stokDurum.OlusturmaTarihi,
                    Stok = stokDurum.Stok
                };

                fifoListe.Add(fifoStokDurum);
                kalanMiktar -= kullanilacakMiktar;
            }

            return fifoListe;
        }

        // FIFO ile stok transferi yap
        public async Task<(bool success, List<FifoDetay> fifoDetaylar)> TransferStokFifoAsync(int kaynakDepoEslestirmeId, int hedefDepoEslestirmeId, int stokId, decimal miktar, int kullaniciId)
        {
            try
            {
                var fifoDetaylar = new List<FifoDetay>();
                
                // FIFO ile stok listesi al
                var fifoStokListesi = await GetFifoStokListesiAsync(stokId, kaynakDepoEslestirmeId, miktar);
                
                if (fifoStokListesi.Sum(sd => sd.DurumMiktar) < miktar)
                {
                    return (false, fifoDetaylar); // Yeterli stok yok
                }

                decimal kalanMiktar = miktar;

                // Her FIFO stok durumundan çıkış yap
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

                // Hedef depoya giriş yap
                var hedefStokDurum = await _context.StokDurums
                    .FirstOrDefaultAsync(sd => sd.StokId == stokId && sd.DepoEslestirmeId == hedefDepoEslestirmeId);

                if (hedefStokDurum != null)
                {
                    hedefStokDurum.DurumMiktar += miktar;
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

                // SaveChanges yapmıyoruz çünkü üst seviyede transaction var
                return (true, fifoDetaylar);
            }
            catch (Exception ex)
            {
                // Hata detayını logla
                System.Diagnostics.Debug.WriteLine($"TransferStokFifoAsync Hatası: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return (false, new List<FifoDetay>());
            }
        }
    }
}
