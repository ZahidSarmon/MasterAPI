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
        var isCommand = await _service.Post(command);

        return Ok(isCommand);
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var pages = await _service.GetPages();

        return Ok(pages);
    }

    [HttpGet("GetPageInputsByPage",Name = "GetPageInputsByPage")]
    public async Task<IActionResult> GetPageInputByPage([FromQuery] Guid id)
    {
        var pageInputs = await _service.GetPageInputs(id);

        return Ok(pageInputs);
    }

}
