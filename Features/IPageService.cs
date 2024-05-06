using Master.Domain;
using Master.Features.DTOs;

namespace Master.Features;

public interface IPageService
{
    Task<PageInputValueModel> GetPageInputValuesAsync(Guid pageId);

    Task<IEnumerable<PageInputModel>> GetPageInputValueAsync(Guid pageId,Guid pageTableId);

    Task<bool> PostPageInputValuesAsync(PostPageInputCommand command);

    Task<bool> PutPageInputValuesAsync(PutPageInputCommand command);

    Task<bool> DeletePageInputValueAsync(DeletePageCommand command);

    Task<bool> PostPageInputsAsync(PageCommand command);

    Task<IEnumerable<PageInputModel>> GetPageInputsAsync(Guid pageId);

    Task<IEnumerable<PageLookupModel>> GetLookupPagesAsync();

    Task<IEnumerable<Lookup<string>>> GetTableColumnsAsync(string schema, string table);

    Task<IEnumerable<Lookup<string>>> GetTableNamesAsync(string schema);

    Task<IEnumerable<Lookup<string>>> GetTableSchemasAsync();

    Task<Property<Entities.Page>> GetPagesAsync(PageDR pagination);

    Task<bool> DeletePageAsync(Guid id);
}
