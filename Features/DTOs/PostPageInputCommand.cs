using Master.Domain;

namespace Master.Features.DTOs;

public record PostPageInputCommand(
    Guid? Id,
    string TableName,
    IList<string> Columns,
    IDictionary<string,string> ColumnWithValues,
    IList<ComboInput> ComboInputs,
    string User);

public record ComboInput
{
    public List<Lookup<string>> Data { get; set; } = new();
    public string? TableSchema { get; set; }
    public string? TableName { get; set; }
}
