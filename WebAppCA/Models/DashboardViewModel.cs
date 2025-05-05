// WebAppCA/Models/DashboardViewModel.cs
using System;
using System.Collections.Generic;

namespace WebAppCA.Models
{
    public class DashboardViewModel
    {
        // Statistiques générales
        public int UserCount { get; set; }
        public int DeviceCount { get; set; }
        public int DoorCount { get; set; }
        public int TodayAccessCount { get; set; }

        // Activité récente
        public List<RecentActivityModel> RecentActivities { get; set; }

        // Événements récents
        public List<RecentEventModel> RecentEvents { get; set; }

        public DashboardViewModel()
        {
            RecentActivities = new List<RecentActivityModel>();
            RecentEvents = new List<RecentEventModel>();
        }
    }

    public class RecentActivityModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string AccessPoint { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; } // Entrée, Sortie, etc.
    }

    public class RecentEventModel
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
    }
}