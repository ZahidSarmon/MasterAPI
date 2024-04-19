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

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] PageCommand command)
    {
        var isCommand = await _service.PostAsync(command);

        return Ok(isCommand);
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var pages = await _service.GetPagesAsync();

        return Ok(pages);
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
        var isCommand = await _service.DeletePageInputValuesAsync(command);

        return Ok(isCommand);
    }


    [HttpGet("GetPageInputValues",Name = "GetPageInputValues")]
    public async Task<IActionResult> GetPageInputValues([FromQuery] Guid id)
    {
        var pageInputValue = await _service.GetPageInputValuesAsync(id);

        return Ok(pageInputValue);
    }
}
