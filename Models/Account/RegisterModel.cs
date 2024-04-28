using System.ComponentModel.DataAnnotations;

namespace MagazinOnline.Models.Account
{
    public class RegisterModel
    {
        [Required]
        [MinLength(4, ErrorMessage = "Username must be at least 4 characters")]
        public string Username { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }
    }
}
