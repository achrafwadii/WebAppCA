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

        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Pointage> Pointages { get; set; }
        public DbSet<PointAcces> PointsAcces { get; set; }

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
        }
    }
}