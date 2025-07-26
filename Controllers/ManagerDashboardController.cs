using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SmartComply.Controllers
{
  [Route("Manager/Index")]
  public class ManagerDashboardController : Controller
  {
    private readonly ApplicationDbContext _context;

    public ManagerDashboardController(ApplicationDbContext context)
    {
      _context = context;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
      return View("~/Views/Manager/Index.cshtml");
    }
    private int GetLoggedInManagerBranchId()
    {
      var staffIdClaim = User.FindFirst("StaffId")?.Value;
      if (int.TryParse(staffIdClaim, out int staffId))
      {
        var branchId = _context.Staffs
            .Where(s => s.StaffId == staffId)
            .Select(s => s.StaffBranchId)
            .FirstOrDefault();

        return (int)branchId;
      }

      return 0; // or throw an exception if preferred
    }

    [HttpGet("GetAuditorPerformanceData")]
    public async Task<IActionResult> GetAuditorPerformanceData()
    {
      var branchId = GetLoggedInManagerBranchId();
      if (branchId == 0) return Unauthorized();

      var auditors = await _context.Staffs
          .Where(s => s.StaffRole == "User" && s.StaffIsActive && s.StaffBranchId == branchId)
          .Select(s => new { s.StaffId, s.StaffName })
          .ToListAsync();

      var staffIds = auditors.Select(s => s.StaffId).ToList(); // ToList() makes it usable in EF Core

      var auditStats = await _context.Audits
          .Where(a => a.StaffId.HasValue && staffIds.Contains(a.StaffId.Value))
          .GroupBy(a => a.StaffId)
          .Select(g => new {
            StaffId = g.Key,
            Draft = g.Count(a => a.Status == "Draft"),
            Done = g.Count(a => a.Status == "Done"),
            Overdue = g.Count(a => a.DueDate < DateTime.UtcNow && a.Status != "Done"),
            Rejected = g.Count(a => a.Status == "Rejected")
          }).ToListAsync();

      var result = auditors.Select(auditor => {
        var stat = auditStats.FirstOrDefault(x => x.StaffId == auditor.StaffId);
        return new
        {
          auditorName = auditor.StaffName,
          draftCount = stat?.Draft ?? 0,
          doneCount = stat?.Done ?? 0,
          overdueCount = stat?.Overdue ?? 0,
          rejectedCount = stat?.Rejected ?? 0
        };
      }).ToList();

      return Json(result);
    }

    [HttpGet("GetComplianceCategories")]
    public async Task<IActionResult> GetComplianceCategories()
    {
      var branchId = GetLoggedInManagerBranchId();
      if (branchId == 0) return Unauthorized();

      var categories = await _context.ComplianceCategories
          .Where(c => c.CategoryIsEnabled &&
            _context.Audits.Any(a => a.CategoryId == c.CategoryId && a.Staff.StaffBranchId == branchId)) 
          .Select(c => new { c.CategoryId, c.CategoryName })
          .ToListAsync();

      return Json(categories);
    }

    [HttpGet("GetComplianceSummary")]
    public async Task<IActionResult> GetComplianceSummary(int categoryId)
    {
      var branchId = GetLoggedInManagerBranchId();
      if (branchId == 0) return Unauthorized();

      var summary = await _context.Audits
          .Where(a => a.CategoryId == categoryId && a.Staff.StaffBranchId == branchId)
          .GroupBy(a => a.Status)
          .Select(g => new {
            status = g.Key,
            count = g.Count()
          }).ToListAsync();

      return Json(summary);
    }

    [HttpGet("GetNonComplianceTrend")]
    public async Task<IActionResult> GetNonComplianceTrend()
    {
      var branchId = GetLoggedInManagerBranchId();
      if (branchId == 0) return Unauthorized();

      var now = DateTime.UtcNow;
      var startOfMonth = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1), DateTimeKind.Utc);

      var data = await _context.Audits
        .Where(a => a.CreatedAt >= startOfMonth && a.Status == "Rejected" && a.Staff.StaffBranchId == branchId)
        .Select(a => new
        {
          Date = a.CreatedAt.Date
        })
        .ToListAsync();

      var grouped = data
        .GroupBy(a => a.Date)
        .Select(g => new
        {
          Date = g.Key,
          RejectedCount = g.Count()
        })
        .OrderBy(g => g.Date)
        .ToList();

      return Json(grouped);
    }


  }
}
