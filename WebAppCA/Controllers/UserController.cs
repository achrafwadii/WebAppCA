using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using WebAppCA.Models;

namespace WebAppCA.Controllers
{
    public class UserController : Controller
    {
        // GET: Liste des utilisateurs
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            // Ceci devrait être remplacé par une récupération réelle depuis la base de données
            var users = GetSampleUsers();
            return View(users);
        }

        // GET: Détails d'un utilisateur
        public IActionResult Details(int id)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            var user = GetSampleUsers().FirstOrDefault(u => u.UserID == id);
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

            var model = new UserInfoModel
            {
                Status = "Actif",
                CreatedAt = DateTime.Now,
                AccessGroups = new List<string>(),
                AccessibleDoors = new List<int>()
            };

            return View(model);
        }

        // POST: Création d'un utilisateur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(UserInfoModel model, IFormCollection form)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            // Récupérer les valeurs des switches et cases à cocher
            model.Status = form["statusSwitch"] == "on" ? "Actif" : "Inactif";

            // Traiter les groupes d'accès sélectionnés (à adapter selon votre UI)
            var selectedGroups = form["accessGroups"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            model.AccessGroups = selectedGroups.ToList();

            // Traiter les portes accessibles
            var selectedDoors = form.Keys
                .Where(k => k.StartsWith("door_") && form[k] == "on")
                .Select(k => int.Parse(k.Replace("door_", "")))
                .ToList();
            model.AccessibleDoors = selectedDoors;

            if (ModelState.IsValid)
            {
                // Dans une implémentation réelle, vous enregistreriez l'utilisateur dans la base de données
                model.CreatedAt = DateTime.Now;

                TempData["Message"] = "Utilisateur créé avec succès";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Formulaire d'édition d'un utilisateur
        public IActionResult Edit(int id)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            var user = GetSampleUsers().FirstOrDefault(u => u.UserID == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Mise à jour d'un utilisateur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, UserInfoModel model, IFormCollection form)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            if (id != model.UserID)
            {
                return NotFound();
            }

            // Récupérer les valeurs des switches et cases à cocher
            model.Status = form["statusSwitch"] == "on" ? "Actif" : "Inactif";

            // Traiter les groupes d'accès sélectionnés
            var selectedGroups = form["accessGroups"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            model.AccessGroups = selectedGroups.ToList();

            // Traiter les portes accessibles
            var selectedDoors = form.Keys
                .Where(k => k.StartsWith("door_") && form[k] == "on")
                .Select(k => int.Parse(k.Replace("door_", "")))
                .ToList();
            model.AccessibleDoors = selectedDoors;

            if (ModelState.IsValid)
            {
                // Dans une implémentation réelle, vous mettriez à jour l'utilisateur dans la base de données

                TempData["Message"] = "Utilisateur mis à jour avec succès";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: Suppression d'un utilisateur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            // Dans une implémentation réelle, vous supprimeriez l'utilisateur de la base de données

            TempData["Message"] = "Utilisateur supprimé avec succès";
            return RedirectToAction(nameof(Index));
        }

        // POST: Attribution de droits d'accès aux portes
        [HttpPost]
        public IActionResult AssignAccess(int userId, List<int> doorIds)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            // Dans une implémentation réelle, vous attribueriez les droits d'accès dans la base de données

            TempData["Message"] = "Droits d'accès modifiés avec succès";
            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // Méthode de démo pour générer des utilisateurs d'exemple
        private List<UserInfoModel> GetSampleUsers()
        {
            return new List<UserInfoModel>
            {
                new UserInfoModel
                {
                    UserID = 1,
                    Username = "admin",
                    FirstName = "Pierre",
                    LastName = "Dupont",
                    Email = "admin@timetrack.com",
                    Phone = "01 23 45 67 89",
                    Department = "IT",
                    Position = "Administrateur système",
                    BadgeNumber = "A12345",
                    UserType = "Admin",
                    Status = "Actif",
                    CreatedAt = DateTime.Now.AddMonths(-6),
                    LastLogin = DateTime.Now.AddDays(-1),
                    AccessGroups = new List<string> { "Administrateurs", "Gestionnaires" },
                    AccessibleDoors = new List<int> { 1, 2, 3, 4 }
                },
                new UserInfoModel
                {
                    UserID = 2,
                    Username = "m.martin",
                    FirstName = "Marie",
                    LastName = "Martin",
                    Email = "m.martin@timetrack.com",
                    Phone = "01 23 45 67 90",
                    Department = "RH",
                    Position = "Directrice RH",
                    BadgeNumber = "B67890",
                    UserType = "Manager",
                    Status = "Actif",
                    CreatedAt = DateTime.Now.AddMonths(-3),
                    LastLogin = DateTime.Now.AddDays(-3),
                    AccessGroups = new List<string> { "Gestionnaires", "RH" },
                    AccessibleDoors = new List<int> { 1, 2 }
                },
                new UserInfoModel
                {
                    UserID = 3,
                    Username = "j.durand",
                    FirstName = "Jean",
                    LastName = "Durand",
                    Email = "j.durand@timetrack.com",
                    Phone = "01 23 45 67 91",
                    Department = "Commercial",
                    Position = "Vendeur",
                    BadgeNumber = "C45678",
                    UserType = "Utilisateur",
                    Status = "Actif",
                    CreatedAt = DateTime.Now.AddMonths(-1),
                    LastLogin = DateTime.Now.AddHours(-12),
                    AccessGroups = new List<string> { "Commerciaux" },
                    AccessibleDoors = new List<int> { 1 }
                }
            };
        }
    }
}