namespace Hefi.Mobile.Models;

//represents a pair of authentication tokens used by the mobile app for secure API access.
public sealed class Token
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
}
