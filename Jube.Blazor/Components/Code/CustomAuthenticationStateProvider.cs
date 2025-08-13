using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Jube.Blazor.Components.Code;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    public CustomAuthenticationStateProvider()
    {
        CurrentUser = GetAnonymous();
    }

    private ClaimsPrincipal CurrentUser { get; set; }

    private ClaimsPrincipal GetUser(string userName, string id, string role)
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.Sid, id),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, role)
        ], "Authentication type");
        return new ClaimsPrincipal(identity);
    }

    private ClaimsPrincipal GetAnonymous()
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.Sid, "0"),
            new Claim(ClaimTypes.Name, "Anonymous"),
            new Claim(ClaimTypes.Role, "Anonymous")
        ], null);
        return new ClaimsPrincipal(identity);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var task = Task.FromResult(new AuthenticationState(CurrentUser));
        return task;
    }

    public Task<AuthenticationState> ChangeUser(string username, string id, string role)
    {
        CurrentUser = GetUser(username, id, role);
        var task = GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(task);
        return task;
    }

    public Task<AuthenticationState> Logout()
    {
        CurrentUser = GetAnonymous();
        var task = GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(task);
        return task;
    }
}