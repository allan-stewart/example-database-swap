using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using RockPaperScissors.Api.Domain;
using RockPaperScissors.Api.Repositories;

[ApiController]
[Route("/players")]
public class PlayerController(IMongoDatabase database,
    IMatchesRepository matchesRepository,
    IMatchEventRepository matchEventRepository,
    IFeatureFlagRepository featureFlagRepository) : ControllerBase
{
    private readonly IMongoCollection<Player> playerCollection = database.GetCollection<Player>("players");

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

        var wins = await matchesRepository.LoadWinCountForPlayerAsync(id, from, to);
        return Ok(new WinCountResponse(id, wins, from, to));
    }

    [HttpGet("{id:guid}/throws")]
    public async Task<IActionResult> GetThrowCounts(Guid id)
    {
        if (!await featureFlagRepository.IsEnabledAsync("throw-statistics"))
        {
            return NotFound();
        }

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
