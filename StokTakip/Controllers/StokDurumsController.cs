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
    public class StokDurumsController : Controller
    {
        private readonly Data.StokTakipBgcContext _context;

        public StokDurumsController(Data.StokTakipBgcContext context)
        {
            _context = context;
        }

        // GET: StokDurums
        public async Task<IActionResult> Index()
        {
            var stokTakipBgcContext = _context.StokDurums.Include(s => s.DepoEslestirme).Include(s => s.GuncelleyenKullaniciNavigation).Include(s => s.OlusturanKullaniciNavigation).Include(s => s.Stok);
            return View(await stokTakipBgcContext.ToListAsync());
        }

        // GET: StokDurums/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stokDurum = await _context.StokDurums
                .Include(s => s.DepoEslestirme)
                .Include(s => s.GuncelleyenKullaniciNavigation)
                .Include(s => s.OlusturanKullaniciNavigation)
                .Include(s => s.Stok)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stokDurum == null)
            {
                return NotFound();
            }

            return View(stokDurum);
        }

        // GET: StokDurums/Create
        public IActionResult Create()
        {
            ViewData["DepoEslestirmeId"] = new SelectList(_context.DepoEslestirmes, "Id", "Id");
            ViewData["GuncelleyenKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id");
            ViewData["OlusturanKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id");
            ViewData["StokId"] = new SelectList(_context.Stoks, "Id", "Id");
            return View();
        }

        // POST: StokDurums/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StokId,DepoEslestirmeId,DurumMiktar,OlusturanKullanici,GuncelleyenKullanici,OlusturmaTarihi,GuncellemeTarihi")] StokDurum stokDurum)
        {
            if (ModelState.IsValid)
            {
                _context.Add(stokDurum);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepoEslestirmeId"] = new SelectList(_context.DepoEslestirmes, "Id", "Id", stokDurum.DepoEslestirmeId);
            ViewData["GuncelleyenKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", stokDurum.GuncelleyenKullanici);
            ViewData["OlusturanKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", stokDurum.OlusturanKullanici);
            ViewData["StokId"] = new SelectList(_context.Stoks, "Id", "Id", stokDurum.StokId);
            return View(stokDurum);
        }

        // GET: StokDurums/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stokDurum = await _context.StokDurums.FindAsync(id);
            if (stokDurum == null)
            {
                return NotFound();
            }
            ViewData["DepoEslestirmeId"] = new SelectList(_context.DepoEslestirmes, "Id", "Id", stokDurum.DepoEslestirmeId);
            ViewData["GuncelleyenKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", stokDurum.GuncelleyenKullanici);
            ViewData["OlusturanKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", stokDurum.OlusturanKullanici);
            ViewData["StokId"] = new SelectList(_context.Stoks, "Id", "Id", stokDurum.StokId);
            return View(stokDurum);
        }

        // POST: StokDurums/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StokId,DepoEslestirmeId,DurumMiktar,OlusturanKullanici,GuncelleyenKullanici,OlusturmaTarihi,GuncellemeTarihi")] StokDurum stokDurum)
        {
            if (id != stokDurum.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(stokDurum);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StokDurumExists(stokDurum.Id))
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
            ViewData["DepoEslestirmeId"] = new SelectList(_context.DepoEslestirmes, "Id", "Id", stokDurum.DepoEslestirmeId);
            ViewData["GuncelleyenKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", stokDurum.GuncelleyenKullanici);
            ViewData["OlusturanKullanici"] = new SelectList(_context.Kullanicis, "Id", "Id", stokDurum.OlusturanKullanici);
            ViewData["StokId"] = new SelectList(_context.Stoks, "Id", "Id", stokDurum.StokId);
            return View(stokDurum);
        }

        // GET: StokDurums/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stokDurum = await _context.StokDurums
                .Include(s => s.DepoEslestirme)
                .Include(s => s.GuncelleyenKullaniciNavigation)
                .Include(s => s.OlusturanKullaniciNavigation)
                .Include(s => s.Stok)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (stokDurum == null)
            {
                return NotFound();
            }

            return View(stokDurum);
        }

        // POST: StokDurums/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var stokDurum = await _context.StokDurums.FindAsync(id);
            if (stokDurum != null)
            {
                _context.StokDurums.Remove(stokDurum);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StokDurumExists(int id)
        {
            return _context.StokDurums.Any(e => e.Id == id);
        }
    }
}