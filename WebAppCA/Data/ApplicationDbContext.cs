using Microsoft.EntityFrameworkCore;
using WebAppCA.Models;

namespace WebAppCA.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
            public DbSet<DeviceInfo> Devices { get; set; }
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Pointage> Pointages { get; set; }
        public DbSet<PointAcces> PointsAcces { get; set; }
        public DbSet<DoorInfoModel> Doors { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration des relations
            modelBuilder.Entity<Pointage>()
                .HasOne(p => p.Utilisateur)
                .WithMany()
                .HasForeignKey(p => p.UtilisateurId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pointage>()
                .HasOne(p => p.PointAcces)
                .WithMany()
                .HasForeignKey(p => p.PointAccesId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Pointage>()
                .HasOne(p => p.Utilisateur)
                .WithMany()
                .HasForeignKey(p => p.UtilisateurId)
                .OnDelete(DeleteBehavior.Restrict);
            // Configuration de la table Utilisateurs
            modelBuilder.Entity<Utilisateur>()
                .ToTable("Utilisateurs");
            modelBuilder.Entity<Utilisateur>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Pointage>()
                .HasOne(p => p.PointAcces)
                .WithMany()
                .HasForeignKey(p => p.PointAccesId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure DeviceInfo nullable properties
            modelBuilder.Entity<DeviceInfo>()
                .Property(d => d.DeviceName)
                .IsRequired(false);

            modelBuilder.Entity<DeviceInfo>()
                .Property(d => d.Description)
                .IsRequired(false);

            modelBuilder.Entity<DeviceInfo>()
                .Property(d => d.Status)
                .IsRequired(false);

            modelBuilder.Entity<DeviceInfo>()
                .Property(d => d.LastConnectionTime)
                .IsRequired(false);
            // Configuration pour DoorInfoModel
            modelBuilder.Entity<DoorInfoModel>()
                .HasKey(d => d.DoorID);

            modelBuilder.Entity<DoorInfoModel>()
                .Property(d => d.Name)
                .HasMaxLength(48);
        }
    }
}