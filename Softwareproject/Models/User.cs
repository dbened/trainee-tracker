namespace Softwareproject.Models
{
    public abstract class User
    {
        public int Id { get; set; }

        public string EmailAddress { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool ForgotPassword { get; set; } = false;
    }
}