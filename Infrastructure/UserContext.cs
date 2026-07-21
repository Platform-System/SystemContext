using Microsoft.AspNetCore.Http;
using BuildingBlocks.Abstractions;
using SystemContext.Abstractions;
using System.Security.Claims;
using System.Linq;
using System.Text.Json;

namespace SystemContext.Infrastructure
{
    public class UserContext : IUserContext, ICurrentUserProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public UserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
        public Guid? UserId
        {
            get
            {
                var rawUserId = User?.FindFirst("sub")?.Value
                          ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return Guid.TryParse(rawUserId, out var id) ? id : null;
            }
        }
        public string? Email =>
            User?.FindFirst("email")?.Value
            ?? User?.FindFirst(ClaimTypes.Email)?.Value;
        public string? UserName =>
            User?.FindFirst("preferred_username")?.Value
            ?? User?.Identity?.Name;
        public string? CurrentUserId => UserId?.ToString();
        public IReadOnlyCollection<string> Roles =>
            User?.FindAll(ClaimTypes.Role).Select(x => x.Value)
                .Concat(User?.FindAll("role").Select(x => x.Value) ?? [])
                .Concat(User?.FindAll("roles").Select(x => x.Value) ?? [])
                .Concat(ReadRealmRoles(User) ?? [])
                .Concat(ReadClientRoles(User) ?? [])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            ?? [];
        public bool IsInRole(string role) => Roles.Any(x => string.Equals(x, role, StringComparison.OrdinalIgnoreCase));
        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        private static IEnumerable<string> ReadRealmRoles(ClaimsPrincipal? user)
        {
            var realmAccess = user?.FindFirst("realm_access")?.Value;
            if (string.IsNullOrWhiteSpace(realmAccess))
                return [];

            try
            {
                using var document = JsonDocument.Parse(realmAccess);
                if (!document.RootElement.TryGetProperty("roles", out var rolesElement) ||
                    rolesElement.ValueKind != JsonValueKind.Array)
                {
                    return [];
                }

                return ReadRoles(rolesElement).ToArray();
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static IEnumerable<string> ReadClientRoles(ClaimsPrincipal? user)
        {
            var resourceAccess = user?.FindFirst("resource_access")?.Value;
            if (string.IsNullOrWhiteSpace(resourceAccess))
                return [];

            try
            {
                using var document = JsonDocument.Parse(resourceAccess);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return [];

                return document.RootElement
                    .EnumerateObject()
                    .SelectMany(property =>
                        property.Value.TryGetProperty("roles", out var rolesElement) &&
                        rolesElement.ValueKind == JsonValueKind.Array
                            ? ReadRoles(rolesElement)
                            : [])
                    .ToArray();
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static IEnumerable<string> ReadRoles(JsonElement rolesElement)
        {
            foreach (var roleElement in rolesElement.EnumerateArray())
            {
                var role = roleElement.GetString();
                if (!string.IsNullOrWhiteSpace(role))
                    yield return role;
            }
        }
    }
}
