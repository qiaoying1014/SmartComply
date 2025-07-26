using Microsoft.AspNetCore.Mvc;

namespace SmartComply.Controllers
{
  public class AdminController : Controller
  {
    public IActionResult Index()
    {
      return View();
    }
  }
}
