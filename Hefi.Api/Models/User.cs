namespace Hefi.Api.Models;

// Represents a registered user in the app
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password_Hash { get; set; } = string.Empty;
    public DateTime Created_At { get; set; }
}
