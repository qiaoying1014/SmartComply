using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartComply.Models;

namespace SmartComply.Data
{
  public class ApplicationDbContext : IdentityDbContext<Users>
  {
    public ApplicationDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<ComplianceCategory> ComplianceCategories { get; set; }
    public DbSet<Form> Forms { get; set; }
    public DbSet<FormElement> FormElements { get; set; }
    public DbSet<FormResponder> FormResponders { get; set; }
    public DbSet<FormResponse> FormResponses { get; set; }
    public DbSet<CorrectiveAction> CorrectiveActions { get; set; }
    public DbSet<Audit> Audits { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);


      var admin = new Staff
      {
        StaffId = 12, // Required for seeding. Cannot be auto-generated.
        StaffName = "Admin",
        StaffEmail = "admin@gmail.com",
        StaffPassword = "AQAAAAIAAYagAAAAEFU3VYzAtINidq7KyBiehNaIuFHYTMzDVDb+Ar/dY5DQnARn6xNgckqWwUDrLe1plA==",
        StaffIsActive = true,
        StaffRole = "Admin",
        StaffBranchId = null,
      };

      var manager = new Staff
      {
        StaffId = 13, // Required for seeding. Cannot be auto-generated.
        StaffName = "Manager",
        StaffEmail = "manager@gmail.com",
        StaffPassword = "AQAAAAIAAYagAAAAEJBLqq+TcLFGRYUXS5RKYJfOA3jrpvtXf8oWzfIQ8lDBLi2GzwhAwQKF2BVm/9OW3w==",
        StaffIsActive = true,
        StaffRole = "Manager",
        StaffBranchId = null,
      };

      var user = new Staff
      {
        StaffId = 14, // Required for seeding. Cannot be auto-generated.
        StaffName = "User",
        StaffEmail = "user@gmail.com",
        StaffPassword = "AQAAAAIAAYagAAAAEE/oLb4iyXp5FZ4e3rbNVykNPFPWXHvG6+AdPJBVUpV5bL8ZqPsmvMPFaZY8D014fQ==",
        StaffIsActive = true,
        StaffRole = "User",
        StaffBranchId = 4,
      };

      modelBuilder.Entity<Staff>().HasData(admin);
      modelBuilder.Entity<Staff>().HasData(manager);
      modelBuilder.Entity<Staff>().HasData(user);

      // Convert FormStatus enum to string in the database
      modelBuilder.Entity<Form>()
                  .Property(f => f.Status)
                  .HasConversion<string>();

      // Map CorrectiveAction dates and timestamps
      modelBuilder.Entity<CorrectiveAction>(entity =>
      {
        // Tell EF to store TargetDate as a plain "date" column
        entity.Property(e => e.TargetDate)
              .HasColumnType("date");

        // Likewise, store CompletionDate as a plain "date"
        entity.Property(e => e.CompletionDate)
              .HasColumnType("date");

        // Store CreatedAt/UpdatedAt as "timestamp with time zone"
        entity.Property(e => e.CreatedAt)
              .HasColumnType("timestamp with time zone");

        entity.Property(e => e.UpdatedAt)
              .HasColumnType("timestamp with time zone");
      });
    }
  }
}
