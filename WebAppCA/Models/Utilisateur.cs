using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppCA.Models
{
    public class Utilisateur
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [Display(Name = "Nom")]
        [StringLength(100)]
        public string Nom { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        [Display(Name = "Prénom")]
        [StringLength(100)]
        public string Prenom { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [Display(Name = "Email")]
        [StringLength(100)]
        public string Email { get; set; }

        [Display(Name = "Téléphone")]
        [StringLength(20)]
        public string Telephone { get; set; }

        [Display(Name = "Département")]
        [StringLength(50)]
        public string Departement { get; set; }

        [Display(Name = "Position")]
        [StringLength(50)]
        public string Position { get; set; }

        [Display(Name = "Numéro de badge")]
        [StringLength(20)]
        public string BadgeNumber { get; set; }

        [Display(Name = "PIN")]
        [StringLength(10)]
        public string PIN { get; set; }

        [Display(Name = "Type d'utilisateur")]
        [StringLength(20)]
        public string UserType { get; set; }

        [Display(Name = "Statut")]
        [StringLength(20)]
        public string Status { get; set; }

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Dernière connexion")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "Adresse IP")]
        [StringLength(50)]
        public string IPAddress { get; set; }

        [Display(Name = "Utiliser mode d'authentification personnalisé")]
        public bool UseCustomAuthMode { get; set; }

        [Display(Name = "Niveau de sécurité")]
        public int SecurityLevel { get; set; }

        [Display(Name = "Opérateur BioStar")]
        [StringLength(20)]
        public string BioStarOperator { get; set; }

        [Display(Name = "Date de début")]
        public DateTime StartDate { get; set; }
        
        [Display(Name = "Heure de début")]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "Heure de fin")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Date de fin")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Numéro de carte")]
        public string CardNumber { get; set; }

        [Display(Name = "Données biométriques")]
        public string BiometricData { get; set; }

        [Display(Name = "QR Code")]
        public string QRCode { get; set; }

        [Display(Name = "Face ID")]
        public byte[] FaceData { get; set; }

        // Propriétés de navigation (pour les relations)
        [NotMapped]
        public List<string> AccessGroups { get; set; } = new List<string>();

        [NotMapped]
        public List<int> AccessibleDoors { get; set; } = new List<int>();

        // Propriétés complémentaires pour l'affichage
        [NotMapped] // Si FullName est une propriété calculée
        public string FullName => $"{Prenom} {Nom}";

        [NotMapped]
        public string Username => $"{Prenom.ToLower()[0]}.{Nom.ToLower()}";
    }
}