namespace Master.Features.DTOs;

public record PostPageExcelInputCommand(
    Guid? Id,
    string TableName,
    IList<string> Columns,
    IList<IList<string>> Values,
    string User);
