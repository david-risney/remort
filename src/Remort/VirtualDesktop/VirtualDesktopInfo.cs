namespace Remort.VirtualDesktop;

/// <summary>
/// Describes a Windows virtual desktop for display in the desktop switcher.
/// </summary>
/// <param name="Id">The virtual desktop's GUID as stored in the Windows Registry.</param>
/// <param name="Name">The display name: custom name on Win11, or "Desktop N" fallback.</param>
/// <param name="Index">The 0-based ordinal position in the desktop list.</param>
public sealed record VirtualDesktopInfo(Guid Id, string Name, int Index);
