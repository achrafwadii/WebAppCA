using System;
using System.ComponentModel.DataAnnotations;
using static WebAppCA.Controllers.DoorController;

namespace WebAppCA.Models
{
   
        public class DoorInfoModel
        {
            public uint DoorID { get; set; }

            [Display(Name = "Nom")]
            [Required(ErrorMessage = "Le nom de la porte est requis")]
            [StringLength(48, ErrorMessage = "Le nom ne peut pas dépasser 48 caractères")]
            public string Name { get; set; }

            [Display(Name = "Équipement")]
            [Required(ErrorMessage = "L'ID du dispositif est requis")]
            public uint DeviceID { get; set; }

            [Display(Name = "Nom équipement")]
            public string DeviceName { get; set; }

            [Display(Name = "Port")]
            [Required(ErrorMessage = "Le port du relais est requis")]
            [Range(0, 255, ErrorMessage = "Le port doit être entre 0 et 255")]
            public byte RelayPort { get; set; }

            [Display(Name = "Statut")]
            public string Status { get; set; }

            [Display(Name = "Description")]
            [StringLength(255, ErrorMessage = "La description ne peut pas dépasser 255 caractères")]
            public string Description { get; set; }

            [Display(Name = "Point d'accès")]
            public int PointAccesId { get; set; }

            // Propriété conservée pour compatibilité
            public byte Mode { get; set; }
        }
    

    // Modèle pour le statut d'une porte
    public class DoorStatusModel
    {
        public uint DoorID { get; set; }
        public bool IsOpen { get; set; }
        public bool IsUnlocked { get; set; }
        public bool HeldOpen { get; set; }
        public uint AlarmFlags { get; set; }

        public string StatusText => IsUnlocked ? "Déverrouillée" : "Verrouillée";
        public string StateText => IsOpen ? "Ouverte" : "Fermée";
        public string AlarmText
        {
            get
            {
                if (AlarmFlags == 0) return "Aucune";
                var result = string.Empty;

                if ((AlarmFlags & 0x01) != 0) result += "Ouverture forcée, ";
                if ((AlarmFlags & 0x02) != 0) result += "Maintenue ouverte, ";
                if ((AlarmFlags & 0x04) != 0) result += "Violation APB, ";

                return result.TrimEnd(' ', ',');
            }
        }
    }

    public class AddDoorModel
    {
        [Required(ErrorMessage = "Le nom de la porte est requis")]
        [StringLength(48, ErrorMessage = "Le nom ne peut pas dépasser 48 caractères")]
        public string DoorName { get; set; }

        [Required(ErrorMessage = "L'ID du dispositif est requis")]
        public uint DeviceID { get; set; }

        [Required(ErrorMessage = "Le port du relais est requis")]
        [Range(0, 255, ErrorMessage = "Le port doit être entre 0 et 255")]
        public int PortNumber { get; set; }

        // Ajout d'un champ de description
        [StringLength(255, ErrorMessage = "La description ne peut pas dépasser 255 caractères")]
        public string Description { get; set; }
    }
}