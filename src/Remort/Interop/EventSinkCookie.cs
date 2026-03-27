using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Remort.Interop;

/// <summary>
/// Manages a COM connection point advisory connection (Advise/Unadvise).
/// </summary>
internal sealed class EventSinkCookie
{
    private IConnectionPoint? _cp;
    private int _cookie;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSinkCookie"/> class,
    /// establishing a connection point advisory for the specified event interface.
    /// </summary>
    /// <param name="source">The COM object that implements <see cref="IConnectionPointContainer"/>.</param>
    /// <param name="sink">The event sink object to advise.</param>
    /// <param name="eventInterface">The event interface type (must have a COM GUID).</param>
    /// <exception cref="InvalidCastException">Thrown when <paramref name="source"/> does not implement <see cref="IConnectionPointContainer"/>.</exception>
    public EventSinkCookie(object source, object sink, Type eventInterface)
    {
        if (source is not IConnectionPointContainer cpc)
        {
            throw new InvalidCastException("Source does not implement IConnectionPointContainer");
        }

        Guid iid = eventInterface.GUID;
        cpc.FindConnectionPoint(ref iid, out _cp);
        _cp!.Advise(sink, out _cookie);
    }

    /// <summary>
    /// Disconnects the advisory connection. Safe to call multiple times.
    /// </summary>
    public void Disconnect()
    {
        if (_cp != null && _cookie != 0)
        {
            try
            {
                _cp.Unadvise(_cookie);
            }
            catch (COMException)
            {
                // Best-effort unadvise — COM object may already be released.
            }

            _cookie = 0;
            _cp = null;
        }
    }
}
