namespace Hefi.Api.Dtos;

// request payload for user registration
public record UserCreate(
    string Name,
    string Email,
    string Password
);

// payload for refreshing an access token using a refresh token
public record UserLoginRequest(
    string Email,
    string Password
);

public record UserRefreshRequest(
    string RefreshToken
);
