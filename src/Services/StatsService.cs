using System.Globalization;
using System.IO;
using System.Text.Json;
using ScreenHealthGuardian.Models;

namespace ScreenHealthGuardian.Services;

/// <summary>
/// Thread-safe service for managing habit statistics, rolling 6-month retention,
/// and atomic persistence in %APPDATA%/ScreenHealthGuardian/stats.json.
/// </summary>
public class StatsService
{
    private const int MaxRetentionDays = 180; // 6 months maximum retention
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly string StatsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScreenHealthGuardian"
    );

    private static readonly string StatsPath = Path.Combine(StatsDir, "stats.json");
    private readonly object _lock = new();
    private HabitStats _stats = new();

    public event Action? OnStatsUpdated;

    public StatsService()
    {
        Load();
    }

    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(StatsPath))
                {
                    string json = File.ReadAllText(StatsPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        _stats = JsonSerializer.Deserialize<HabitStats>(json) ?? new HabitStats();
                        PruneOldRecords();
                        return;
                    }
                }

                _stats = new HabitStats();
                SaveInternal();
            }
            catch (Exception)
            {
                // In case of corrupted file, back it up safely and reset
                try
                {
                    string corruptPath = Path.Combine(StatsDir, $"stats.corrupt_{DateTime.Now:yyyyMMddHHmmss}.json");
                    if (File.Exists(StatsPath))
                    {
                        File.Move(StatsPath, corruptPath, overwrite: true);
                    }
                }
                catch
                {
                    // Ignore backup failures
                }

                _stats = new HabitStats();
                SaveInternal();
            }
        }
    }

    public void RecordEyeRest(BreakOutcome outcome)
    {
        lock (_lock)
        {
            _stats.Lifetime.EyeRest.Add(outcome);

            string todayKey = DateTime.Today.ToString(DateFormat, CultureInfo.InvariantCulture);
            if (!_stats.Daily.TryGetValue(todayKey, out var dailyBreak))
            {
                dailyBreak = new DailyBreakStats();
                _stats.Daily[todayKey] = dailyBreak;
            }

            dailyBreak.EyeRest.Add(outcome);
            PruneOldRecords();
            SaveInternal();
        }

        OnStatsUpdated?.Invoke();
    }

    public void RecordPosture(BreakOutcome outcome)
    {
        lock (_lock)
        {
            _stats.Lifetime.Posture.Add(outcome);

            string todayKey = DateTime.Today.ToString(DateFormat, CultureInfo.InvariantCulture);
            if (!_stats.Daily.TryGetValue(todayKey, out var dailyBreak))
            {
                dailyBreak = new DailyBreakStats();
                _stats.Daily[todayKey] = dailyBreak;
            }

            dailyBreak.Posture.Add(outcome);
            PruneOldRecords();
            SaveInternal();
        }

        OnStatsUpdated?.Invoke();
    }

    public DailyBreakStats GetLifetimeSummary()
    {
        lock (_lock)
        {
            return _stats.Lifetime.Clone();
        }
    }

    public DailyBreakStats GetWeeklySummary()
    {
        lock (_lock)
        {
            var summary = new DailyBreakStats();
            var today = DateTime.Today;

            // Rolling last 7 days window (including today)
            for (int i = 0; i < 7; i++)
            {
                string dateKey = today.AddDays(-i).ToString(DateFormat, CultureInfo.InvariantCulture);
                if (_stats.Daily.TryGetValue(dateKey, out var dayStats))
                {
                    summary.EyeRest.Respected += dayStats.EyeRest.Respected;
                    summary.EyeRest.Discarded += dayStats.EyeRest.Discarded;
                    summary.EyeRest.Snoozed += dayStats.EyeRest.Snoozed;

                    summary.Posture.Respected += dayStats.Posture.Respected;
                    summary.Posture.Discarded += dayStats.Posture.Discarded;
                    summary.Posture.Snoozed += dayStats.Posture.Snoozed;
                }
            }

            return summary;
        }
    }

    public void ResetStats()
    {
        lock (_lock)
        {
            _stats = new HabitStats();
            SaveInternal();
        }

        OnStatsUpdated?.Invoke();
    }

    private void PruneOldRecords()
    {
        // Remove records older than 180 days (6 months)
        var cutoff = DateTime.Today.AddDays(-MaxRetentionDays);
        var expiredKeys = new List<string>();

        foreach (var key in _stats.Daily.Keys)
        {
            if (DateTime.TryParseExact(key, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                if (parsedDate < cutoff)
                {
                    expiredKeys.Add(key);
                }
            }
            else
            {
                // Malformed date key
                expiredKeys.Add(key);
            }
        }

        foreach (var expired in expiredKeys)
        {
            _stats.Daily.Remove(expired);
        }
    }

    private void SaveInternal()
    {
        lock (_lock)
        {
            string tempPath = string.Empty;
            try
            {
                if (!Directory.Exists(StatsDir))
                {
                    Directory.CreateDirectory(StatsDir);
                }

                string json = JsonSerializer.Serialize(_stats, new JsonSerializerOptions { WriteIndented = true });
                tempPath = Path.Combine(StatsDir, $"stats_{Guid.NewGuid():N}.tmp");

                File.WriteAllText(tempPath, json);
                File.Move(tempPath, StatsPath, overwrite: true);
            }
            catch
            {
                // Silently handle transient IO errors
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore cleanup failure
                    }
                }
            }
        }
    }
}
