using System.Runtime.InteropServices;

namespace Remort.Interop;

/// <summary>
/// The IMsTscAxEvents dispatch interface as declared in the MSTSCAX type library.
/// DIID: {336D5562-EFA8-482E-8CB3-C5C0FC7A7DB6}.
/// Declaring it with InterfaceIsIDispatch lets the CLR handle IDispatch::Invoke
/// marshalling and route calls to the matching [DispId] methods on our sink.
/// </summary>
[ComImport]
[Guid("336D5562-EFA8-482E-8CB3-C5C0FC7A7DB6")]
[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
internal interface IMsTscAxEvents
{
    /// <summary>Fired when the client begins connecting.</summary>
    [DispId(1)]
    public void OnConnecting();

    /// <summary>Fired when the connection is established.</summary>
    [DispId(2)]
    public void OnConnected();

    /// <summary>Fired when login is complete.</summary>
    [DispId(3)]
    public void OnLoginComplete();

    /// <summary>Fired when the session disconnects.</summary>
    /// <param name="discReason">The disconnect reason code.</param>
    [DispId(4)]
    public void OnDisconnected(int discReason);

    /// <summary>Fired when entering full-screen mode.</summary>
    [DispId(5)]
    public void OnEnterFullScreenMode();

    /// <summary>Fired when leaving full-screen mode.</summary>
    [DispId(6)]
    public void OnLeaveFullScreenMode();

    /// <summary>Fired when channel data is received.</summary>
    /// <param name="chanName">The channel name.</param>
    /// <param name="data">The channel data.</param>
    [DispId(7)]
    public void OnChannelReceivedData(
        [MarshalAs(UnmanagedType.BStr)] string chanName,
        [MarshalAs(UnmanagedType.BStr)] string data);

    /// <summary>Fired when the control requests full-screen mode.</summary>
    [DispId(8)]
    public void OnRequestGoFullScreen();

    /// <summary>Fired when the control requests leaving full-screen mode.</summary>
    [DispId(9)]
    public void OnRequestLeaveFullScreen();

    /// <summary>Fired on a fatal error.</summary>
    /// <param name="errorCode">The fatal error code.</param>
    [DispId(10)]
    public void OnFatalError(int errorCode);

    /// <summary>Fired on a non-fatal warning.</summary>
    /// <param name="warningCode">The warning code.</param>
    [DispId(11)]
    public void OnWarning(int warningCode);

    /// <summary>Fired when the remote desktop size changes.</summary>
    /// <param name="width">The new width in pixels.</param>
    /// <param name="height">The new height in pixels.</param>
    [DispId(12)]
    public void OnRemoteDesktopSizeChange(int width, int height);

    /// <summary>Fired on idle timeout notification.</summary>
    [DispId(13)]
    public void OnIdleTimeoutNotification();

    /// <summary>Fired when the control requests container minimization.</summary>
    [DispId(14)]
    public void OnRequestContainerMinimize();
}
