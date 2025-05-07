using Microsoft.EntityFrameworkCore;
using ConnectThruEventsBackend.Models;

namespace ConnectThruEventsBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSet Properties
        public DbSet<User> Users { get; set; }
        public DbSet<EventManager> EventManagers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<EventDetail> EventDetails { get; set; }
        public DbSet<EmployeeAssignment> EmployeeAssignments { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<CreatedEvent> CreatedEvents { get; set; }
        public DbSet<PublishEvent> PublishEvents{get; set;}
        public DbSet<EventEnrollment> EventEnrollments { get; set; }
        public DbSet<FavoriteEvent> FavoriteEvents { get; set; }

     protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Unique Indexes
    modelBuilder.Entity<User>()
        .HasIndex(u => u.Email)
        .IsUnique();

    modelBuilder.Entity<Admin>()
        .HasIndex(a => a.Email)
        .IsUnique();

    // User ↔ EventManager (One-to-One)
    modelBuilder.Entity<EventManager>()
        .HasOne(em => em.User)
        .WithOne(u => u.EventManager)
        .HasForeignKey<EventManager>(em => em.UserId)
        .OnDelete(DeleteBehavior.Cascade);

    // EventManager ↔ Events (One-to-Many)
    modelBuilder.Entity<Event>()
        .HasOne(e => e.EventManager)
        .WithMany(em => em.Events)
        .HasForeignKey(e => e.EventManagerId)
        .OnDelete(DeleteBehavior.Cascade);

    // EventManager ↔ Tasks (One-to-Many)
    modelBuilder.Entity<TaskItem>()
        .HasOne(t => t.EventManager)
        .WithMany(e => e.Tasks)
        .HasForeignKey(t => t.EventManagerId)
        .OnDelete(DeleteBehavior.Cascade);

    // EmployeeAssignment ↔ EventDetail (Many-to-One) — Keep this Cascade
    modelBuilder.Entity<EmployeeAssignment>()
        .HasOne(e => e.EventDetail)
        .WithMany(ed => ed.EmployeeAssignments)
        .HasForeignKey(e => e.EventDetailId)
        .OnDelete(DeleteBehavior.Cascade);

    // TaskAssignment ↔ EmployeeAssignment — Keep Cascade
    modelBuilder.Entity<TaskAssignment>()
        .HasOne(ta => ta.EmployeeAssignment)
        .WithMany(ea => ea.TaskAssignments)
        .HasForeignKey(ta => ta.EmployeeAssignmentId)
        .OnDelete(DeleteBehavior.Cascade);

    // TaskAssignment ↔ EventDetail — 🔧 FIX: Change to NoAction
    modelBuilder.Entity<TaskAssignment>()
        .HasOne(ta => ta.EventDetail)
        .WithMany(ed => ed.TaskAssignments)
        .HasForeignKey(ta => ta.EventDetailId)
        .OnDelete(DeleteBehavior.NoAction);

    // Decimal precision setup
    modelBuilder.Entity<Event>()
        .Property(e => e.BudgetUtilization)
        .HasColumnType("decimal(18, 2)");

    modelBuilder.Entity<EventManager>()
        .Property(em => em.PricePerEvent)
        .HasColumnType("decimal(18, 2)");

    modelBuilder.Entity<TaskAssignment>()
        .Property(ta => ta.TaskCost)
        .HasColumnType("decimal(18, 2)");

        modelBuilder.Entity<CreatedEvent>()
        .HasOne(e => e.User)
        .WithMany()
        .HasForeignKey(e => e.UserId)
        .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

    modelBuilder.Entity<CreatedEvent>()
        .HasOne(e => e.EventManager)
        .WithMany()
        .HasForeignKey(e => e.EventManagerId)
        .OnDelete(DeleteBehavior.Restrict); //Prevent cascade delete

    modelBuilder.Entity<EventEnrollment>()
        .HasOne(e => e.User)
        .WithMany() // or `.WithMany(u => u.Enrollments)` if you've added a navigation property in User
        .HasForeignKey(e => e.UserId);

    modelBuilder.Entity<EventEnrollment>()
        .HasOne(e => e.PublishEvent)
        .WithMany(p => p.Enrollments)
        .HasForeignKey(e => e.PublishEventId);

    // Admin seeding
    modelBuilder.Entity<Admin>().HasData(new Admin
    {
        Id = Guid.Parse("d4e8fa5e-4c1b-4a8b-9c1a-5a53dfb6e688"),
        FullName = "Admin User",
        Email = "admin@example.com",
        PasswordHash = "$2a$11$8USc88gZHguCgzNv5ax8RuUxgZlNiDBZKcBIbVnuNX1Bg628VLGsy", // Replace with real hash
        CreatedAt = new DateTime(2024, 2, 25)
    });

    // Add additional seeds here if needed
 }
}
}
