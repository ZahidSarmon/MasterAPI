using Master.Domain;

namespace Master.Entities;

public class Page : Audit<Guid>
{
    public string DbName { get; set; }
    public string Name { get; set; }
    public string? Definition { get; set; }
    public bool IsActive { get; set; }
}
