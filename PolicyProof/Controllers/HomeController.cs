using Microsoft.AspNetCore.Mvc;

namespace PolicyProof.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Upload", "Analysis");
}
