// WebAppCA/Services/DashboardService.cs
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppCA.Data;
using WebAppCA.Models;

namespace WebAppCA.Services
{
    public class DashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(string timeFrame = "today")
        {
            var dashboard = new DashboardViewModel();

            try
            {
                // Récupérer les compteurs généraux
                dashboard.UserCount = await _context.Utilisateurs.CountAsync();
                dashboard.DeviceCount = await _context.Devices.CountAsync();

                // Compter le nombre de portes différentes dans les pointages
                // Normalement, cela devrait venir d'une table dédiée aux portes
                dashboard.DoorCount = await _context.PointsAcces.CountAsync();

                // Obtenir le timestamp de début en fonction du timeFrame
                DateTime startDate = DateTime.Today; // Par défaut aujourd'hui

                if (timeFrame == "week")
                {
                    // Début de la semaine (lundi)
                    int diff = (7 + (DateTime.Today.DayOfWeek - DayOfWeek.Monday)) % 7;
                    startDate = DateTime.Today.AddDays(-diff);
                }
                else if (timeFrame == "month")
                {
                    // Début du mois
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                }

                // Compter les accès pour la période spécifiée
                dashboard.TodayAccessCount = await _context.Pointages
                    .Where(p => p.Date >= startDate)
                    .CountAsync();

                // Récupérer les activités récentes
                dashboard.RecentActivities = await _context.Pointages
                    .Include(p => p.Utilisateur)
                    .Include(p => p.PointAcces)
                    .OrderByDescending(p => p.Date)
                    .ThenByDescending(p => p.HeureEntree)
                    .Take(10)
                    .Select(p => new RecentActivityModel
                    {
                        Id = p.Id,
                        UserName = $"{p.Utilisateur.Prenom} {p.Utilisateur.Nom}",
                        AccessPoint = p.PointAcces.Nom,
                        Timestamp = p.Date.Date.Add(p.HeureEntree.TimeOfDay),
                        EventType = p.HeureSortie.HasValue ? "Sortie" : "Entrée"
                    })
                    .ToListAsync();

                // Pour l'exemple, nous allons simuler des événements récents
                // Dans une implémentation réelle, cela pourrait être un journal d'événements système
                dashboard.RecentEvents = await Task.FromResult(new List<RecentEventModel>
                {
                    new RecentEventModel {
                        Id = 1,
                        EventType = "Système",
                        Description = "Démarrage du service",
                        Timestamp = DateTime.Now.AddHours(-1)
                    },
                    new RecentEventModel {
                        Id = 2,
                        EventType = "Alerte",
                        Description = "Tentative d'accès non autorisé",
                        Timestamp = DateTime.Now.AddHours(-2)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des données du tableau de bord");
                // Retourner un tableau de bord avec des valeurs par défaut
            }

            return dashboard;
        }
    }
}