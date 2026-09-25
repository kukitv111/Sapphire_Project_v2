namespace Sapphire.Auth.Infrastructure.Persistence;

public sealed class BootstrapRecord
{
    public int Id { get; set; } = 1;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
