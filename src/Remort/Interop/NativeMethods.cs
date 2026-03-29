using System.Runtime.InteropServices;

namespace Remort.Interop;

/// <summary>
/// P/Invoke declarations for Win32 input simulation.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Matches Win32 API struct layout.")]
internal static partial class NativeMethods
{
    internal const int INPUT_KEYBOARD = 1;
    internal const uint KEYEVENTF_KEYUP = 0x0002;
    internal const ushort VK_LWIN = 0x5B;
    internal const ushort VK_LCONTROL = 0xA2;
    internal const ushort VK_TAB = 0x09;
    internal const ushort VK_LEFT = 0x25;
    internal const ushort VK_RIGHT = 0x27;

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>
    /// Opens the host Windows Task View by simulating Win+Tab.
    /// Uses keybd_event which injects at a lower level than SendInput,
    /// ensuring the keypress reaches the host OS even when an RDP session has focus.
    /// </summary>
    internal static void SendTaskViewKeyPress()
    {
        const byte vkLWin = 0x5B;
        const byte vkTab = 0x09;
        const uint keyUp = 0x0002;

        KeybdEvent(vkLWin, 0, 0, UIntPtr.Zero);       // Win down
        KeybdEvent(vkTab, 0, 0, UIntPtr.Zero);         // Tab down
        KeybdEvent(vkTab, 0, keyUp, UIntPtr.Zero);     // Tab up
        KeybdEvent(vkLWin, 0, keyUp, UIntPtr.Zero);    // Win up
    }

    private static INPUT CreateKeyDown(ushort vk)
    {
        return new INPUT
        {
            Type = INPUT_KEYBOARD,
            Union = new INPUTUNION { Keyboard = new KEYBDINPUT { VirtualKey = vk } },
        };
    }

    private static INPUT CreateKeyUp(ushort vk)
    {
        return new INPUT
        {
            Type = INPUT_KEYBOARD,
            Union = new INPUTUNION { Keyboard = new KEYBDINPUT { VirtualKey = vk, Flags = KEYEVENTF_KEYUP } },
        };
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll", EntryPoint = "keybd_event")]
    private static extern void KeybdEvent(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [StructLayout(LayoutKind.Sequential)]
    internal struct INPUT
    {
        public int Type;
        public INPUTUNION Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct INPUTUNION
    {
        [FieldOffset(0)]
        public KEYBDINPUT Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct KEYBDINPUT
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }
}
