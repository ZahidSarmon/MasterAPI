namespace Master.Features.DTOs;

public record PutPageInputCommand(string TableName, IList<string> Columns, IDictionary<string, string> ColumnWithValues, string ModifiedBy);