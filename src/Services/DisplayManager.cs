using System.Runtime.InteropServices;
using System.Windows;

namespace ScreenHealthGuardian.Services;

/// <summary>
/// Represents the work area and positioning coordinates of a display monitor in WPF DIPs.
/// </summary>
public record DisplayInfo(double LeftDip, double TopDip, double WidthDip, double HeightDip, bool IsPrimary)
{
    public (double Left, double Top) CalculateCenter(double windowWidthDip, double windowHeightDip)
    {
        double left = LeftDip + Math.Max(0, (WidthDip - windowWidthDip) / 2.0);
        double top = TopDip + Math.Max(0, (HeightDip - windowHeightDip) / 2.0);
        return (left, top);
    }
}

/// <summary>
/// Native Win32 monitor enumeration service with per-monitor DPI handling, cursor-monitor resolution, and safe WPF fallbacks.
/// </summary>
public static class DisplayManager
{
    private const uint MONITORINFOF_PRIMARY = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    private enum MonitorDpiType
    {
        MdtEffectiveDpi = 0,
        MdtAngularDpi = 1,
        MdtRawDpi = 2,
        MdtDefault = MdtEffectiveDpi
    }

    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

    private record RawDisplayItem(DisplayInfo Info, RECT PhysicalMonitorRect);

    private static List<RawDisplayItem> EnumerateAllRawDisplays()
    {
        var list = new List<RawDisplayItem>();

        try
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data) =>
            {
                var mi = new MONITORINFO();
                mi.cbSize = Marshal.SizeOf(typeof(MONITORINFO));

                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    double dpiScaleX = 1.0;
                    double dpiScaleY = 1.0;

                    try
                    {
                        if (GetDpiForMonitor(hMonitor, MonitorDpiType.MdtEffectiveDpi, out uint dpiX, out uint dpiY) == 0)
                        {
                            dpiScaleX = dpiX / 96.0;
                            dpiScaleY = dpiY / 96.0;
                        }
                    }
                    catch
                    {
                        // Default to 1.0 (96 DPI) if Shcore.dll is unavailable or fails
                        dpiScaleX = 1.0;
                        dpiScaleY = 1.0;
                    }

                    if (dpiScaleX <= 0) dpiScaleX = 1.0;
                    if (dpiScaleY <= 0) dpiScaleY = 1.0;

                    double leftDip = mi.rcWork.Left / dpiScaleX;
                    double topDip = mi.rcWork.Top / dpiScaleY;
                    double widthDip = (mi.rcWork.Right - mi.rcWork.Left) / dpiScaleX;
                    double heightDip = (mi.rcWork.Bottom - mi.rcWork.Top) / dpiScaleY;
                    bool isPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0;

                    var info = new DisplayInfo(leftDip, topDip, widthDip, heightDip, isPrimary);
                    list.Add(new RawDisplayItem(info, mi.rcMonitor));
                }

                return true;
            }, IntPtr.Zero);
        }
        catch
        {
            // Suppress P/Invoke errors and fallback below
        }

        // Fallback to primary SystemParameters if enumeration returned no monitors
        if (list.Count == 0)
        {
            var fallbackInfo = new DisplayInfo(
                LeftDip: SystemParameters.WorkArea.Left,
                TopDip: SystemParameters.WorkArea.Top,
                WidthDip: SystemParameters.WorkArea.Width,
                HeightDip: SystemParameters.WorkArea.Height,
                IsPrimary: true
            );
            list.Add(new RawDisplayItem(fallbackInfo, new RECT { Left = 0, Top = 0, Right = (int)SystemParameters.PrimaryScreenWidth, Bottom = (int)SystemParameters.PrimaryScreenHeight }));
        }

        return list;
    }

    /// <summary>
    /// Enumerates all active display monitors and returns their DIP work areas.
    /// </summary>
    public static IReadOnlyList<DisplayInfo> GetDisplays()
    {
        return EnumerateAllRawDisplays().Select(d => d.Info).ToList();
    }

    /// <summary>
    /// Resolves the specific display monitor that currently contains the mouse cursor.
    /// Falls back to the primary display if cursor coordinates lie outside all monitors.
    /// </summary>
    public static DisplayInfo GetCursorDisplay()
    {
        var rawDisplays = EnumerateAllRawDisplays();
        if (rawDisplays.Count <= 1)
        {
            return rawDisplays[0].Info;
        }

        if (GetCursorPos(out POINT pt))
        {
            foreach (var item in rawDisplays)
            {
                if (pt.X >= item.PhysicalMonitorRect.Left && pt.X < item.PhysicalMonitorRect.Right &&
                    pt.Y >= item.PhysicalMonitorRect.Top && pt.Y < item.PhysicalMonitorRect.Bottom)
                {
                    return item.Info;
                }
            }
        }

        // Fallback to primary display
        return rawDisplays.FirstOrDefault(d => d.Info.IsPrimary)?.Info ?? rawDisplays[0].Info;
    }
}
