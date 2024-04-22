using Master.Domain;

namespace Master.Features.DTOs;

public class PageInputModel
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? DatabaseName { get; set; }
    public string? FieldType { get; set; }
    public string? DataType { get; set; }
    public string? Size { get; set; }
    public int DecimalPlace { get; set; }
    public string? PlaceHolder { get; set; }
    public ComboInputModel? ComboInput { get; set; } = new();
    public Selective<string>? RadioInputs { get; set; } = new();
   public Selective<string>? CheckBoxInputs { get; set; } = new();
    public string? DefaultDate { get; set; }
    public bool IsRequired { get; set; }
    public int Ordinal { get; set; }
}

public class ComboInputModel
{
    public bool IsDataBaseSource { get; set; }
    public List<Lookup<string>> Data { get; set; } = new();
    public ComboInputTableRefModel TableRef { get; set; } = new();
}


public class ComboInputTableRefModel
{
    public string? TableSchema { get; set; }
    public string? TableName { get; set; }
    public string? IdColumn { get; set; }
    public string? NameColumn { get; set; }
    public string? WhereClause { get; set; }
}

