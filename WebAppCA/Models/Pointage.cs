using System;
using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    public class Pointage
    {
        public int Id { get; set; }

        [Required]
        public int UtilisateurId { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Display(Name = "Heure d'entrée")]
        [DataType(DataType.Time)]
        public DateTime HeureEntree { get; set; }

        [Display(Name = "Heure de sortie")]
        [DataType(DataType.Time)]
        public DateTime? HeureSortie { get; set; }

        [Display(Name = "Durée")]
        public TimeSpan? Duree => HeureSortie.HasValue ? HeureSortie.Value - HeureEntree : null;

        [Required]
        [Display(Name = "Point d'accès")]
        public int PointAccesId { get; set; }

        // Propriétés de navigation
        public virtual Utilisateur Utilisateur { get; set; }
        public virtual PointAcces PointAcces { get; set; }
    }

    public class Utilisateur
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nom")]
        public string Nom { get; set; }

        [Required]
        [Display(Name = "Prénom")]
        public string Prenom { get; set; }

        [Display(Name = "Nom complet")]
        public string NomComplet => $"{Prenom} {Nom}";

        // Autres propriétés comme email, département, etc.
    }

    public class PointAcces
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nom")]
        public string Nom { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }
    }

    public class FiltrePresence
    {
        [DataType(DataType.Date)]
        [Display(Name = "Date de début")]
        public DateTime? DateDebut { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date de fin")]
        public DateTime? DateFin { get; set; }

        [Display(Name = "Utilisateur")]
        public int? UtilisateurId { get; set; }
    }
}