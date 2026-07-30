using Microsoft.AspNetCore.Mvc;

namespace PolicyProof.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult Privacy() => View();
}
