//  /Data/ApplicationDbContext.cs

using AppraisalPortal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppraisalPortal
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<AppraisalCycle> AppraisalCycles => Set<AppraisalCycle>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<Form> Forms => Set<Form>();
        public DbSet<Response> Responses => Set<Response>();

        // NEW
        public DbSet<Responsibility> Responsibilities => Set<Responsibility>();
        public DbSet<KPIItem> KPIItems => Set<KPIItem>();
        public DbSet<SoftSkillRating> SoftSkillRatings => Set<SoftSkillRating>();

        public DbSet<ManagerScope> ManagerScopes => Set<ManagerScope>();
        public DbSet<ManagerScopeDepartment> ManagerScopeDepartments => Set<ManagerScopeDepartment>();


        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Employee>()
             .HasOne(e => e.Manager)
             .WithMany()
             .HasForeignKey(e => e.ManagerId)
             .OnDelete(DeleteBehavior.NoAction);

            b.Entity<Question>().Property(q => q.Weight).HasPrecision(5, 2);
            b.Entity<Form>().Property(f => f.EmployeeScore).HasPrecision(5, 2);
            b.Entity<Form>().Property(f => f.ManagerScore).HasPrecision(5, 2);
            b.Entity<Form>().Property(f => f.FinalScore).HasPrecision(5, 2);

            b.Entity<ManagerScope>()
            .HasOne(ms => ms.ManagerEmployee)
            .WithMany() // no back-collection needed
            .HasForeignKey(ms => ms.ManagerEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

            b.Entity<ManagerScopeDepartment>()
              .HasOne(md => md.ManagerScope)
              .WithMany(ms => ms.Departments)
              .HasForeignKey(md => md.ManagerScopeId)
              .OnDelete(DeleteBehavior.Cascade);



            b.Entity<Responsibility>().Property(r => r.AchievementPercent).HasPrecision(5, 0);
            b.Entity<KPIItem>().Property(k => k.Score).HasPrecision(5, 0);
            b.Entity<SoftSkillRating>().Property(s => s.Score).HasPrecision(3, 0);


            b.Entity<Employee>().HasIndex(e => e.EmpCode).IsUnique();
            b.Entity<Employee>().HasIndex(e => e.ManagerId);
            b.Entity<Employee>().HasIndex(e => e.Department);
            b.Entity<Employee>().HasIndex(e => e.UserId);
            b.Entity<ManagerScope>().HasIndex(s => s.ManagerEmployeeId);

        }
    }
}