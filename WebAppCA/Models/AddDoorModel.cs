using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    public class AddDoorModel
    {
        [Required(ErrorMessage = "Le nom de la porte est requis")]
        [StringLength(48, ErrorMessage = "Le nom ne peut pas dépasser 48 caractères")]
        [Display(Name = "Nom de la porte")]
        public string DoorName { get; set; }

        [Required(ErrorMessage = "L'ID du dispositif est requis")]
        [Display(Name = "ID du dispositif")]
        public uint DeviceID { get; set; }

        [Required(ErrorMessage = "Le port du relais est requis")]
        [Display(Name = "Port du relais")]
        [Range(0, 255, ErrorMessage = "Le port du relais doit être entre 0 et 255")]
        public int PortNumber { get; set; }

        [StringLength(255)]
        [Display(Name = "Description")]
        public string Description { get; set; }
    }
}