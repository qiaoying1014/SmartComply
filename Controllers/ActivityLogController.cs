using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartComply.Data;
using SmartComply.Models;
using SmartComply.ViewModels;
using System.Security.Claims;

public class ActivityLogController : Controller
{
  private readonly ApplicationDbContext _context;

  public ActivityLogController(ApplicationDbContext context)
  {
    _context = context;
  }

  public IActionResult Index()
  {
    var staffId = User.FindFirst("StaffId")?.Value;
    var staffRole = User.FindFirst(ClaimTypes.Role)?.Value;

    if (string.IsNullOrEmpty(staffId))
    {
      return RedirectToAction("Login", "Auth");
    }

    var staffIdInt = int.Parse(staffId);

    // Include Staff entity early to allow filtering by navigation properties
    IQueryable<ActivityLog> logsQuery = _context.ActivityLogs
        .Include(log => log.Staff);

    if (staffRole == "User")
    {
      logsQuery = logsQuery.Where(log => log.StaffId == staffIdInt);
    }
    else if (staffRole == "Manager")
    {
      var staffBranchId = _context.Staffs
          .Where(staff => staff.StaffId == staffIdInt)
          .Select(staff => staff.StaffBranchId)
          .FirstOrDefault();

      logsQuery = logsQuery.Where(log =>
          log.Staff.StaffBranchId == staffBranchId || log.StaffId == staffIdInt);
    }
    // Admins see all logs — no filtering needed

    var logs = logsQuery
        .OrderByDescending(log => log.ActionTimestamp)
        .ToList();

    foreach (var log in logs)
    {
      log.ActionTimestamp = log.ActionTimestamp.AddHours(8); // Malaysia time

      if (log.StaffId == staffIdInt)
      {
        log.ActionDescription = log.ActionDescription.Replace(
            $"Staff {log.Staff.StaffName} (ID: {log.StaffId})",
            "You");
      }
    }

    var groupedLogs = logs
        .GroupBy(log =>
            log.ActionTimestamp.Date == DateTime.UtcNow.Date ? "Today" :
            log.ActionTimestamp.Date == DateTime.UtcNow.AddDays(-1).Date ? "Yesterday" :
            log.ActionTimestamp.ToString("dd MMM yyyy"))
        .Select(group => new ActivityLogViewModel
        {
          GroupName = group.Key,
          Logs = group.Select(log => new ActivityLogDetails
          {
            LogId = log.LogId,
            StaffId = log.StaffId,
            ActionTimestamp = log.ActionTimestamp,
            ActionType = log.ActionType,
            ActionDescription = log.ActionDescription
          }).ToList()
        }).ToList();

    return View(groupedLogs);
  }
}
