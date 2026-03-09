using System.ComponentModel.DataAnnotations;

namespace MEC.Portal.Models // Namespace portal olarak güncellendi
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı adı veya email zorunludur.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}