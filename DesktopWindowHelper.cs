using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
namespace DesktopImgFrame;
public static class DesktopWindowHelper
{
    private static bool IsWindows11_24H2()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (key == null) return false;

        var displayVersion = key.GetValue("DisplayVersion") as string;
        return displayVersion == "24H2";
    }
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const uint WM_SPAWN_WORKERW = 0x052C;
    private const uint SMTO_NORMAL = 0x0000;

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    static bool IsWindowClass(IntPtr hWnd, string className)
    {
        StringBuilder sb = new StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString() == className;
    }

    public static bool EmbedWindowToDesktop(Window window)
    {
        IntPtr progman = FindWindow("Progman", null);
        SendMessageTimeout(progman, WM_SPAWN_WORKERW, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, 1000, out _);
        IntPtr hwnd = new WindowInteropHelper(window).Handle;
        if (IsWindows11_24H2())
        {
            // 在 Windows 11 24H2 版本中，使用新的 WorkerW 和 SHELLDLL_DefView 结构
            //bug: 刷新图标时会消失
            //Progman->WorkerW   |     SHELLDLL_DefView
            IntPtr workerW = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
            IntPtr shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (workerW != IntPtr.Zero && shellView != IntPtr.Zero)
            {
                SetParent(hwnd, shellView);

                return true;
            }
        }
        else
        {
            // 遍历查找 WorkerW -> SHELLDLL_DefView 结构
            IntPtr workerw = IntPtr.Zero;
            EnumWindows((topHandle, _) =>
            {
                if (IsWindowClass(topHandle, "WorkerW"))
                {
                    IntPtr shellViewWin = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (shellViewWin != IntPtr.Zero)
                    {
                        workerw = topHandle;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (workerw != IntPtr.Zero)
            {
                // 嵌入WorkerW窗口
                SetParent(hwnd, workerw);
            }
        }

        return false;
    }
}
