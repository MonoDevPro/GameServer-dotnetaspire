namespace GameServer.CharacterService.Application.Models;

// Request models
public record CreateCharacterRequest(string Name, string Class);
public record UpdateCharacterRequest(string? Name, string? Class);

// Response models
public record CharacterResponse(
    Guid Id,
    string Name,
    string Class,
    int Level,
    DateTime CreatedAt,
    DateTime LastUpdatedAt,
    bool IsActive);

public record CharacterListResponse(
    IEnumerable<CharacterResponse> Characters,
    int TotalCount);

// Error response
public record ErrorResponse(string Message, string? Details = null);