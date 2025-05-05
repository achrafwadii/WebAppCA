using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebAppCA.Models;
using WebAppCA.Data;

namespace WebAppCA.Controllers
{
    public class PresenceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PresenceController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET: Presence
        public async Task<IActionResult> Index(FiltrePresence filtre)
        {
            // Récupérer la liste des utilisateurs pour le dropdown
            ViewBag.Utilisateurs = await _context.Utilisateurs
                .OrderBy(u => u.Nom)
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.FullName
                })
                .ToListAsync();

            // Préparer la requête
            var query = _context.Pointages
                .Include(p => p.Utilisateur)
                .Include(p => p.PointAcces)
                .AsQueryable();

            // Appliquer les filtres
            if (filtre.DateDebut.HasValue)
            {
                query = query.Where(p => p.Date >= filtre.DateDebut.Value.Date);
            }

            if (filtre.DateFin.HasValue)
            {
                query = query.Where(p => p.Date <= filtre.DateFin.Value.Date);
            }

            if (filtre.UtilisateurId.HasValue)
            {
                query = query.Where(p => p.UtilisateurId == filtre.UtilisateurId.Value);
            }

            // Ordonner les résultats
            var pointages = await query
                .OrderByDescending(p => p.Date)
                .ThenByDescending(p => p.HeureEntree)
                .ToListAsync();

            return View(pointages);
        }

        // GET: Presence/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pointage = await _context.Pointages
                .Include(p => p.Utilisateur)
                .Include(p => p.PointAcces)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (pointage == null)
            {
                return NotFound();
            }

            return View(pointage);
        }

        // POST: Presence/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UtilisateurId,Date,HeureEntree,HeureSortie,PointAccesId")] Pointage pointage)
        {
            if (ModelState.IsValid)
            {
                // Validation: l'heure de sortie doit être après l'heure d'entrée
                if (pointage.HeureSortie.HasValue &&
                    pointage.HeureSortie.Value.TimeOfDay <= pointage.HeureEntree.TimeOfDay)
                {
                    ModelState.AddModelError("HeureSortie", "L'heure de sortie doit être après l'heure d'entrée");
                    return BadRequest(ModelState);
                }

                _context.Add(pointage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // En cas d'erreur, renvoyer les erreurs en format JSON
            return BadRequest(ModelState);
        }

        // GET: Presence/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pointage = await _context.Pointages.FindAsync(id);
            if (pointage == null)
            {
                return NotFound();
            }

            ViewBag.Utilisateurs = new SelectList(_context.Utilisateurs, "Id", "NomComplet", pointage.UtilisateurId);
            ViewBag.PointsAcces = new SelectList(_context.PointsAcces, "Id", "Nom", pointage.PointAccesId);

            return View(pointage);
        }

        // POST: Presence/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UtilisateurId,Date,HeureEntree,HeureSortie,PointAccesId")] Pointage pointage)
        {
            if (id != pointage.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(pointage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PointageExists(pointage.Id))
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

            ViewBag.Utilisateurs = new SelectList(_context.Utilisateurs, "Id", "NomComplet", pointage.UtilisateurId);
            ViewBag.PointsAcces = new SelectList(_context.PointsAcces, "Id", "Nom", pointage.PointAccesId);

            return View(pointage);
        }

        // POST: Presence/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pointage = await _context.Pointages.FindAsync(id);
            _context.Pointages.Remove(pointage);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PointageExists(int id)
        {
            return _context.Pointages.Any(e => e.Id == id);
        }

        // GET: Presence/GetUtilisateurs
        [HttpGet]
        public async Task<IActionResult> GetUtilisateurs()
        {
            var utilisateurs = await _context.Utilisateurs
                .OrderBy(u => u.Nom)
                .Select(u => new { id = u.Id, nom = u.FullName })
                .ToListAsync();

            return Json(utilisateurs);
        }

        // GET: Presence/GetPointsAcces
        [HttpGet]
        public async Task<IActionResult> GetPointsAcces()
        {
            var pointsAcces = await _context.PointsAcces
                .OrderBy(p => p.Nom)
                .Select(p => new { id = p.Id, nom = p.Nom })
                .ToListAsync();

            return Json(pointsAcces);
        }
        public IActionResult Attendance()
        {
            // Populate Utilisateurs before returning the view
            ViewBag.Utilisateurs = _context.Utilisateurs
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.FullName
                })
                .ToList();

            // Your existing logic for filtering and retrieving pointages
            var pointages = _context.Pointages
                .Include(p => p.Utilisateur)
                .Include(p => p.PointAcces)
                // Apply any filtering logic
                .ToList();

            return View(pointages);
        }
    }
}