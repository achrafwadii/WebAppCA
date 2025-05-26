using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppCA.Models
{
    public class PointAcces
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "ID de la porte")]
        public int DoorID { get; set; }

        [Required]
        [Display(Name = "Nom")]
        [StringLength(48, ErrorMessage = "Le nom ne peut pas dépasser 48 caractères")]
        public string Nom { get; set; }

        [Required]
        [Display(Name = "ID du dispositif")]
        public uint DeviceID { get; set; }

        [Required]
        [Display(Name = "Port du relais")]
        [Range(0, 255)]
        public byte RelayPort { get; set; }

        [Display(Name = "Description")]
        [StringLength(255)]
        public string Description { get; set; }

        [Display(Name = "Est verrouillé")]
        public bool EstVerrouille { get; set; } = true;

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Date de mise à jour")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Est actif")]
        public bool EstActif { get; set; } = true;

        [Display(Name = "Niveau de sécurité")]
        [Range(1, 5)]
        public int NiveauSecurite { get; set; } = 1;

        // Collection des pointages
        public virtual ICollection<Pointage> Pointages { get; set; } = new List<Pointage>();

        // Relation avec la porte
        [ForeignKey("DoorID")]
        public virtual DoorInfoModel Door { get; set; }
    }
}