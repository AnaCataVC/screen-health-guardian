using System.Threading;
using ScreenHealthGuardian.Models;
using ScreenHealthGuardian.Services;
using ScreenHealthGuardian.Views;
using Xunit;

namespace ScreenHealthGuardian.Tests;

public class StatsServiceTests
{
    [Fact]
    public void StatCounters_ComplianceRate_CalculatesCorrectly()
    {
        var counters = new StatCounters();
        Assert.Equal(0, counters.Total);
        Assert.Equal(0.0, counters.ComplianceRate);

        counters.Add(BreakOutcome.Respected);
        counters.Add(BreakOutcome.Respected);
        counters.Add(BreakOutcome.Respected);
        counters.Add(BreakOutcome.Respected); // 4 respected
        counters.Add(BreakOutcome.Discarded); // 1 discarded
        counters.Add(BreakOutcome.Snoozed);   // 1 snoozed

        Assert.Equal(6, counters.Total);
        // Evaluated = 4 + 1 = 5. Compliance = 4 / 5 * 100 = 80.0%
        Assert.Equal(80.0, counters.ComplianceRate);
    }

    [Fact]
    public void StatsService_RecordEyeRest_IncrementsLifetimeAndDaily()
    {
        var service = new StatsService();
        service.ResetStats();

        service.RecordEyeRest(BreakOutcome.Respected);
        service.RecordEyeRest(BreakOutcome.Discarded);
        service.RecordEyeRest(BreakOutcome.Snoozed);

        var lifetime = service.GetLifetimeSummary();
        Assert.Equal(1, lifetime.EyeRest.Respected);
        Assert.Equal(1, lifetime.EyeRest.Discarded);
        Assert.Equal(1, lifetime.EyeRest.Snoozed);
        Assert.Equal(3, lifetime.EyeRest.Total);
        Assert.Equal(50.0, lifetime.EyeRest.ComplianceRate);

        var weekly = service.GetWeeklySummary();
        Assert.Equal(1, weekly.EyeRest.Respected);
        Assert.Equal(1, weekly.EyeRest.Discarded);
        Assert.Equal(1, weekly.EyeRest.Snoozed);
    }

    [Fact]
    public void StatsService_ResetStats_ClearsAllData()
    {
        var service = new StatsService();
        service.RecordEyeRest(BreakOutcome.Respected);
        service.RecordPosture(BreakOutcome.Respected);

        service.ResetStats();

        var lifetime = service.GetLifetimeSummary();
        Assert.Equal(0, lifetime.EyeRest.Total);
        Assert.Equal(0, lifetime.Posture.Total);

        var weekly = service.GetWeeklySummary();
        Assert.Equal(0, weekly.EyeRest.Total);
        Assert.Equal(0, weekly.Posture.Total);
    }

    [Fact]
    public void AlertOverlayWindow_GuidedLookAway_AppliesDarkSkipButton()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                WpfTestHelper.EnsureApplication();

                bool completedCalled = false;
                bool skippedCalled = false;
                bool snoozedCalled = false;

                var window = new AlertOverlayWindow(
                    emoji: "👀",
                    title: "Rest Your Eyes",
                    message: "Look away",
                    buttonText: "Skip",
                    accentHex: "#4fc3f7",
                    autoDismissSec: 0,
                    soundEnabled: false,
                    isGuidedLookAway: true,
                    restingTextFormat: "Resting eyes... {0}s",
                    completedText: "Done!",
                    onCompleted: () => completedCalled = true,
                    onSkipped: () => skippedCalled = true,
                    snoozeButtonText: "Snooze",
                    onSnoozed: () => snoozedCalled = true
                );

                Assert.NotNull(window.DismissButton);
                Assert.NotNull(System.Windows.Application.Current);
                var darkStyle = System.Windows.Application.Current.TryFindResource("ModernDarkButton") as System.Windows.Style;
                Assert.NotNull(darkStyle);
                Assert.Same(darkStyle, window.DismissButton.Style);

                // Test Skip trigger
                window.Skip();
                Assert.True(skippedCalled);
                Assert.False(completedCalled);
                Assert.False(snoozedCalled);
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null)
        {
            throw threadEx;
        }
    }

    [Fact]
    public void AlertOverlayWindow_GuidedLookAway_CompleteCallbackTriggers()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                WpfTestHelper.EnsureApplication();

                bool completedCalled = false;
                bool skippedCalled = false;

                var window = new AlertOverlayWindow(
                    emoji: "👀",
                    title: "Rest Your Eyes",
                    message: "Look away",
                    buttonText: "Skip",
                    accentHex: "#4fc3f7",
                    autoDismissSec: 0,
                    soundEnabled: false,
                    isGuidedLookAway: true,
                    onCompleted: () => completedCalled = true,
                    onSkipped: () => skippedCalled = true
                );

                window.Complete();
                Assert.True(completedCalled);
                Assert.False(skippedCalled);
            }
            catch (Exception ex)
            {
                threadEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (threadEx != null)
        {
            throw threadEx;
        }
    }
}
