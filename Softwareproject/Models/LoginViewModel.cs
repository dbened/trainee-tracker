namespace Softwareproject.Models;
public class LoginViewModel
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? ErrorMessage { get; set; }
}