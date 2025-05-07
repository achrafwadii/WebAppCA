using System;
using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    // Modèle pour les informations de base d'une porte
    public class DoorInfoModel
    {
        public string DoorName { get; set; }    // Nom de la porte  
        public string DeviceName { get; set; }  // Nom de l’appareil ou identifiant  
        public byte RelayPort { get; set; }     // Numéro du relais (port)  
        public byte Mode { get; set; }
        public string Status { get; set; }      // État de la porte (ouvert/fermé) 
        public uint DoorID { get; set; }

        [Required(ErrorMessage = "Le nom de la porte est requis")]
        [StringLength(48, ErrorMessage = "Le nom ne peut pas dépasser 48 caractères")]
        public string Name { get; set; }

        [Required(ErrorMessage = "L'ID du dispositif est requis")]
        public uint DeviceID { get; set; }       

        // Propriétés supplémentaires pour lier avec PointAcces
        public int PointAccesId { get; set; }
        public string Description { get; set; }
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

    // Modèle pour l'ajout d'une porte
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