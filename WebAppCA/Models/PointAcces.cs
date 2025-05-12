using System;
using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    public class PointAcces
    {
        [Key]
        public int Id { get; set; }

        public uint DoorID { get; set; }

        [Required]
        [StringLength(48)]
        public string Nom { get; set; }

        [Required]
        public uint DeviceID { get; set; }

        [Required]
        public byte RelayPort { get; set; }

        public string Description { get; set; }

        public bool EstVerrouille { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}