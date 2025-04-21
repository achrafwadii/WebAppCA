namespace WebAppCA.Models
{
    // Models/User.cs
    public class User
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; } // Toujours stocker des mots de passe hashés
    }
}
