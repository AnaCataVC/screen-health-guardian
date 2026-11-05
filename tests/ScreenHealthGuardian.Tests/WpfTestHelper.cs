using System.Windows;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ScreenHealthGuardian.Tests;

public static class WpfTestHelper
{
    private static readonly object Lock = new();

    public static void EnsureApplication()
    {
        lock (Lock)
        {
            if (Application.Current == null)
            {
                var app = new App();
                app.InitializeComponent();
            }
        }
    }
}
