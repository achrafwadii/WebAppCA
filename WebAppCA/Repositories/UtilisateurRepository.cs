using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppCA.Data;
using WebAppCA.Models;

namespace WebAppCA.Repositories
{
    public class UtilisateurRepository
    {
        private readonly ApplicationDbContext _context;

        public UtilisateurRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Récupérer tous les utilisateurs
        public async Task<List<Utilisateur>> GetAllAsync()
        {
            return await _context.Utilisateurs.OrderBy(u => u.Nom).ToListAsync();
        }

        // Récupérer un utilisateur par ID
        public async Task<Utilisateur> GetByIdAsync(int id)
        {
            return await _context.Utilisateurs.FindAsync(id);
        }

        // Rechercher des utilisateurs avec filtres
        public async Task<List<Utilisateur>> SearchAsync(string searchTerm = null, string status = null, string departement = null)
        {
            var query = _context.Utilisateurs.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.Nom.Contains(searchTerm) ||
                                         u.Prenom.Contains(searchTerm) ||
                                         u.Email.Contains(searchTerm) ||
                                         u.BadgeNumber.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(u => u.Status == status);
            }

            if (!string.IsNullOrEmpty(departement))
            {
                query = query.Where(u => u.Departement == departement);
            }

            return await query.OrderBy(u => u.Nom).ToListAsync();
        }

        // Récupérer la liste des départements pour filtrage
        public async Task<List<string>> GetDepartmentsAsync()
        {
            return await _context.Utilisateurs
                .Where(u => u.Departement != null)
                .Select(u => u.Departement)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
        }

        // Ajouter un utilisateur
        public async Task<Utilisateur> AddAsync(Utilisateur utilisateur)
        {
            _context.Utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync();
            return utilisateur;
        }

        // Mettre à jour un utilisateur
        public async Task<Utilisateur> UpdateAsync(Utilisateur utilisateur)
        {
            _context.Entry(utilisateur).State = EntityState.Modified;

            // Prévenir la mise à jour de certains champs si nécessaire
            _context.Entry(utilisateur).Property(x => x.CreatedAt).IsModified = false;

            await _context.SaveChangesAsync();
            return utilisateur;
        }

        // Supprimer un utilisateur
        public async Task DeleteAsync(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur != null)
            {
                _context.Utilisateurs.Remove(utilisateur);
                await _context.SaveChangesAsync();
            }
        }
    }
}