using Master.Features;
using Master.Features.DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

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

    [HttpPost("PostPageExcelInputValues",Name = "PostPageExcelInputValues")]
    public async Task<IActionResult> PostPageExcelInputValues(PostPageExcelInputCommand command)
    {
        var isCommand = await _service.PostPageExcelInputValueAsync(command);

        return Ok(isCommand);
    }

    [HttpGet("ParseTest",Name = "ParseTest")]
    public async Task<IActionResult> ParseTest()
    {
        JsonTest();

        return Ok();
    }

    private void JsonTest()
    {
        string basePath = Directory.GetParent(@"./")!.FullName;
        var _path = Path.Combine(basePath, "Json\\parser.json");
        var jsonData = System.IO.File.ReadAllText(_path);

        List<IDictionary<string, IDictionary<string, string>>> dataList = new();
        JArray jsonArray = JArray.Parse(jsonData);
        foreach (JObject jsonObject in jsonArray)
        {
            BuildList(jsonObject, ref dataList);
        }

        var data = dataList.ToArray();
    }

    private void BuildList(JObject jsonObject, ref List<IDictionary<string, IDictionary<string, string>>> dataList, string parentKey = "Person")
    {
        var colValue = new Dictionary<string, string>();

        var tableList = new Dictionary<string, IDictionary<string, string>>()!;

        foreach (var property in jsonObject.Properties())
        {
            string parentId = Guid.NewGuid().ToString();

            string key = property.Name;

            JToken value = property.Value;

            if (value.Type == JTokenType.Object)
            {
                BuildList((JObject)value, ref dataList, key);
            }
            else if (value.Type == JTokenType.Array)
            {
                int idx = 0;
                foreach (var arrayItem in (JArray)value)
                {
                    BuildList((JObject)arrayItem, ref dataList, key);
                    idx++;
                }
            }
            else
            {
                colValue.Add(key, value.ToString());

                if (tableList.ContainsKey(parentKey))
                {
                    tableList[parentKey] = colValue;
                }
                else
                {
                    tableList.Add(parentKey, colValue);
                }
            }
        }

        /*foreach (var dataDict in dataList)
        {
            foreach (var table in tableList)
            {
                if (dataDict.ContainsKey(table.Key))
                {
                    var val = table;

                }
                foreach (var data in dataDict)
                {
                    if (table.Key.Equals(data.Key))
                    {

                    }
                }
            }
        }*/

        var index = dataList.FindIndex(item => item.ContainsKey(parentKey));
        if (index != -1)
        {
            dataList[index].Add(parentKey, tableList[parentKey]);
        }
        else
        {
            dataList.Add(tableList);
        }
    }

}
