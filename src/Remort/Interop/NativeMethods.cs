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
    internal const ushort VK_LEFT = 0x25;
    internal const ushort VK_RIGHT = 0x27;

    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

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
