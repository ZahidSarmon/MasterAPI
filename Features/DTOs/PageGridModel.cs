namespace Master.Features.DTOs;

public class PageGridModel
{
    public Guid PageId { get; set; }
    public Guid PageInputId { get; set; }
    public string PageName { get; set; }
    public string PageInputName { get; set; }
    public string PageInputValue { get; set; }
}
