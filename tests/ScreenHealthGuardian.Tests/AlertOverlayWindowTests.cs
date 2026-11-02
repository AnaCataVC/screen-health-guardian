using System.Threading;
using System.Windows;
using ScreenHealthGuardian.Views;
using Xunit;

namespace ScreenHealthGuardian.Tests;

public class AlertOverlayWindowTests
{
    [Fact]
    public void AlertOverlayWindow_PostureAccent_AppliesSecondaryAccentStyle()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var window = new AlertOverlayWindow(
                    emoji: "✨",
                    title: "Posture Check",
                    message: "Keep your back straight",
                    buttonText: "Posture corrected",
                    accentHex: "#b388ff",
                    autoDismissSec: 0,
                    soundEnabled: false
                );

                Assert.NotNull(window.DismissButton);
                Assert.NotNull(Application.Current);
                var expectedStyle = Application.Current.TryFindResource("ModernSecondaryAccentButton") as Style;
                Assert.NotNull(expectedStyle);
                Assert.Same(expectedStyle, window.DismissButton.Style);
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
    public void AlertOverlayWindow_LookAwayAccent_KeepsModernPrimaryButtonStyle()
    {
        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (Application.Current == null)
                {
                    var app = new App();
                    app.InitializeComponent();
                }

                var window = new AlertOverlayWindow(
                    emoji: "👀",
                    title: "Rest Your Eyes",
                    message: "Look away",
                    buttonText: "Done",
                    accentHex: "#4fc3f7",
                    autoDismissSec: 0,
                    soundEnabled: false
                );

                Assert.NotNull(window.DismissButton);
                Assert.NotNull(Application.Current);
                var expectedStyle = Application.Current.TryFindResource("ModernPrimaryButton") as Style;
                Assert.NotNull(expectedStyle);
                Assert.Same(expectedStyle, window.DismissButton.Style);
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
