using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SmartComply.Controllers
{
  public class AdminDashboardController : Controller
  {
    private readonly ApplicationDbContext _context;

    public AdminDashboardController(ApplicationDbContext context)
    {
      _context = context;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> GetDashboardData()
    {
      var auditorsCount = await _context.Staffs.CountAsync(s => s.StaffRole == "User" && s.StaffIsActive);
      var managersCount = await _context.Staffs.CountAsync(s => s.StaffRole == "Manager" && s.StaffIsActive);
      var activeBranches = await _context.Branches.CountAsync(b => b.BranchIsActive);
      var enabledCategories = await _context.ComplianceCategories.CountAsync(c => c.CategoryIsEnabled);

      return Json(new
      {
        auditorsCount,
        managersCount,
        activeBranches,
        enabledCategories
      });
    }

    [HttpGet]
    public async Task<IActionResult> GetFormDistribution()
    {
      var data = await _context.Forms
        .Include(f => f.Category)
        .GroupBy(f => f.Category.CategoryName)
        .Select(g => new { category = g.Key, count = g.Count() })
        .ToListAsync();

      return Json(data);
    }
  }
}
