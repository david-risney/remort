using System.Runtime.InteropServices;

namespace Remort.Interop;

/// <summary>
/// Implements <see cref="IMsTscAxEvents"/>, routing COM event callbacks to
/// <see cref="RdpClientHost"/> CLR events.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
internal sealed class MsTscAxEventsSink : IMsTscAxEvents
{
    private readonly RdpClientHost _host;

    public MsTscAxEventsSink(RdpClientHost host) => _host = host;

    public void OnConnecting()
    {
        _host.RaiseConnecting();
    }

    public void OnConnected()
    {
        _host.RaiseConnected();
    }

    public void OnLoginComplete()
    {
    }

    public void OnDisconnected(int discReason)
    {
        _host.RaiseDisconnected(discReason);
    }

    public void OnEnterFullScreenMode()
    {
    }

    public void OnLeaveFullScreenMode()
    {
    }

    public void OnChannelReceivedData(string chanName, string data)
    {
    }

    public void OnRequestGoFullScreen()
    {
    }

    public void OnRequestLeaveFullScreen()
    {
    }

    public void OnFatalError(int errorCode)
    {
        _host.RaiseFatalError(errorCode);
    }

    public void OnWarning(int warningCode)
    {
        _host.RaiseWarning(warningCode);
    }

    public void OnRemoteDesktopSizeChange(int width, int height)
    {
    }

    public void OnIdleTimeoutNotification()
    {
    }

    public void OnRequestContainerMinimize()
    {
    }
}
