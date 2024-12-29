using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Blazor.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;


public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly JWTService _jwtService;

    public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage, JWTService jwtService)
    {
        _sessionStorage = sessionStorage;
        _jwtService = jwtService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var storedToken = await _sessionStorage.GetAsync<string>("token");
            if (storedToken.Success && !string.IsNullOrEmpty(storedToken.Value))
            {
                var principal = _jwtService.ValidateToken(storedToken.Value);
                if (principal != null)
                {
                    return new AuthenticationState(principal);
                }
            }
        }
        catch
        {
            // Hvis der opstår fejl, returner uautoriseret bruger
        }

        return new AuthenticationState(new ClaimsPrincipal());
    }
}