using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using RockPaperScissors.Api.Domain;
using RockPaperScissors.Api.Repositories;

[ApiController]
[Route("/players")]
public class PlayerController(IMongoDatabase database, IMatchEventRepository matchEventRepository) : ControllerBase
{
    private readonly IMongoCollection<Player> playerCollection = database.GetCollection<Player>("players");
    private readonly IMongoCollection<Match> matchCollection = database.GetCollection<Match>("matches");

    [HttpPost]
    public async Task<IActionResult> CreatePlayer([FromBody] CreatePlayerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required.");
        }

        var player = new Player
        {
            Name = request.Name.Trim()
        };
        await playerCollection.InsertOneAsync(player);

        return Created($"/players/{player.Id}", player);
    }

    [HttpGet]
    public async Task<IActionResult> ListPlayers()
    {
        var players = await playerCollection.Find(FilterDefinition<Player>.Empty).ToListAsync();
        return Ok(players);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPlayer(Guid id)
    {
        var player = await playerCollection.Find(p => p.Id == id).FirstOrDefaultAsync();
        return player is null ? NotFound() : Ok(player);
    }

    [HttpGet("{id:guid}/wins")]
    public async Task<IActionResult> GetWinCount(Guid id, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to)
    {
        var playerExists = await playerCollection.Find(p => p.Id == id).AnyAsync();
        if (!playerExists)
        {
            return NotFound();
        }

        var filter = Builders<Match>.Filter.Eq(m => m.WinnerPlayerId, id);
        if (from is not null)
        {
            filter &= Builders<Match>.Filter.Gte(m => m.RecordedAt, from.Value);
        }
        if (to is not null)
        {
            filter &= Builders<Match>.Filter.Lt(m => m.RecordedAt, to.Value);
        }

        var wins = await matchCollection.CountDocumentsAsync(filter);
        return Ok(new WinCountResponse(id, wins, from, to));
    }

    [HttpGet("{id:guid}/throws")]
    public async Task<IActionResult> GetThrowCounts(Guid id)
    {
        var playerExists = await playerCollection.Find(p => p.Id == id).AnyAsync();
        if (!playerExists)
        {
            return NotFound();
        }

        var throwCounts = await matchEventRepository.CountThrowsByPlayerAsync(id);
        return Ok(throwCounts);
    }
}

public record CreatePlayerRequest(string Name);

public record WinCountResponse(Guid PlayerId, long Wins, DateTimeOffset? From, DateTimeOffset? To);
