using System;
using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    public class DeviceInfoModel
    {
        [Required]
        public int DeviceID { get; set; }

        [Display(Name = "Nom")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string DeviceName { get; set; }

        [Display(Name = "Adresse IP")]
        [RegularExpression(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", ErrorMessage = "Adresse IP invalide")]
        public string IPAddress { get; set; }

        [Display(Name = "Port")]
        [Range(0, 65535, ErrorMessage = "Le port doit être entre 0 et 65535")]
        public int Port { get; set; }

        [Display(Name = "État")]
        public string Status { get; set; }

        [Display(Name = "Dernière connexion")]
        [DataType(DataType.DateTime)]
        public DateTime? LastConnection { get; set; }
    }
}