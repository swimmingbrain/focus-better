using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonkMode.Domain.Models;
using MonkMode.Infrastructure.Identity;

namespace MonkMode.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // define dbsets
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<RecurringTask> RecurringTasks { get; set; }
        public DbSet<TimeBlock> TimeBlocks { get; set; }
        public DbSet<FocusSession> FocusSessions { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ExportSettings> ExportSettings { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // rename AspNetUsers table to keep Entity Framework from complaining
            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");

            modelBuilder.Entity<User>().ToTable("Users");

            // user
            modelBuilder.Entity<User>()
                .HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Tasks)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.TimeBlocks)
                .WithOne(tb => tb.User)
                .HasForeignKey(tb => tb.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.FocusSessions)
                .WithOne(fs => fs.User)
                .HasForeignKey(fs => fs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // friendship (bidirectional)
            modelBuilder.Entity<User>()
                .HasMany(u => u.SentFriendRequests)
                .WithOne(f => f.Requester)
                .HasForeignKey(f => f.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasMany(u => u.ReceivedFriendRequests)
                .WithOne(f => f.Requestee)
                .HasForeignKey(f => f.RequesteeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(u => u.ExportSettings)
                .WithOne(e => e.User)
                .HasForeignKey<ExportSettings>(e => e.UserId);

            // task
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.RecurringConfiguration)
                .WithOne(r => r.Task)
                .HasForeignKey<RecurringTask>(r => r.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // n-zu-m: TaskItem <-> TimeBlock
            modelBuilder.Entity<TaskItem>()
                .HasMany(t => t.LinkedTimeBlocks)
                .WithMany(tb => tb.LinkedTasks)
                .UsingEntity(j => j.ToTable("TaskTimeBlockMapping"));

            // export settings
            modelBuilder.Entity<ExportSettings>()
                .HasMany(e => e.Events)
                .WithOne(ce => ce.Settings)
                .HasForeignKey(ce => ce.ExportSettingsId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}