using lmsPortalBe.Data;
using Microsoft.EntityFrameworkCore;

namespace lmsPortalBe.Services;

public class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
  private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

  private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
  private readonly ILogger<RefreshTokenCleanupService> _logger = logger;

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using var timer = new PeriodicTimer(Interval);

    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
      try
      {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ILmsPortalContext>();

        var deleted = await context.RefreshTokens
            .Where(t => t.Expires < DateTime.UtcNow)
            .ExecuteDeleteAsync(stoppingToken);

        if (deleted > 0)
        {
          _logger.LogInformation("Deleted {Count} expired refresh token(s).", deleted);
        }
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        break;
      }
    }
  }
}
