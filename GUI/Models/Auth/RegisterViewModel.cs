using System.ComponentModel.DataAnnotations;

namespace GUI.Models.Auth;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(200, ErrorMessage = "Họ tên không được vượt quá 200 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Range(1, short.MaxValue, ErrorMessage = "Role không hợp lệ.")]
    public short RoleId { get; set; } = 3;
}
