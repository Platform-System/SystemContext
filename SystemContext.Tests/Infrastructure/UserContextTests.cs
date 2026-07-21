using Microsoft.AspNetCore.Http;
using SystemContext.Infrastructure;
using System.Security.Claims;
using Xunit;

namespace SystemContext.Tests.Infrastructure;

public sealed class UserContextTests
{
    [Fact]
    public void Properties_WhenClaimsExist_ReturnExpectedValues()
    {
        var userId = Guid.CreateVersion7();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim("email", "user@example.com"),
                new Claim("preferred_username", "hung"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("role", "admin"),
                new Claim("roles", "Seller")
            ], "Bearer"))
        };

        var userContext = new UserContext(new HttpContextAccessor { HttpContext = context });

        Assert.Equal(userId, userContext.UserId);
        Assert.Equal(userId.ToString(), userContext.CurrentUserId);
        Assert.Equal("user@example.com", userContext.Email);
        Assert.Equal("hung", userContext.UserName);
        Assert.Equal(2, userContext.Roles.Count);
        Assert.Contains("Admin", userContext.Roles);
        Assert.Contains("Seller", userContext.Roles);
        Assert.True(userContext.IsInRole("admin"));
        Assert.True(userContext.IsAuthenticated);
    }

    [Fact]
    public void Roles_WhenOnlyKeycloakJsonClaimsExist_AreReadSuccessfully()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("realm_access", """{"roles":["admin","seller"]}"""),
                new Claim("resource_access", """{"gateway":{"roles":["catalog.write","admin"]}}""")
            ], "Bearer"))
        };

        var userContext = new UserContext(new HttpContextAccessor { HttpContext = context });

        Assert.Equal(3, userContext.Roles.Count);
        Assert.Contains("admin", userContext.Roles);
        Assert.Contains("seller", userContext.Roles);
        Assert.Contains("catalog.write", userContext.Roles);
        Assert.True(userContext.IsInRole("ADMIN"));
    }

    [Fact]
    public void Properties_WhenSubjectIsInvalid_FallsBackSafely()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "not-a-guid")
            ]))
        };

        var userContext = new UserContext(new HttpContextAccessor { HttpContext = context });

        Assert.Null(userContext.UserId);
        Assert.Null(userContext.CurrentUserId);
        Assert.False(userContext.IsAuthenticated);
        Assert.Empty(userContext.Roles);
    }
}
