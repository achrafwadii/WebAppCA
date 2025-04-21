using Microsoft.AspNetCore.Mvc;

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
}