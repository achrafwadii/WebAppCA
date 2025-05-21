// Models/DeleteAccountViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Confirmation de suppression")]
        [MustBeTrue(ErrorMessage = "Vous devez accepter la suppression")]
        public bool Confirmation { get; set; }
    }

    public class MustBeTrueAttribute : ValidationAttribute
    {
        public override bool IsValid(object value) => value is bool b && b;
    }
}