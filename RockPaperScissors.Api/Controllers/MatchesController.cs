using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using RockPaperScissors.Api.Domain;
using RockPaperScissors.Api.Repositories;

[ApiController]
[Route("/matches")]
public class MatchesController(
    IMongoDatabase database,
    IMatchesRepository matchesRepository,
    IMatchEventRepository matchEventRepository) : ControllerBase
{
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
        await matchesRepository.InsertMatchAsync(match);

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
            }).ToList();

            await matchEventRepository.AddAsync(matchEvents);
        }

        return Created($"/matches/{match.MatchId}", match);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMatch(Guid id)
    {
        var match = await matchesRepository.LoadMatchByIdAsync(id);
        return match is null ? NotFound() : Ok(match);
    }

    [HttpGet("{id:guid}/throws")]
    public async Task<IActionResult> GetMatchThrows(Guid id)
    {
        var matchExists = await matchesRepository.DoesMatchExistAsync(id);
        if (!matchExists)
        {
            return NotFound();
        }

        var matchEvents = await matchEventRepository.GetByMatchAsync(id);
        return Ok(matchEvents);
    }
}

public record MatchThrows(string WinnerPlayerThrow, string LoserPlayerThrow);

public record RecordMatchRequest(Guid WinnerPlayerId, Guid LoserPlayerId, int WinnerWins, int LoserWins, int BestOf = 1, MatchThrows[]? Throws = null);
