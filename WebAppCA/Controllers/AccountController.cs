using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using WebAppCA.Models;
using WebAppCA.Services;

// Controllers/AccountController.cs
public class AccountController : Controller
{
    private readonly UserService _userService;

    public AccountController(UserService userService)
    {
        _userService = userService;
    }

    // GET: /Account/Register
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    public IActionResult Register(string username, string email, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            ViewBag.ErrorMessage = "Les mots de passe ne correspondent pas";
            return View();
        }

        try
        {
            _userService.Register(new Useer { Username = username, Email = email }, password);
            TempData["SuccessMessage"] = "Inscription réussie!";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = ex.Message;
            return View();
        }
    }

    // GET: /Account/Login
    public IActionResult Login()
    {
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.ErrorMessage = "Veuillez remplir tous les champs.";
            return View();
        }

        try
        {
            var user = _userService.Login(username, password);

            HttpContext.Session.SetString("IsAuthenticated", "true");
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);

            return RedirectToAction("Dashboard", "Home");
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = ex.Message;
            return View();
        }
    }
}