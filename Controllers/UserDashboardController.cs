using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartComply.Controllers
{
  public class UserDashboardController : Controller
  {
    private readonly ApplicationDbContext _context;

    public UserDashboardController(ApplicationDbContext context)
    {
      _context = context;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> GetMyAuditSummary()
    {
      var staffId = int.Parse(User.FindFirst("StaffId").Value);

      var audits = await _context.Audits
          .Where(a => a.StaffId == staffId)
          .ToListAsync();

      var result = new
      {
        Draft = audits.Count(a => a.Status == "Draft"),
        Done = audits.Count(a => a.Status == "Done"),
        Overdue = audits.Count(a => a.DueDate < DateTime.UtcNow && a.Status != "Done"),
        Rejected = audits.Count(a => a.Status == "Rejected")
      };

      return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetUpcomingDeadlines()
    {
      var staffId = int.Parse(User.FindFirst("StaffId").Value);

      var now = DateTime.UtcNow;
      var nextWeek = now.AddDays(7);

      var upcoming = await _context.Audits
          .Where(a => a.StaffId == staffId && a.Status == "Draft" && a.DueDate >= now && a.DueDate <= nextWeek)
          .OrderBy(a => a.DueDate)
          .Select(a => new { a.AuditName, DueDate = a.DueDate.ToString("yyyy-MM-dd") })
          .ToListAsync();

      return Json(upcoming);
    }
  }
}
