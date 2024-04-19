using Master.Features.DTOs;

namespace Master.Features;

public interface IPageService
{
    Task<PageInputValueDTO> GetPageInputValuesAsync(Guid pageId);

    Task<bool> PostPageInputValuesAsync(PostPageInputCommand command);

    Task<bool> PutPageInputValuesAsync(PutPageInputCommand command);

    Task<bool> DeletePageInputValuesAsync(DeletePageCommand command);

    Task<bool> PostAsync(PageCommand command);

    Task<IEnumerable<PageInputDTO>> GetPageInputsAsync(Guid pageId);

    Task<IEnumerable<PageLookupDTO>> GetPagesAsync();
}
