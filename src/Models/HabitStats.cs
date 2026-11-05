using System.Text.Json.Serialization;

namespace ScreenHealthGuardian.Models;

public enum BreakOutcome
{
    Respected,
    Discarded,
    Snoozed
}

public class StatCounters
{
    [JsonPropertyName("respected")]
    public int Respected { get; set; } = 0;

    [JsonPropertyName("discarded")]
    public int Discarded { get; set; } = 0;

    [JsonPropertyName("snoozed")]
    public int Snoozed { get; set; } = 0;

    [JsonIgnore]
    public int Total => Respected + Discarded + Snoozed;

    [JsonIgnore]
    public double ComplianceRate
    {
        get
        {
            int evaluated = Respected + Discarded;
            return evaluated > 0 ? ((double)Respected / evaluated) * 100.0 : 0.0;
        }
    }

    public void Add(BreakOutcome outcome)
    {
        switch (outcome)
        {
            case BreakOutcome.Respected:
                Respected++;
                break;
            case BreakOutcome.Discarded:
                Discarded++;
                break;
            case BreakOutcome.Snoozed:
                Snoozed++;
                break;
        }
    }

    public StatCounters Clone() => new()
    {
        Respected = Respected,
        Discarded = Discarded,
        Snoozed = Snoozed
    };
}

public class DailyBreakStats
{
    [JsonPropertyName("eye_rest")]
    public StatCounters EyeRest { get; set; } = new();

    [JsonPropertyName("posture")]
    public StatCounters Posture { get; set; } = new();

    public DailyBreakStats Clone() => new()
    {
        EyeRest = EyeRest.Clone(),
        Posture = Posture.Clone()
    };
}

public class HabitStats
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("lifetime")]
    public DailyBreakStats Lifetime { get; set; } = new();

    [JsonPropertyName("daily")]
    public Dictionary<string, DailyBreakStats> Daily { get; set; } = new();
}
