using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace WebAppCA.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("IsAuthenticated") != "true")
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Attendance()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult Menu()
        {
            return View();
        }

        public IActionResult Doors()
        {
            // Redirect to DoorController.Index (note the singular Door)
            return RedirectToAction("Index", "Door");
        }
        public IActionResult Welcome()
        {
            // Cette action affichera la page d'accueil sans vérifier l'authentification
            return View();
        }



    }
}