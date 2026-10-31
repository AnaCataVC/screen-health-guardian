using ScreenHealthGuardian.Models;
using ScreenHealthGuardian.Services;
using Xunit;

namespace ScreenHealthGuardian.Tests;

public class HealthTimerServiceTests
{
    [Fact]
    public void AlertSnoozed_SetsCorrectRemainingTimeForLookAway()
    {
        var configService = new ConfigService();
        configService.Update(c =>
        {
            c.LookAwayIntervalMin = 20;
            c.PostureIntervalMin = 45;
        });

        var timerService = new HealthTimerService(configService);

        // Snooze look-away alert by 5 minutes
        timerService.AlertSnoozed(wasLookAway: true, snoozeMinutes: 5);

        var (lookRem, _) = timerService.GetRemainingMinutes();
        Assert.Equal(5, lookRem);
    }

    [Fact]
    public void AlertSnoozed_SetsCorrectRemainingTimeForPosture()
    {
        var configService = new ConfigService();
        configService.Update(c =>
        {
            c.LookAwayIntervalMin = 20;
            c.PostureIntervalMin = 45;
        });

        var timerService = new HealthTimerService(configService);

        // Snooze posture alert by 5 minutes
        timerService.AlertSnoozed(wasLookAway: false, snoozeMinutes: 5);

        var (_, postRem) = timerService.GetRemainingMinutes();
        Assert.Equal(5, postRem);
    }

    [Theory]
    [InlineData("en", "settings_preview_alert", "Preview Alert")]
    [InlineData("es", "settings_preview_alert", "Probar Alerta")]
    [InlineData("en", "alert_snooze_btn", "Snooze (5m)")]
    [InlineData("es", "alert_snooze_btn", "Posponer (5m)")]
    [InlineData("en", "unit_sec", "sec")]
    [InlineData("es", "unit_sec", "seg")]
    public void LocalizationService_ReturnsExpectedTranslations(string lang, string key, string expected)
    {
        var locService = new LocalizationService();
        locService.SetLanguage(lang);

        string actual = locService.Get(key);
        Assert.Equal(expected, actual);
    }
}
