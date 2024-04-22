using Master.Domain;

namespace Master.Features.DTOs;

public record PageInputModelPayload(
    string Id,
    string Title,
    string DatabaseName,
    string FieldType,
    string? PlaceHolder,
    IEnumerable<Lookup<string>> ComboData);
