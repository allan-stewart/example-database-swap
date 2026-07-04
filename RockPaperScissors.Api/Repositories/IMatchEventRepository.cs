using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.Api.Repositories;

public interface IMatchEventRepository
{
    Task AddAsync(List<MatchEvent> matchEvents);

    Task<List<MatchEvent>> GetByMatchAsync(Guid matchId);

    Task<Dictionary<string, long>> CountThrowsByPlayerAsync(Guid playerId);
}
