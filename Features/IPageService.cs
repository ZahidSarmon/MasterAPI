using Master.Domain;
using Master.Features.DTOs;

namespace Master.Features;

public interface IPageService
{
    Task<PageInputValueModel> GetPageInputValuesAsync(Guid pageId);

    Task<bool> PostPageInputValuesAsync(PostPageInputCommand command);

    Task<bool> PutPageInputValuesAsync(PutPageInputCommand command);

    Task<bool> DeletePageInputValuesAsync(DeletePageCommand command);

    Task<bool> PostPageInputsAsync(PageCommand command);

    Task<IEnumerable<PageInputModel>> GetPageInputsAsync(Guid pageId);

    Task<IEnumerable<PageLookupModel>> GetPagesAsync();

    Task<IEnumerable<Lookup<string>>> GetTableColumnsAsync(string schema, string table);

    Task<IEnumerable<Lookup<string>>> GetTableNamesAsync(string schema);

    Task<IEnumerable<Lookup<string>>> GetTableSchemasAsync();
}
