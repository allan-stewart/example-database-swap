using MongoDB.Driver;
using RockPaperScissors.Api.Domain;

namespace RockPaperScissors.Api.Repositories;

public class MongoMatchesRepository(IMongoDatabase database) : IMatchesRepository
{
    private readonly IMongoCollection<Match> matchCollection = database.GetCollection<Match>("matches");

    public async Task InsertMatchAsync(Match match)
    {
        await matchCollection.InsertOneAsync(match);
    }

    public async Task<Match?> LoadMatchByIdAsync(Guid matchId)
    {
        return await matchCollection.Find(m => m.MatchId == matchId).FirstOrDefaultAsync();
    }

    public async Task<bool> DoesMatchExistAsync(Guid matchId)
    {
        return await matchCollection.Find(m => m.MatchId == matchId).AnyAsync();
    }

    public async Task<long> LoadWinCountForPlayerAsync(Guid playerId, DateTimeOffset? from, DateTimeOffset? to)
    {
        var filter = Builders<Match>.Filter.Eq(m => m.WinnerPlayerId, playerId);
        if (from is not null)
        {
            filter &= Builders<Match>.Filter.Gte(m => m.RecordedAt, from.Value);
        }
        if (to is not null)
        {
            filter &= Builders<Match>.Filter.Lt(m => m.RecordedAt, to.Value);
        }

        return await matchCollection.CountDocumentsAsync(filter);
    }
}
