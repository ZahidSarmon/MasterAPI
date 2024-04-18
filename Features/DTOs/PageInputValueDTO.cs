namespace Master.Features.DTOs;

public class PageInputValueDTO
{
    public Guid PageId { get; set; }
    public Guid PageInputId { get; set; }
    public string? Value { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? PlaceHolder { get; set; }
}
