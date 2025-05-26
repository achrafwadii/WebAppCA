using System;
using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    public class DeviceInfoModel
    {
        [Key]
        public int DeviceID { get; set; }

        [Required(ErrorMessage = "Le nom du dispositif est requis")]
        [Display(Name = "Nom du dispositif")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string DeviceName { get; set; }

        [Required(ErrorMessage = "L'adresse IP est requise")]
        [Display(Name = "Adresse IP")]
        [RegularExpression(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", 
            ErrorMessage = "Format d'adresse IP invalide")]
        public string IPAddress { get; set; }

        [Required(ErrorMessage = "Le port est requis")]
        [Display(Name = "Port")]
        [Range(1, 65535, ErrorMessage = "Le port doit être entre 1 et 65535")]
        public int Port { get; set; }

        [Display(Name = "État")]
        [StringLength(50)]
        public string Status { get; set; } = "Inactif";

        [Display(Name = "Dernière connexion")]
        [DataType(DataType.DateTime)]
        public DateTime? LastConnection { get; set; }

        [Display(Name = "Date de création")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Est actif")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Description")]
        [StringLength(255)]
        public string Description { get; set; }

        // Collection de portes associées
        public virtual ICollection<DoorInfoModel> Doors { get; set; } = new List<DoorInfoModel>();
    }
}