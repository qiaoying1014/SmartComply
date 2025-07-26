using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data;
using SmartComply.Models;
using SmartComply.ViewModels;

namespace SmartComply.Controllers
{
  public class ManagerController : Controller
  {
    private readonly ApplicationDbContext _context;

    public ManagerController(ApplicationDbContext context)
    {
      _context = context;
    }
    public IActionResult Index()
    {
      return View();
    }

  }
}
