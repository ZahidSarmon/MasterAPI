using Master.Features;
using Master.Features.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Master.Controllers;

public class PageController : BaseController
{
    private readonly IPageService _service;
    public PageController(IPageService service)
    {
        _service = service;
    }

    [HttpPost("GetPages",Name ="GetPages")]
    public async Task<IActionResult> GetPages([FromBody] PageDR pageDR)
    {
        var pages = await _service.GetPagesAsync(pageDR);

        return Ok(pages);
    }

    [HttpGet("GetLookupPages", Name = "GetLookupPages")]
    public async Task<IActionResult> GetLookupPages()
    {
        var pages = await _service.GetLookupPagesAsync();

        return Ok(pages);
    }

    [HttpDelete("DeletePage",Name ="DeletePage")]
    public async Task<IActionResult> DeletePage([FromQuery] Guid id)
    {
        var isCommand = await _service.DeletePageAsync(id);

        return Ok(isCommand);
    }

    [HttpPost("PostPageInputs",Name = "PostPageInputs")]
    public async Task<IActionResult> PostPageInputs([FromBody] PageCommand command)
    {
        var isCommand = await _service.PostPageInputsAsync(command);

        return Ok(isCommand);
    }

    [HttpGet("GetPageInputs", Name = "GetPageInputs")]
    public async Task<IActionResult> GetPageInputs([FromQuery] Guid id)
    {
        var pageInputs = await _service.GetPageInputsAsync(id);

        return Ok(pageInputs);
    }

    [HttpPost("PostPageInputValues", Name = "PostPageInputValues")]
    public async Task<IActionResult> PostPageInputValues([FromBody] PostPageInputCommand command)
    {
        var isCommand = await _service.PostPageInputValuesAsync(command);

        return Ok(isCommand);
    }

    [HttpPut("PutPageInputValues", Name = "PutPageInputValues")]
    public async Task<IActionResult> PutPageInputValues([FromBody] PutPageInputCommand command)
    {
        var isCommand = await _service.PutPageInputValuesAsync(command);

        return Ok(isCommand);
    }

    [HttpDelete("DeletePageInputValues",Name = "DeletePageInputValues")]
    public async Task<IActionResult> DeletePageInputValues([FromQuery] DeletePageCommand command)
    {
        var isCommand = await _service.DeletePageInputValueAsync(command);

        return Ok(isCommand);
    }

    [HttpGet("GetPageInputValues",Name = "GetPageInputValues")]
    public async Task<IActionResult> GetPageInputValues([FromQuery] Guid id)
    {
        var pageInputValue = await _service.GetPageInputValuesAsync(id);

        return Ok(pageInputValue);
    }

    [HttpGet("GetPageInputValue", Name = "GetPageInputValue")]
    public async Task<IActionResult> GetPageInputValue([FromQuery] Guid pageId,Guid pageTableId)
    {
        var pageInputValue = await _service.GetPageInputValueAsync(pageId,pageTableId);

        return Ok(pageInputValue);
    }

    [HttpGet("GetTableColumns", Name = "GetTableColumns")]
    public async Task<IActionResult> GetTableNames([FromQuery] string Schema, [FromQuery] string Table)
    {
        var tableColumns = await _service.GetTableColumnsAsync(Schema,Table);

        return Ok(tableColumns);
    }

    [HttpGet("GetTableNames", Name = "GetTableNames")]
    public async Task<IActionResult> GetTableNames([FromQuery] string Schema)
    {
        var tableNames = await _service.GetTableNamesAsync(Schema);

        return Ok(tableNames);
    }

    [HttpGet("GetTableSchemas", Name = "GetTableSchemas")]
    public async Task<IActionResult> GetTableSchemas()
    {
        var tableSchemas = await _service.GetTableSchemasAsync();

        return Ok(tableSchemas);
    }

}
