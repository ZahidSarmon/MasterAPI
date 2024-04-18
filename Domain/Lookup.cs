namespace Master.Domain;

public class Lookup<T>
{
    public required T Id { get; init; }
    public required string Name { get; init; }
}
