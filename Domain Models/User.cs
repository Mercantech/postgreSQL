namespace Domain_Models
{
    public class User : Common
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int RoleId { get; set; } = 1;
        public Role Role { get; set; } = Role.User;

        public string GetRoleName()
        {
            return this.RoleId switch
            {
                1 => "User",
                2 => "Admin",
                3 => "Dev",
                _ => "Ukendt"
            };
        }
    }

    public enum Role
    {
        User,
        Admin,
        Dev
    }
}