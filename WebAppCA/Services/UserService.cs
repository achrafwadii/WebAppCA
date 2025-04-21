// Services/UserService.cs
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using WebAppCA.Models;

namespace WebAppCA.Services
{
    public class UserService
    {
        private const string UsersFile = "users.json";
        private List<User> _users;

        public UserService()
        {
            LoadUsers();
        }

        public void Register(User newUser, string password)
        {
            if (_users.Any(u => u.Username == newUser.Username))
            {
                throw new Exception("Ce nom d'utilisateur est déjà pris");
            }

            newUser.PasswordHash = HashPassword(password);
            _users.Add(newUser);
            SaveUsers();
        }

        public User Login(string username, string password)
        {
            var user = _users.FirstOrDefault(u => u.Username == username);
            if (user == null || user.PasswordHash != HashPassword(password))
            {
                throw new Exception("Identifiants incorrects");
            }
            return user;
        }

        private void LoadUsers()
        {
            if (File.Exists(UsersFile))
            {
                var json = File.ReadAllText(UsersFile);
                _users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
            }
            else
            {
                _users = new List<User>();
            }
        }

        private void SaveUsers()
        {
            var json = JsonSerializer.Serialize(_users);
            File.WriteAllText(UsersFile, json);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        }
    }
}