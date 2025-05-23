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
                EndTime = TimeSpan.Parse("23:59"),
                UserType = "Utilisateur",
                UseCustomAuthMode = false
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

            try
            {
                // Traitement du statut depuis le switch
                model.Status = form["statusSwitch"] == "on" ? "Actif" : "Inactif";

                // Traitement du mode d'authentification
                model.UseCustomAuthMode = form["authModeSwitch"] != "on";

                // Validation du numéro de carte si fourni
                if (!string.IsNullOrEmpty(model.CardNumber))
                {
                    if (!IsValidCardNumber(model.CardNumber))
                    {
                        ModelState.AddModelError("CardNumber", "Format de carte invalide (minimum 8 chiffres requis)");
                    }
                }

                // Validation des champs requis
                if (string.IsNullOrWhiteSpace(model.Nom))
                {
                    ModelState.AddModelError("Nom", "Le nom est requis");
                }

                if (string.IsNullOrWhiteSpace(model.Prenom))
                {
                    ModelState.AddModelError("Prenom", "Le prénom est requis");
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError("Email", "L'email est requis");
                }
                else if (!IsValidEmail(model.Email))
                {
                    ModelState.AddModelError("Email", "Format d'email invalide");
                }

                // Validation des dates
                if (model.StartDate > model.EndDate)
                {
                    ModelState.AddModelError("EndDate", "La date de fin doit être postérieure à la date de début");
                }

                // Validation des heures
                if (model.StartTime >= model.EndTime)
                {
                    ModelState.AddModelError("EndTime", "L'heure de fin doit être postérieure à l'heure de début");
                }

                // Validation du niveau de sécurité
                if (model.SecurityLevel < 1 || model.SecurityLevel > 5)
                {
                    ModelState.AddModelError("SecurityLevel", "Le niveau de sécurité doit être entre 1 et 5");
                }

                // S'assurer que CreatedAt est défini
                model.CreatedAt = DateTime.Now;

                if (ModelState.IsValid)
                {
                    await _repository.AddAsync(model);
                    TempData["Message"] = "Utilisateur créé avec succès";
                    TempData["MessageType"] = "success";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Erreur lors de la création de l'utilisateur: {ex.Message}";
                TempData["MessageType"] = "error";
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
                TempData["Message"] = "Utilisateur non trouvé";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Index));
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
                TempData["Message"] = "ID utilisateur invalide";
                TempData["MessageType"] = "error";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Récupérer l'utilisateur existant pour préserver certaines données
                var existingUser = await _repository.GetByIdAsync(id);
                if (existingUser == null)
                {
                    TempData["Message"] = "Utilisateur non trouvé";
                    TempData["MessageType"] = "error";
                    return RedirectToAction(nameof(Index));
                }

                // Préserver les données qui ne doivent pas être modifiées
                model.CreatedAt = existingUser.CreatedAt;
                if (model.LastLogin == null || model.LastLogin == DateTime.MinValue)
                {
                    model.LastLogin = existingUser.LastLogin;
                }

                // Traitement des valeurs des switches et cases à cocher
                model.Status = form["statusSwitch"] == "on" ? "Actif" : "Inactif";
                model.UseCustomAuthMode = form["authModeSwitch"] != "on";

                // Validation du numéro de carte si fourni
                if (!string.IsNullOrEmpty(model.CardNumber))
                {
                    if (!IsValidCardNumber(model.CardNumber))
                    {
                        ModelState.AddModelError("CardNumber", "Format de carte invalide (minimum 8 chiffres requis)");
                    }
                }

                // Validation des champs requis
                if (string.IsNullOrWhiteSpace(model.Nom))
                {
                    ModelState.AddModelError("Nom", "Le nom est requis");
                }

                if (string.IsNullOrWhiteSpace(model.Prenom))
                {
                    ModelState.AddModelError("Prenom", "Le prénom est requis");
                }

                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    ModelState.AddModelError("Email", "L'email est requis");
                }
                else if (!IsValidEmail(model.Email))
                {
                    ModelState.AddModelError("Email", "Format d'email invalide");
                }

                // Validation des dates
                if (model.StartDate > model.EndDate)
                {
                    ModelState.AddModelError("EndDate", "La date de fin doit être postérieure à la date de début");
                }

                // Validation des heures
                if (model.StartTime >= model.EndTime)
                {
                    ModelState.AddModelError("EndTime", "L'heure de fin doit être postérieure à l'heure de début");
                }

                // Validation du niveau de sécurité
                if (model.SecurityLevel < 1 || model.SecurityLevel > 5)
                {
                    ModelState.AddModelError("SecurityLevel", "Le niveau de sécurité doit être entre 1 et 5");
                }

                if (ModelState.IsValid)
                {
                    await _repository.UpdateAsync(model);
                    TempData["Message"] = "Utilisateur mis à jour avec succès";
                    TempData["MessageType"] = "success";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Erreur lors de la mise à jour de l'utilisateur: {ex.Message}";
                TempData["MessageType"] = "error";
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

            try
            {
                var user = await _repository.GetByIdAsync(id);
                if (user == null)
                {
                    TempData["Message"] = "Utilisateur non trouvé";
                    TempData["MessageType"] = "error";
                    return RedirectToAction(nameof(Index));
                }

                await _repository.DeleteAsync(id);
                TempData["Message"] = $"Utilisateur {user.FullName} supprimé avec succès";
                TempData["MessageType"] = "success";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Erreur lors de la suppression: {ex.Message}";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Attribution de droits d'accès aux portes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignAccess(int userId, List<int> doorIds)
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Dans une implémentation réelle, vous attribueriez les droits d'accès dans une table de relation
                // Pour l'instant, on simule l'attribution
                var user = await _repository.GetByIdAsync(userId);
                if (user == null)
                {
                    TempData["Message"] = "Utilisateur non trouvé";
                    TempData["MessageType"] = "error";
                    return RedirectToAction(nameof(Index));
                }

                // Simuler l'attribution des droits
                user.AccessibleDoors = doorIds ?? new List<int>();

                TempData["Message"] = "Droits d'accès modifiés avec succès";
                TempData["MessageType"] = "success";
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Erreur lors de la modification des droits: {ex.Message}";
                TempData["MessageType"] = "error";
            }

            return RedirectToAction(nameof(Details), new { id = userId });
        }

        // Méthodes de validation privées
        private bool IsValidCardNumber(string number)
        {
            return !string.IsNullOrWhiteSpace(number) &&
                   number.Length >= 8 &&
                   number.All(char.IsDigit);
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Méthode utilitaire pour valider le format du téléphone (optionnel)
        private bool IsValidPhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true; // Le téléphone n'est pas requis

            // Supprimer les espaces, tirets, points, parenthèses
            var cleanPhone = phone.Replace(" ", "").Replace("-", "").Replace(".", "").Replace("(", "").Replace(")", "");

            // Vérifier que ce sont tous des chiffres et que la longueur est raisonnable
            return cleanPhone.All(char.IsDigit) && cleanPhone.Length >= 8 && cleanPhone.Length <= 15;
        }

        // Action pour vérifier la disponibilité d'un email (AJAX)
        [HttpPost]
        public async Task<JsonResult> CheckEmailAvailability(string email, int? userId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { available = false, message = "Email requis" });
            }

            // Dans une vraie application, vous vérifieriez dans la base de données
            // si l'email existe déjà pour un autre utilisateur
            // Pour l'instant, on simule cette vérification

            return Json(new { available = true, message = "Email disponible" });
        }

        // Action pour récupérer les suggestions de départements (AJAX)
        [HttpGet]
        public async Task<JsonResult> GetDepartmentSuggestions(string term)
        {
            var departments = await _repository.GetDepartmentsAsync();
            var suggestions = departments
                .Where(d => d.ToLower().Contains(term.ToLower()))
                .Take(10)
                .ToList();

            return Json(suggestions);
        }
    }
}