using Master.Domain;

namespace Master.Features.DTOs;

public class PageInput
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? DatabaseName { get; set; }
    public string? FieldType { get; set; }
    public string? DataType { get; set; }
    public string? Size { get; set; }
    public int DecimalPlace { get; set; }
    public string? PlaceHolder { get; set; }
    public ComboInput? ComboInput { get; set; } = new();
    public Selective<string>? RadioInputs { get; set; } = new();
   public Selective<string>? CheckBoxInputs { get; set; } = new();
    public string? DefaultDate { get; set; }
    public bool IsRequired { get; set; }
    public int Ordinal { get; set; }
}

public class ComboInput
{
    public bool IsDbSource { get; set; }
    public List<Lookup<string>> FixedValues { get; set; } = new();
    public ComboInputTableRef TableRef { get; set; } = new();
}


public class ComboInputTableRef
{
    public string? TableName { get; set; }
    public string? IdColumn { get; set; }
    public string? NameColumn { get; set; }
    public string? WhereClause { get; set; }
}

