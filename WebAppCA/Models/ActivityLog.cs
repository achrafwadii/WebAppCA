using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppCA.Models
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Date et heure")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Type d'action")]
        [StringLength(50)]
        public string ActionType { get; set; }

        [Required]
        [Display(Name = "Description")]
        [StringLength(255)]
        public string Description { get; set; }

        [Display(Name = "ID de la porte")]
        public int? DoorID { get; set; }

        [Display(Name = "ID de l'utilisateur")]
        public int? UtilisateurId { get; set; }

        [Display(Name = "Adresse IP")]
        [StringLength(45)] // Pour IPv6
        public string IPAddress { get; set; }

        [Display(Name = "Niveau de sévérité")]
        [StringLength(20)]
        public string Severity { get; set; } = "Info"; // Info, Warning, Error, Critical

        [Display(Name = "Détails supplémentaires")]
        public string AdditionalData { get; set; } // JSON pour données supplémentaires

        // Relations de navigation
        [ForeignKey("DoorID")]
        public virtual DoorInfoModel Door { get; set; }

        [ForeignKey("UtilisateurId")]
        public virtual Utilisateur Utilisateur { get; set; }
    }
}

