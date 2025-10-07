using FCG.Game.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Game.API.Controllers;

[Authorize]
public class GamesController : ApiBaseController
{
    private readonly GameService _gameService;

    public GamesController(GameService gameService)
    {
        _gameService = gameService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameDto dto)
    {
        var gameId = await _gameService.CreateGameAsync(dto);
        return CreatedAtAction(nameof(GetGame), new { id = gameId }, new { gameId });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetGame(Guid id)
    {
        var game = await _gameService.GetGameByIdAsync(id);

        if (game == null)
            return NotFound();

        return Ok(game);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchGames(
        [FromQuery] string term,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var games = await _gameService.SearchGamesAsync(term, page, pageSize);

        return Ok(new
        {
            page,
            pageSize,
            total = games.Count,
            data = games
        });
    }

    [HttpGet("genre/{genre}")]
    public async Task<IActionResult> GetByGenre(string genre, [FromQuery] int limit = 20)
    {
        var games = await _gameService.GetGamesByGenreAsync(genre, limit);
        return Ok(games);
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations([FromQuery] int limit = 10)
    {
        var userId = GetUserId();
        var recommendations = await _gameService.GetRecommendationsAsync(userId, limit);

        return Ok(new
        {
            userId,
            recommendations,
            count = recommendations.Count
        });
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopular([FromQuery] int limit = 10)
    {
        var games = await _gameService.GetMostPopularGamesAsync(limit);
        return Ok(games);
    }
}