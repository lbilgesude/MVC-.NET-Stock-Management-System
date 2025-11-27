using StokTakip.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace StokTakip.Services
{
    public interface IUyariService
    {
        Task<List<UyariModel>> GetUyarilarAsync();
        Task AddUyariAsync(UyariModel uyari);
        Task MarkAsReadAsync(int uyariId);
        Task MarkAllAsReadAsync();
        Task<int> GetUnreadCountAsync();
        Task SilUyariAsync(int uyariId);
    }

    public class UyariService : IUyariService
    {
        private static List<UyariModel> _uyarilar = new List<UyariModel>();
        private static int _nextId = 1;
        private readonly string _dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "uyarilar.json");

        public UyariService()
        {
            LoadUyarilarFromFile();
        }

        private void LoadUyarilarFromFile()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    var json = File.ReadAllText(_dataFilePath);
                    var data = JsonSerializer.Deserialize<UyariData>(json);
                    if (data != null)
                    {
                        _uyarilar = data.Uyarilar ?? new List<UyariModel>();
                        _nextId = data.NextId;
                    }
                    Console.WriteLine($"📁 Dosyadan {_uyarilar.Count} uyarı yüklendi");
                }
                else
                {
                    Console.WriteLine("📁 Uyarı dosyası bulunamadı, boş liste ile başlatılıyor");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Uyarılar yüklenirken hata: {ex.Message}");
                _uyarilar = new List<UyariModel>();
                _nextId = 1;
            }
        }

        private void SaveUyarilarToFile()
        {
            try
            {
                var data = new UyariData
                {
                    Uyarilar = _uyarilar,
                    NextId = _nextId
                };
                
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                
                var directory = Path.GetDirectoryName(_dataFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(_dataFilePath, json);
                Console.WriteLine($"💾 {_uyarilar.Count} uyarı dosyaya kaydedildi");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Uyarılar kaydedilirken hata: {ex.Message}");
            }
        }

        public async Task<List<UyariModel>> GetUyarilarAsync()
        {
            Console.WriteLine($"🔍 GetUyarilarAsync çağrıldı, {_uyarilar.Count} uyarı var");
            return await Task.FromResult(_uyarilar.OrderByDescending(u => u.OlusturmaTarihi).ToList());
        }

        public async Task AddUyariAsync(UyariModel uyari)
        {
            Console.WriteLine($"🔔 AddUyariAsync çağrıldı: {uyari.Baslik}");
            uyari.Id = _nextId++;
            uyari.OlusturmaTarihi = DateTime.Now;
            uyari.Okundu = false;
            _uyarilar.Add(uyari);
            SaveUyarilarToFile();
            Console.WriteLine($"✅ UyariService'e uyarı eklendi: {uyari.Baslik} (Toplam: {_uyarilar.Count})");
            await Task.CompletedTask;
        }

        public async Task MarkAsReadAsync(int uyariId)
        {
            var uyari = _uyarilar.FirstOrDefault(u => u.Id == uyariId);
            if (uyari != null)
            {
                uyari.Okundu = true;
                SaveUyarilarToFile();
                Console.WriteLine($"✅ Uyarı okundu olarak işaretlendi: {uyariId}");
            }
            await Task.CompletedTask;
        }

        public async Task MarkAllAsReadAsync()
        {
            foreach (var uyari in _uyarilar.Where(u => !u.Okundu))
            {
                uyari.Okundu = true;
            }
            SaveUyarilarToFile();
            Console.WriteLine($"✅ Tüm uyarılar okundu olarak işaretlendi");
            await Task.CompletedTask;
        }

        public async Task<int> GetUnreadCountAsync()
        {
            return await Task.FromResult(_uyarilar.Count(u => !u.Okundu));
        }

        public async Task SilUyariAsync(int uyariId)
        {
            Console.WriteLine($"🗑️ SilUyariAsync çağrıldı: UyariId={uyariId}");
            var uyari = _uyarilar.FirstOrDefault(u => u.Id == uyariId);
            if (uyari != null)
            {
                _uyarilar.Remove(uyari);
                SaveUyarilarToFile();
                Console.WriteLine($"✅ UyariService'den uyarı silindi: {uyari.Baslik} (Toplam: {_uyarilar.Count})");
            }
            else
            {
                Console.WriteLine($"❌ Uyarı bulunamadı: {uyariId}");
            }
            await Task.CompletedTask;
        }
    }

    public class UyariData
    {
        public List<UyariModel> Uyarilar { get; set; } = new List<UyariModel>();
        public int NextId { get; set; } = 1;
    }
}
