using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StokTakip.Data;
using StokTakip.Models;

namespace StokTakip.Controllers
{
    public class DepoEslestirmesController : Controller
    {
        private readonly Data.StokTakipBgcContext _context;

        public DepoEslestirmesController(Data.StokTakipBgcContext context)
        {
            _context = context;
        }

        // GET: DepoEslestirmes
        public async Task<IActionResult> Index()
        {
            var stokTakipBgcContext = _context.DepoEslestirmes.Include(d => d.AltDepo).Include(d => d.Depo).Include(d => d.GuncelleyenKullaniciNavigation).Include(d => d.OlusturanKullaniciNavigation);
            return View(await stokTakipBgcContext.ToListAsync());
        }

        // GET: DepoEslestirmes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var depoEslestirme = await _context.DepoEslestirmes
                .Include(d => d.AltDepo)
                .Include(d => d.Depo)
                .Include(d => d.GuncelleyenKullaniciNavigation)
                .Include(d => d.OlusturanKullaniciNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (depoEslestirme == null)
            {
                return NotFound();
            }

            return View(depoEslestirme);
        }

        // GET: DepoEslestirmes/Create
        public IActionResult Create()
        {
            ViewData["AltDepoId"] = new SelectList(_context.AltDepos, "Id", "Id");
            ViewData["DepoId"] = new SelectList(_context.Depos, "Id", "Id");
            ViewData["GuncelleyenKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id");
            ViewData["OlusturanKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id");
            return View();
        }

        // POST: DepoEslestirmes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DepoId,AltDepoId,Statu,OlusturanKullanici,GuncelleyenKullanici,OlusturmaTarihi,GuncellemeTarihi")] DepoEslestirme depoEslestirme)
        {
            if (ModelState.IsValid)
            {
                _context.Add(depoEslestirme);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AltDepoId"] = new SelectList(_context.AltDepos, "Id", "Id", depoEslestirme.AltDepoId);
            ViewData["DepoId"] = new SelectList(_context.Depos, "Id", "Id", depoEslestirme.DepoId);
            ViewData["GuncelleyenKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", depoEslestirme.GuncelleyenKullanici);
            ViewData["OlusturanKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", depoEslestirme.OlusturanKullanici);
            return View(depoEslestirme);
        }

        // GET: DepoEslestirmes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var depoEslestirme = await _context.DepoEslestirmes.FindAsync(id);
            if (depoEslestirme == null)
            {
                return NotFound();
            }
            ViewData["AltDepoId"] = new SelectList(_context.AltDepos, "Id", "Id", depoEslestirme.AltDepoId);
            ViewData["DepoId"] = new SelectList(_context.Depos, "Id", "Id", depoEslestirme.DepoId);
            ViewData["GuncelleyenKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", depoEslestirme.GuncelleyenKullanici);
            ViewData["OlusturanKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", depoEslestirme.OlusturanKullanici);
            return View(depoEslestirme);
        }

        // POST: DepoEslestirmes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DepoId,AltDepoId,Statu,OlusturanKullanici,GuncelleyenKullanici,OlusturmaTarihi,GuncellemeTarihi")] DepoEslestirme depoEslestirme)
        {
            if (id != depoEslestirme.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(depoEslestirme);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepoEslestirmeExists(depoEslestirme.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AltDepoId"] = new SelectList(_context.AltDepos, "Id", "Id", depoEslestirme.AltDepoId);
            ViewData["DepoId"] = new SelectList(_context.Depos, "Id", "Id", depoEslestirme.DepoId);
            ViewData["GuncelleyenKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", depoEslestirme.GuncelleyenKullanici);
            ViewData["OlusturanKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", depoEslestirme.OlusturanKullanici);
            return View(depoEslestirme);
        }

        // GET: DepoEslestirmes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var depoEslestirme = await _context.DepoEslestirmes
                .Include(d => d.AltDepo)
                .Include(d => d.Depo)
                .Include(d => d.GuncelleyenKullaniciNavigation)
                .Include(d => d.OlusturanKullaniciNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (depoEslestirme == null)
            {
                return NotFound();
            }

            return View(depoEslestirme);
        }

        // POST: DepoEslestirmes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var depoEslestirme = await _context.DepoEslestirmes
                .Include(de => de.AltDepo)
                .Include(de => de.Depo)
                .FirstOrDefaultAsync(de => de.Id == id);
                
            if (depoEslestirme != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var depoAdi = depoEslestirme.Depo.DepoAdi;
                    var altDepoAdi = depoEslestirme.AltDepo.AltDepoAdi;
                    
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
                    
                    // Ana kaydı sil
                    await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM DEPO_ESLESTIRME WHERE id = {0}", id);
                    
                    // Constraint'leri tekrar aktif et
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE STOK_HAREKET WITH CHECK CHECK CONSTRAINT ALL");
                    await _context.Database.ExecuteSqlRawAsync(
                        "ALTER TABLE STOK_DURUM WITH CHECK CHECK CONSTRAINT ALL");
                    
                    // Transaction'ı commit et
                    await transaction.CommitAsync();
                    
                    TempData["SuccessMessage"] = $"'{depoAdi} - {altDepoAdi}' eşleştirmesi ve tüm bağlı kayıtlar başarıyla silindi.";
                }
                catch (Exception ex)
                {
                    // Hata durumunda transaction'ı geri al
                    await transaction.RollbackAsync();
                    
                    // Constraint'leri tekrar aktif etmeye çalış
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

        private bool DepoEslestirmeExists(int id)
        {
            return _context.DepoEslestirmes.Any(e => e.Id == id);
        }
    }
}
