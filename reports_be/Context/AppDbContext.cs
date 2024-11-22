using Microsoft.EntityFrameworkCore;
using reports_be.Models;
using Host = Microsoft.Extensions.Hosting.Host;

namespace reports_be.Context;

//TODO: Overhaul
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<RestorePoint> RestorePoints => Set<RestorePoint>();
    public DbSet<VbrHost> VbrHost => Set<VbrHost>();
    
    public DbSet<BackedUpHost> BackedUpHosts => Set<BackedUpHost>();
    public DbSet<BackupStatus> BackupStatus => Set<BackupStatus>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<BackupStatus>().HasNoKey().ToView("BackupStatus");
        
        modelBuilder.Entity<VbrHost>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.HasMany(h => h.BackedUpHosts)
                .WithOne(bh => bh.VbrHost)
                .HasForeignKey(bh => bh.VbrId);
        });

        modelBuilder.Entity<BackedUpHost>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.HasMany(rp => rp.RestorePoint)
                .WithOne(h => h.BackedUpHost)
                .HasForeignKey(h => h.BHostId);
        });

        // Configure the RestorePoint entity
        modelBuilder.Entity<RestorePoint>(entity =>
        {
            // Composite key is on (HostId, Date, ParentJob)
            entity.HasKey(rp => new { rp.VbrId, rp.BHostId, rp.Date, rp.ParentJob });

            // FK Relationship (BackedUpHost has a one to many relationship with RestorePoint)
            entity.HasOne(rp => rp.BackedUpHost) // Navigation property in RestorePoint
                .WithMany(h => h.RestorePoint) // Navigation property ( VbrHost -> RestoreP )
                .HasForeignKey(rp => rp.BHostId) // FK on HostId
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete if a BHost is deleted
            // FK Relationship (VbrHost has a one to many relationship with RestorePoints)
            entity.HasOne(rp => rp.VbrHost)
                .WithMany(v => v.RestorePoints)
                .HasForeignKey(rp => rp.VbrId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}