namespace Master.Features.DTOs;

public record PageCommand
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string DatabaseName { get; init; }
    public required string Definition { get; init; }
    public bool IsActive { get; init; }
}
