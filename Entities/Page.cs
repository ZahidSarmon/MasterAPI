using Master.Domain;

namespace Master.Entities;

public class Page : Audit<Guid>
{
    public required string DatabaseName { get; set; }
    public required string Name { get; set; }
    public required string Definition { get; set; }
    public bool IsActive { get; set; }
}
