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
    /// Sends Win+Tab to invoke the host Windows Task View.
    /// </summary>
    internal static void SendTaskViewKeyPress()
    {
        INPUT[] inputs =
        [
            CreateKeyDown(VK_LWIN),
            CreateKeyDown(VK_TAB),
            CreateKeyUp(VK_TAB),
            CreateKeyUp(VK_LWIN),
        ];

        _ = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
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
