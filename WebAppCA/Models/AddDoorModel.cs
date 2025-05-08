using System.ComponentModel.DataAnnotations;
using Xunit.Sdk;

namespace WebAppCA.Models
{
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
