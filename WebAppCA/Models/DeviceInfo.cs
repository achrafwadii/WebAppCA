using System;
using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    public class DeviceInfo
    {
        [Key]
        public int DeviceId { get; set; }

        // Make these nullable where appropriate
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [StringLength(50)]
        public string IPAddress { get; set; } = string.Empty; // Default value

        [Required]
        [Range(1, 65535)]
        public int Port { get; set; }

        public bool UseSSL { get; set; }
        public string? Description { get; set; }
        public DateTime? LastConnectionTime { get; set; } // Nullable DateTime
        public bool? IsConnected { get; set; } // Nullable bool
        public string? Status { get; set; }
    }
}