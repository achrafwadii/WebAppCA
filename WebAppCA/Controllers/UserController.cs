using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppCA.Models;
using WebAppCA.Repositories;

namespace WebAppCA.Controllers
{
    public class UserController : Controller
    {
        private readonly UtilisateurRepository _repository;

        public UserController(UtilisateurRepository repository)
        {
            _repository = repository;
        }

        // GET: Liste des utilisateurs
        public async Task<IActionResult> Index(string searchTerm = null, string status = null, string departement = null)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            // Récupérer les utilisateurs depuis la base de données
            var users = await _repository.SearchAsync(searchTerm, status, departement);

            // Récupérer la liste des départements pour le filtre
            ViewBag.Departments = await _repository.GetDepartmentsAsync();

            return View(users);
        }

        // GET: Détails d'un utilisateur
        public async Task<IActionResult> Details(int id)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _repository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Formulaire de création d'un utilisateur
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new Utilisateur
            {
                Status = "Actif",
                CreatedAt = DateTime.Now,
                StartDate = DateTime.Now,
                EndDate = new DateTime(2037, 12, 31),
                SecurityLevel = 5,
                BioStarOperator = "Jamais",
                StartTime = TimeSpan.Parse("00:00"),
                EndTime = TimeSpan.Parse("23:59")
            };

            return View(model);
        }

        // POST: Création d'un utilisateur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Utilisateur model, IFormCollection form)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            // Récupérer les valeurs des switches et cases à cocher
            model.Status = form["statusSwitch"] == "on" ? "Actif" : "Inactif";
            model.UseCustomAuthMode = form["authModeSwitch"] != "on"; // Inverser car le switch est "utiliser le mode par défaut"

            if (ModelState.IsValid)
            {
                // Enregistrer l'utilisateur dans la base de données
                model.CreatedAt = DateTime.Now;
                await _repository.AddAsync(model);

                TempData["Message"] = "Utilisateur créé avec succès";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Formulaire d'édition d'un utilisateur
        public async Task<IActionResult> Edit(int id)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _repository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Mise à jour d'un utilisateur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Utilisateur model, IFormCollection form)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            if (id != model.Id)
            {
                return NotFound();
            }

            // Récupérer les valeurs des switches et cases à cocher
            model.Status = form["statusSwitch"] == "on" ? "Actif" : "Inactif";
            model.UseCustomAuthMode = form["authModeSwitch"] != "on"; // Inverser car le switch est "utiliser le mode par défaut"

            if (ModelState.IsValid)
            {
                await _repository.UpdateAsync(model);

                TempData["Message"] = "Utilisateur mis à jour avec succès";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: Suppression d'un utilisateur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            await _repository.DeleteAsync(id);

            TempData["Message"] = "Utilisateur supprimé avec succès";
            return RedirectToAction(nameof(Index));
        }

        // POST: Attribution de droits d'accès aux portes
        [HttpPost]
        public async Task<IActionResult> AssignAccess(int userId, List<int> doorIds)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            // Dans une implémentation réelle, vous attribueriez les droits d'accès dans une table de relation
            // Pour l'instant, on renvoie simplement vers la page de détails

            TempData["Message"] = "Droits d'accès modifiés avec succès";
            return RedirectToAction(nameof(Details), new { id = userId });
        }
    }
}