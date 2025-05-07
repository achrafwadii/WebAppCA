using System;
using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    public class PointAcces
    {
        [Key]
        public int Id { get; set; }

        // Correspondance avec l'ID de porte dans l'API Door
        public uint DoorID { get; set; }

        [Required]
        [StringLength(48)]
        public string Nom { get; set; }

        // Informations du dispositif associé
        public uint DeviceID { get; set; }

        // Informations du relais
        public byte RelayPort { get; set; }

        // Informations supplémentaires
        public string Description { get; set; }

        // Date de création et de mise à jour
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // État actuel (pour le suivi)
        public bool EstVerrouille { get; set; } = true;
    }
}