using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    public class UserInfoModel
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        [Display(Name = "Nom d'utilisateur")]
        public string Username { get; set; }

        [Display(Name = "Prénom")]
        public string FirstName { get; set; }

        [Display(Name = "Nom")]
        public string LastName { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Téléphone")]
        public string Phone { get; set; }

        [Display(Name = "Département")]
        public string Department { get; set; }

        [Display(Name = "Position")]
        public string Position { get; set; }

        [Display(Name = "Numéro de badge")]
        public string BadgeNumber { get; set; }

        [Display(Name = "Type d'utilisateur")]
        public string UserType { get; set; } // Admin, Manager, Utilisateur

        [Display(Name = "État")]
        public string Status { get; set; } // Actif, Inactif, Suspendu

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Dernière connexion")]
        public DateTime? LastLogin { get; set; }

        // Relations avec d'autres entités
        [Display(Name = "Groupes d'accès")]
        public List<string> AccessGroups { get; set; }

        [Display(Name = "Portes accessibles")]
        public List<int> AccessibleDoors { get; set; }

        // Nouvelles propriétés pour les fonctionnalités supplémentaires
        [Display(Name = "PIN")]
        public string PIN { get; set; }

        [Display(Name = "Adresse IP")]
        public string IPAddress { get; set; }

        [Display(Name = "Mode d'authentification personnalisé")]
        public bool UseCustomAuthMode { get; set; }

        [Display(Name = "Niveau de sécurité")]
        public int SecurityLevel { get; set; } = 5;

        [Display(Name = "Opérateur BioStar")]
        public string BioStarOperator { get; set; } = "Jamais";

        [Display(Name = "Date de début")]
        public DateTime StartDate { get; set; } = new DateTime(2001, 1, 1);

        [Display(Name = "Heure de début")]
        public TimeSpan StartTime { get; set; } = TimeSpan.Zero;

        [Display(Name = "Date de fin")]
        public DateTime EndDate { get; set; } = new DateTime(2037, 12, 31);

        [Display(Name = "Heure de fin")]
        public TimeSpan EndTime { get; set; } = new TimeSpan(23, 59, 0);

        public string FullName => $"{FirstName} {LastName}";
    }
}