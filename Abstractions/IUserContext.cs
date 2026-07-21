namespace SystemContext.Abstractions
{
    public interface IUserContext
    {
        Guid? UserId { get; }
        string? Email { get; }
        string? UserName { get; }
        IReadOnlyCollection<string> Roles { get; }
        bool IsInRole(string role);
        bool IsAuthenticated { get; }
    }
}
