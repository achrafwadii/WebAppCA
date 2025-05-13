using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using WebAppCA.Models;
using WebAppCA.Repositories;

namespace WebAppCA.Controllers
{
    public class UserController : Controller
    {
        private readonly UtilisateurRepository _repository;
        private const string UsersFile = "users.json";

        public UserController(UtilisateurRepository repository)
        {
            _repository = repository;
        }

        // Existing methods remain the same...

        // GET: Afficher la page de suppression de compte
        public IActionResult DeleteAccount()
        {
            // Check if user is authenticated
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the current username from session
            string username = HttpContext.Session.GetString("Username");

            // Fetch the user details
            var user = GetUserByUsername(username);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Traiter la suppression de compte
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAccount(string username, string password)
        {
            // Check if user is authenticated
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if the username matches the logged-in user
            string loggedInUsername = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(loggedInUsername) || loggedInUsername != username)
            {
                TempData["ErrorMessage"] = "Vous n'êtes pas autorisé à supprimer ce compte.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Verify the password before deletion
                if (!ValidateUser(username, password))
                {
                    TempData["ErrorMessage"] = "Mot de passe incorrect.";
                    return RedirectToAction("DeleteAccount");
                }

                // Delete the user account
                DeleteUser(username);

                // Clear the session
                HttpContext.Session.Clear();

                // Redirect to login page with success message
                TempData["SuccessMessage"] = "Votre compte a été supprimé avec succès.";
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                // Handle any errors during account deletion
                TempData["ErrorMessage"] = "Erreur lors de la suppression du compte : " + ex.Message;
                return RedirectToAction("DeleteAccount");
            }
        }

        // Helper method to get user by username
        private Useer GetUserByUsername(string username)
        {
            if (!System.IO.File.Exists(UsersFile))
            {
                return null;
            }

            var json = System.IO.File.ReadAllText(UsersFile);
            var users = JsonSerializer.Deserialize<Useer[]>(json);
            return users?.FirstOrDefault(u => u.Username == username);
        }

        // Helper method to validate user credentials
        private bool ValidateUser(string username, string password)
        {
            if (!System.IO.File.Exists(UsersFile))
            {
                return false;
            }

            var json = System.IO.File.ReadAllText(UsersFile);
            var users = JsonSerializer.Deserialize<Useer[]>(json);

            return users?.Any(u => u.Username == username && u.PasswordHash == HashPassword(password)) ?? false;
        }

        // Helper method to delete user
        private void DeleteUser(string username)
        {
            if (!System.IO.File.Exists(UsersFile))
            {
                return;
            }

            var json = System.IO.File.ReadAllText(UsersFile);
            var users = JsonSerializer.Deserialize<System.Collections.Generic.List<Useer>>(json);

            users?.RemoveAll(u => u.Username == username);

            var updatedJson = JsonSerializer.Serialize(users);
            System.IO.File.WriteAllText(UsersFile, updatedJson);
        }

        // Helper method to hash password (should match the one in UserService)
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }
}