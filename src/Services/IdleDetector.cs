using System.Runtime.InteropServices;

namespace ScreenHealthGuardian.Services;

/// <summary>
/// Native Win32 idle detection using GetLastInputInfo.
/// </summary>
public static class IdleDetector
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();

    /// <summary>
    /// Returns the number of seconds since the last keyboard or mouse input.
    /// </summary>
    public static double GetIdleSeconds()
    {
        LASTINPUTINFO lastInput = new LASTINPUTINFO();
        lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);

        if (!GetLastInputInfo(ref lastInput))
        {
            return 0.0;
        }

        uint currentTick = GetTickCount();
        uint idleMs = currentTick - lastInput.dwTime;
        return idleMs / 1000.0;
    }

    /// <summary>
    /// Checks if the user was active within the given threshold (in seconds).
    /// </summary>
    public static bool IsUserActive(double thresholdSec = 120.0)
    {
        return GetIdleSeconds() < thresholdSec;
    }
}
