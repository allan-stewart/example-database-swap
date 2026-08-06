using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.Api.Repositories;

public interface IMatchesRepository
{
    Task<bool> DoesMatchExistAsync(Guid matchId);
    Task InsertMatchAsync(Match match);
    Task<Match?> LoadMatchByIdAsync(Guid matchId);
    Task<long> LoadWinCountForPlayerAsync(Guid playerId, DateTimeOffset? from, DateTimeOffset? to);
}
