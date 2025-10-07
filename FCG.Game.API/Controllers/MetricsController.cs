using FCG.Game.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Game.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class MetricsController : ApiBaseController
{
    private readonly MetricsService _metricsService;

    public MetricsController(MetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet("top-games")]
    public async Task<IActionResult> GetTopGames([FromQuery] int limit = 10)
    {
        var metrics = await _metricsService.GetTopGamesAsync(limit);
        return Ok(metrics);
    }

    [HttpGet("genres")]
    public async Task<IActionResult> GetGenreStatistics()
    {
        var metrics = await _metricsService.GetGenreStatisticsAsync();
        return Ok(metrics);
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesMetrics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = endDate ?? DateTime.UtcNow;

        var metrics = await _metricsService.GetSalesMetricsAsync(start, end);
        return Ok(metrics);
    }

    [HttpGet("user-behavior")]
    public async Task<IActionResult> GetUserBehavior()
    {
        var metrics = await _metricsService.GetUserBehaviorMetricsAsync();
        return Ok(metrics);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var topGames = await _metricsService.GetTopGamesAsync(5);
        var genres = await _metricsService.GetGenreStatisticsAsync();
        var sales = await _metricsService.GetSalesMetricsAsync(
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow
        );
        var userBehavior = await _metricsService.GetUserBehaviorMetricsAsync();

        return Ok(new
        {
            topGames,
            genres,
            sales,
            userBehavior,
            generatedAt = DateTime.UtcNow
        });
    }
}