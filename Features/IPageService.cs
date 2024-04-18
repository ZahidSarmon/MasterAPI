using Master.Domain;
using Master.Features.DTOs;

namespace Master.Features;

public interface IPageService
{
    Task<bool> Post(PageCommand command);

    Task<IEnumerable<PageInputDTO>> GetPageInputs(Guid pageId);

    Task<IEnumerable<Lookup<Guid>>> GetPages();
}
