namespace Master.Features.DTOs;

public record PostPageInputCommand(string TableName,IList<string> Columns,IDictionary<string,string> ColumnWithValues,string CreatedBy);
