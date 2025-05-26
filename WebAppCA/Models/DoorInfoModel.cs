using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebAppCA.Controllers;

namespace WebAppCA.Models
{
    public class DoorInfoModel
    {
        [Key]
        public int DoorID { get; set; }

        [Required(ErrorMessage = "Le nom de la porte est requis")]
        [Display(Name = "Nom de la porte")]
        [StringLength(48, ErrorMessage = "Le nom ne peut pas dépasser 48 caractères")]
        public string Name { get; set; }

        [Required(ErrorMessage = "L'ID du dispositif est requis")]
        [Display(Name = "ID du dispositif")]
        public int DeviceID { get; set; }

        [Required(ErrorMessage = "Le port du relais est requis")]
        [Display(Name = "Port du relais")]
        [Range(0, 255, ErrorMessage = "Le port du relais doit être entre 0 et 255")]
        public byte RelayPort { get; set; }

        [Display(Name = "Description")]
        [StringLength(255, ErrorMessage = "La description ne peut pas dépasser 255 caractères")]
        public string Description { get; set; }

        // Option A: Exclure complètement ces propriétés de la base de données
        [NotMapped]
        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        [Display(Name = "Dernière mise à jour")]
        public DateTime? UpdatedAt { get; set; }

        // Propriétés de navigation et calculées (déjà NotMapped)
        [NotMapped]
        [Display(Name = "Nom du dispositif")]
        public string DeviceName { get; set; }

        [NotMapped]
        [Display(Name = "Statut")]
        public string Status { get; set; }

        [NotMapped]
        [Display(Name = "ID du point d'accès")]
        public int PointAccesId { get; set; }

        [NotMapped]
        [Display(Name = "Dernière activité")]
        public DateTime? LastActivity { get; set; }

        [NotMapped]
        public bool EstOuverte { get; set; }

        [NotMapped]
        public bool EstVerrouille { get; set; } = true;

        // Relations
        [ForeignKey("DeviceID")]
        public virtual DeviceInfoModel Device { get; set; }

        public virtual DoorStatusModel DoorStatus { get; set; }
    }

    // Reste des classes inchangé...
    public class DoorStatusResponse
    {
        public int DoorID { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public bool IsLocked { get; set; }
        public DateTime LastModified { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class ToggleDoorRequest
    {
        [Required]
        public int DoorID { get; set; }
        [StringLength(255)]
        public string Reason { get; set; }
        public bool ForceAction { get; set; } = false;
    }

    public class DoorHistoryViewModel
    {
        public DoorInfoModel Door { get; set; }
        public PointAcces PointAcces { get; set; }
        public List<Pointage> Pointages { get; set; } = new List<Pointage>();
        public List<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
        public int TotalEntries { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 20;
    }
}