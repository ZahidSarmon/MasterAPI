using Master.Domain;

namespace Master.Features.Enums;

public class FieldType : Enumeration
{
    public static readonly FieldType Text = new(1, "text");
    public static readonly FieldType Number = new(2, "number");
    public FieldType(int id, string name) : base(id, name)
    {
    }
}
