using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Npgsql;
using RockPaperScissors.Api.Domain;

[ApiController]
[Route("/matches")]
public class MatchesController(IMongoDatabase database, NpgsqlDataSource postgres) : ControllerBase
{
    private readonly IMongoCollection<Match> matchCollection = database.GetCollection<Match>("matches");
    private readonly IMongoCollection<Player> playerCollection = database.GetCollection<Player>("players");

    [HttpPost]
    public async Task<IActionResult> RecordMatch([FromBody] RecordMatchRequest request)
    {
        if (request.WinnerPlayerId == request.LoserPlayerId)
        {
            return BadRequest("A match requires two different players.");
        }

        if (request.BestOf < 1)
        {
            return BadRequest("BestOf must be at least 1.");
        }

        if (request.BestOf % 2 == 0)
        {
            return BadRequest("BestOf must be an odd number.");
        }

        if (request.LoserWins < 0)
        {
            return BadRequest("LoserWins cannot be negative.");
        }

        if (request.WinnerWins + request.LoserWins > request.BestOf)
        {
            return BadRequest("The combined wins cannot exceed BestOf.");
        }

        if (request.WinnerWins <= request.LoserWins)
        {
            return BadRequest("The winner must have more wins than the loser.");
        }

        if (request.WinnerWins != (request.BestOf + 1) / 2)
        {
            return BadRequest("WinnerWins must be exactly (BestOf + 1) / 2.");
        }

        var playerIds = new[] { request.WinnerPlayerId, request.LoserPlayerId };
        var knownPlayers = await playerCollection.CountDocumentsAsync(p => playerIds.Contains(p.Id));
        if (knownPlayers != 2)
        {
            return BadRequest("Both players must exist.");
        }

        var match = new Match
        {
            WinnerPlayerId = request.WinnerPlayerId,
            LoserPlayerId = request.LoserPlayerId,
            BestOf = request.BestOf,
            WinnerWins = request.WinnerWins,
            LoserWins = request.LoserWins
        };
        await matchCollection.InsertOneAsync(match);

        if (request.Throws is { Length: > 0 })
        {
            var matchEvents = request.Throws.SelectMany((gameThrows, index) => new[]
            {
                new MatchEvent
                {
                    MatchId = match.MatchId,
                    PlayerId = match.WinnerPlayerId,
                    ThrowIndex = index,
                    Thrown = gameThrows.WinnerPlayerThrow
                },
                new MatchEvent
                {
                    MatchId = match.MatchId,
                    PlayerId = match.LoserPlayerId,
                    ThrowIndex = index,
                    Thrown = gameThrows.LoserPlayerThrow
                }
            });

            await using var connection = await postgres.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            foreach (var matchEvent in matchEvents)
            {
                await using var command = new NpgsqlCommand(
                    "INSERT INTO match_event_log (match_id, player_id, throw_index, thrown) VALUES (@matchId, @playerId, @throwIndex, @thrown)",
                    connection, transaction);
                command.Parameters.AddWithValue("matchId", matchEvent.MatchId);
                command.Parameters.AddWithValue("playerId", matchEvent.PlayerId);
                command.Parameters.AddWithValue("throwIndex", matchEvent.ThrowIndex);
                command.Parameters.AddWithValue("thrown", matchEvent.Thrown);
                await command.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
        }

        return Created($"/matches/{match.MatchId}", match);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMatch(Guid id)
    {
        var match = await matchCollection.Find(m => m.MatchId == id).FirstOrDefaultAsync();
        return match is null ? NotFound() : Ok(match);
    }

    [HttpGet("{id:guid}/throws")]
    public async Task<IActionResult> GetMatchThrows(Guid id)
    {
        var matchExists = await matchCollection.Find(m => m.MatchId == id).AnyAsync();
        if (!matchExists)
        {
            return NotFound();
        }

        var matchEvents = new List<MatchEvent>();
        await using var connection = await postgres.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT match_event_id, match_id, player_id, throw_index, thrown FROM match_event_log WHERE match_id = @matchId ORDER BY throw_index, match_event_id",
            connection);
        command.Parameters.AddWithValue("matchId", id);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            matchEvents.Add(new MatchEvent
            {
                MatchEventId = reader.GetInt64(0),
                MatchId = reader.GetGuid(1),
                PlayerId = reader.GetGuid(2),
                ThrowIndex = reader.GetInt32(3),
                Thrown = reader.GetString(4)
            });
        }

        return Ok(matchEvents);
    }
}

public record MatchThrows(string WinnerPlayerThrow, string LoserPlayerThrow);

public record RecordMatchRequest(Guid WinnerPlayerId, Guid LoserPlayerId, int WinnerWins, int LoserWins, int BestOf = 1, MatchThrows[]? Throws = null);
