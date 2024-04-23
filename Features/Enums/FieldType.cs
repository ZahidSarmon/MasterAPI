using Master.Domain;

namespace Master.Features.Enums;

public class FieldType : Enumeration
{
    public static readonly FieldType Text = new(Random.Shared.Next(), "text");
    public static readonly FieldType DropDown = new(Random.Shared.Next(), "dropdown");
    public static readonly FieldType Date = new(Random.Shared.Next(), "Date");
    public static readonly FieldType  Number = new(Random.Shared.Next(), "number");
    public static readonly FieldType CheckBox = new(Random.Shared.Next(), "checkbox");
    public static readonly FieldType Radio = new(Random.Shared.Next(), "radio");
    public static readonly FieldType AutoComplete = new(Random.Shared.Next(), "autocomplete");
    public static readonly FieldType MultiSelect = new(Random.Shared.Next(), "multiselect");
    public FieldType(int id, string name) : base(id, name)
    {
    }
}
