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

        // DbSets pour les entités principales
        public DbSet<DeviceInfo> Devices { get; set; }
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Pointage> Pointages { get; set; }
        public DbSet<PointAcces> PointsAcces { get; set; }
        public DbSet<DoorInfoModel> Doors { get; set; }
        public DbSet<DoorStatusModel> DoorStatuses { get; set; }
        public DbSet<RecentActivityModel> RecentActivities { get; set; }
        public DbSet<RecentEventModel> RecentEvents { get; set; }

        // Tables de relations pour les groupes d'accès
        public DbSet<AccessGroup> AccessGroups { get; set; }
        public DbSet<UtilisateurAccessGroup> UtilisateurAccessGroups { get; set; }
        public DbSet<AccessGroupPointAcces> AccessGroupPointAcces { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration de la table Devices (DeviceInfo)
            modelBuilder.Entity<DeviceInfo>()
                .ToTable("Devices");

            modelBuilder.Entity<DeviceInfo>()
                .Property(d => d.DeviceName)
                .IsRequired(false)
                .HasMaxLength(100);

            modelBuilder.Entity<DeviceInfo>()
                .Property(d => d.Description)
                .IsRequired(false);

            modelBuilder.Entity<DeviceInfo>()
                .Property(d => d.Status)
                .IsRequired(false)
                .HasMaxLength(50);

            modelBuilder.Entity<DeviceInfo>()
                .Property(d => d.LastConnectionTime)
                .IsRequired(false);

            // Configuration de la table PointsAcces (PointAcces)
            modelBuilder.Entity<PointAcces>()
                .ToTable("PointsAcces");

            modelBuilder.Entity<PointAcces>()
                .Property(p => p.Nom)
                .IsRequired()
                .HasMaxLength(48);

            modelBuilder.Entity<PointAcces>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Configuration de la table Utilisateurs
            modelBuilder.Entity<Utilisateur>()
                .ToTable("Utilisateurs");

            modelBuilder.Entity<Utilisateur>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            modelBuilder.Entity<Utilisateur>()
                .Property(u => u.Nom)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Utilisateur>()
                .Property(u => u.Prenom)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Utilisateur>()
                .Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(100);

            // Configuration de la table Pointages
            modelBuilder.Entity<Pointage>()
                .ToTable("Pointages");

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

            // Configuration pour DoorInfoModel (table Doors)
            modelBuilder.Entity<DoorInfoModel>()
                .ToTable("Doors");

            modelBuilder.Entity<DoorInfoModel>()
                .HasKey(d => d.DoorID);

            modelBuilder.Entity<DoorInfoModel>()
                .Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(48);

            // Configuration pour DoorStatusModel
            modelBuilder.Entity<DoorStatusModel>()
                .ToTable("DoorStatuses");

            modelBuilder.Entity<DoorStatusModel>()
                .HasKey(d => d.DoorID);

            // Configuration pour RecentActivityModel
            modelBuilder.Entity<RecentActivityModel>()
                .ToTable("RecentActivities");

            modelBuilder.Entity<RecentActivityModel>()
                .Property(r => r.UserName)
                .IsRequired()
                .HasMaxLength(200);

            modelBuilder.Entity<RecentActivityModel>()
                .Property(r => r.AccessPoint)
                .IsRequired()
                .HasMaxLength(48);

            modelBuilder.Entity<RecentActivityModel>()
                .Property(r => r.EventType)
                .IsRequired()
                .HasMaxLength(50);

            // Configuration pour RecentEventModel
            modelBuilder.Entity<RecentEventModel>()
                .ToTable("RecentEvents");

            modelBuilder.Entity<RecentEventModel>()
                .Property(r => r.EventType)
                .IsRequired()
                .HasMaxLength(50);

            modelBuilder.Entity<RecentEventModel>()
                .Property(r => r.Description)
                .IsRequired();

            // Configuration pour AccessGroup
            modelBuilder.Entity<AccessGroup>()
                .ToTable("AccessGroups");

            modelBuilder.Entity<AccessGroup>()
                .Property(a => a.Name)
                .IsRequired()
                .HasMaxLength(100);

            // Configuration pour les tables de relation
            modelBuilder.Entity<UtilisateurAccessGroup>()
                .ToTable("UtilisateurAccessGroups")
                .HasKey(u => new { u.UtilisateurId, u.AccessGroupId });

            modelBuilder.Entity<UtilisateurAccessGroup>()
                .HasOne<Utilisateur>()
                .WithMany()
                .HasForeignKey(u => u.UtilisateurId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UtilisateurAccessGroup>()
                .HasOne<AccessGroup>()
                .WithMany()
                .HasForeignKey(u => u.AccessGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AccessGroupPointAcces>()
                .ToTable("AccessGroupPointAcces")
                .HasKey(a => new { a.AccessGroupId, a.PointAccesId });

            modelBuilder.Entity<AccessGroupPointAcces>()
                .HasOne<AccessGroup>()
                .WithMany()
                .HasForeignKey(a => a.AccessGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AccessGroupPointAcces>()
                .HasOne<PointAcces>()
                .WithMany()
                .HasForeignKey(a => a.PointAccesId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    // Classes pour les tables de relation
    public class AccessGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class UtilisateurAccessGroup
    {
        public int UtilisateurId { get; set; }
        public int AccessGroupId { get; set; }
    }

    public class AccessGroupPointAcces
    {
        public int AccessGroupId { get; set; }
        public int PointAccesId { get; set; }
    }
}