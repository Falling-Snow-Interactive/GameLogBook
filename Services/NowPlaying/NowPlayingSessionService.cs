using Microsoft.EntityFrameworkCore;
using VGL.Data;
using VGL.Models;
using VGL.Services.UserProfiles;

namespace VGL.Services.NowPlaying;

public sealed class NowPlayingSessionService(GameLogBookDbContext dbContext, UserProfileSession userSession)
{
    public event Action? ActiveSessionChanged;

    public async Task<GameLog?> GetLatestActiveSessionAsync()
    {
        if (userSession.CurrentUserID is null)
        {
            return null;
        }

        List<GameLog> activeLogs = await IncludeSessionDetails(dbContext.GameLogs.AsNoTracking())
                                         .Where(log => log.UserProfileID == userSession.CurrentUserID.Value
                                                       && log.EndedAt == null)
                                         .ToListAsync();

        return activeLogs
               .OrderByDescending(log => log.StartedAt)
               .ThenByDescending(log => log.ID)
               .FirstOrDefault();
    }

    public async Task<GameLog?> GetSessionAsync(int? logId)
    {
        if (userSession.CurrentUserID is null)
        {
            return null;
        }

        IQueryable<GameLog> query = IncludeSessionDetails(dbContext.GameLogs.AsNoTracking())
            .Where(log => log.UserProfileID == userSession.CurrentUserID.Value);

        if (logId is not null)
        {
            return await query.FirstOrDefaultAsync(log => log.ID == logId.Value);
        }

        List<GameLog> activeLogs = await query
                                         .Where(log => log.EndedAt == null)
                                         .ToListAsync();

        return activeLogs
               .OrderByDescending(log => log.StartedAt)
               .ThenByDescending(log => log.ID)
               .FirstOrDefault();
    }

    public async Task<GameLog?> StartSessionAsync(StartSessionRequest request)
    {
        if (userSession.CurrentUserID is null)
        {
            return null;
        }

        int userProfileId = userSession.CurrentUserID.Value;
        Playthrough? playthrough = null;

        if (request.ExistingPlaythroughID is not null)
        {
            playthrough = await dbContext.Playthroughs
                                         .FirstOrDefaultAsync(item => item.ID == request.ExistingPlaythroughID.Value
                                                                      && item.UserProfileID == userProfileId);
        }

        int? gameId = playthrough?.GameID ?? request.GameID;
        int? platformId = playthrough?.PlatformID ?? request.PlatformID;

        if (gameId is null || platformId is null)
        {
            return null;
        }

        if (playthrough is null)
        {
            string name = string.IsNullOrWhiteSpace(request.NewPlaythroughName)
                              ? await BuildDefaultPlaythroughNameAsync(gameId.Value)
                              : request.NewPlaythroughName.Trim();

            playthrough = new Playthrough
                          {
                              UserProfileID = userProfileId,
                              Name = name,
                              Status = PlaythroughStatus.Playing,
                              GameID = gameId.Value,
                              PlatformID = platformId.Value,
                          };

            dbContext.Playthroughs.Add(playthrough);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            if (playthrough.GameID is null)
            {
                playthrough.GameID = gameId.Value;
            }

            if (playthrough.PlatformID is null)
            {
                playthrough.PlatformID = platformId.Value;
            }
        }

        PlaythroughStatus? statusChange = null;
        if (playthrough.Status is not PlaythroughStatus.Finished and not PlaythroughStatus.Mastered)
        {
            playthrough.Status = PlaythroughStatus.Playing;
            statusChange = PlaythroughStatus.Playing;
        }

        DateTimeOffset now = DateTimeOffset.Now;
        GameLog log = new()
                      {
                          UserProfileID = userProfileId,
                          GameID = gameId.Value,
                          PlatformID = platformId.Value,
                          PlaythroughID = playthrough.ID,
                          StartedAt = now,
                          EndedAt = null,
                          StatusChange = statusChange,
                      };

        dbContext.GameLogs.Add(log);
        await dbContext.SaveChangesAsync();
        ActiveSessionChanged?.Invoke();

        return await GetSessionAsync(log.ID);
    }

    public async Task SaveNotesAsync(int logId, string? notes)
    {
        GameLog? log = await LoadMutableLogAsync(logId);

        if (log is null)
        {
            return;
        }

        log.Notes = Normalize(notes);
        await dbContext.SaveChangesAsync();
        ActiveSessionChanged?.Invoke();
    }

    public async Task SaveStatusChangeAsync(int logId, PlaythroughStatus? statusChange)
    {
        GameLog? log = await LoadMutableLogAsync(logId);

        if (log is null)
        {
            return;
        }

        int playthroughId = log.PlaythroughID;
        log.StatusChange = statusChange;
        await dbContext.SaveChangesAsync();
        await RecalculatePlaythroughStatusAsync(playthroughId);
        ActiveSessionChanged?.Invoke();
    }

    public async Task<GameLog?> EndSessionAsync(int logId)
    {
        GameLog? log = await LoadMutableLogAsync(logId);

        if (log is null)
        {
            return null;
        }

        DateTimeOffset now = DateTimeOffset.Now;
        log.EndedAt = now < log.StartedAt ? log.StartedAt : now;

        await dbContext.SaveChangesAsync();
        await RecalculatePlaythroughStatusAsync(log.PlaythroughID);
        ActiveSessionChanged?.Invoke();

        return await GetSessionAsync(log.ID);
    }

    public async Task RecalculatePlaythroughStatusAsync(int playthroughId)
    {
        if (userSession.CurrentUserID is null)
        {
            return;
        }

        Playthrough? playthrough = await dbContext.Playthroughs
                                                 .FirstOrDefaultAsync(item => item.ID == playthroughId
                                                                              && item.UserProfileID == userSession.CurrentUserID.Value);

        if (playthrough is null)
        {
            return;
        }

        List<GameLog> statusLogs = await dbContext.GameLogs
                                                  .AsNoTracking()
                                                  .Where(log => log.PlaythroughID == playthroughId
                                                                && log.UserProfileID == userSession.CurrentUserID.Value
                                                                && log.StatusChange != null)
                                                  .ToListAsync();

        PlaythroughStatus? latestStatus = statusLogs
                                          .OrderByDescending(log => log.StartedAt)
                                          .ThenByDescending(log => log.ID)
                                          .Select(log => log.StatusChange)
                                          .FirstOrDefault();

        if (latestStatus is null)
        {
            return;
        }

        playthrough.Status = latestStatus.Value;
        await dbContext.SaveChangesAsync();
    }

    private async Task<GameLog?> LoadMutableLogAsync(int logId)
    {
        if (userSession.CurrentUserID is null)
        {
            return null;
        }

        return await dbContext.GameLogs
                              .FirstOrDefaultAsync(log => log.ID == logId
                                                          && log.UserProfileID == userSession.CurrentUserID.Value);
    }

    private async Task<string> BuildDefaultPlaythroughNameAsync(int gameId)
    {
        string? gameName = await dbContext.Games
                                          .AsNoTracking()
                                          .Where(game => game.ID == gameId)
                                          .Select(game => game.Name)
                                          .FirstOrDefaultAsync();

        return string.IsNullOrWhiteSpace(gameName) ? "New Playthrough" : $"{gameName} Playthrough";
    }

    private static IQueryable<GameLog> IncludeSessionDetails(IQueryable<GameLog> query)
    {
        return query
               .Include(log => log.Game)
               .Include(log => log.Playthrough)
               .Include(log => log.Platform);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
