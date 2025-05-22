using System;
using System.ComponentModel.DataAnnotations;
using WebAppCA.Controllers;

namespace WebAppCA.Models
{

    public class DoorInfoModel
    {
        public long DoorID { get; set; }

        [Required(ErrorMessage = "Le nom de la porte est requis")]
        [Display(Name = "Nom de la porte")]
        public string Name { get; set; }

        [Required(ErrorMessage = "L'ID du dispositif est requis")]
        [Display(Name = "ID du dispositif")]
        public uint DeviceID { get; set; }

        [Required(ErrorMessage = "Le port du relais est requis")]
        [Display(Name = "Port du relais")]
        [Range(0, 255, ErrorMessage = "Le port du relais doit être entre 0 et 255")]
        public byte RelayPort { get; set; }

        [Display(Name = "Nom du dispositif")]
        public string DeviceName { get; set; }

        [Display(Name = "Statut")]
        public string Status { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "ID du point d'accès")]
        public int PointAccesId { get; set; }

        [Display(Name = "Dernière activité")]
        public DateTime? LastActivity { get; set; }
        public bool EstOuverte { get; internal set; }
    }


   

    
}