using PinTop.Interop;

namespace PinTop.Services;

/// <summary>Keeps track of the windows pinned by PinTop during the current session.</summary>
internal sealed class PinnedWindowManager
{
    private readonly Dictionary<IntPtr, string> _windows = new();

    public event EventHandler? Changed;

    public int Count => _windows.Count;

    public IReadOnlyDictionary<IntPtr, string> Windows => _windows;

    public bool Contains(IntPtr hwnd) => _windows.ContainsKey(hwnd);

    public void Track(IntPtr hwnd, string title)
    {
        _windows[hwnd] = title;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Untrack(IntPtr hwnd)
    {
        if (_windows.Remove(hwnd))
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Forgets windows that were closed in the meantime.</summary>
    public void Prune()
    {
        var dead = _windows.Keys.Where(hwnd => !NativeMethods.IsWindow(hwnd)).ToList();
        if (dead.Count == 0)
        {
            return;
        }

        foreach (var hwnd in dead)
        {
            _windows.Remove(hwnd);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Removes the topmost flag from every pinned window (used on exit).</summary>
    public void UnpinAll()
    {
        foreach (var hwnd in _windows.Keys.ToList())
        {
            if (NativeMethods.IsWindow(hwnd) && WindowPinner.IsTopmost(hwnd))
            {
                WindowPinner.SetTopmost(hwnd, false);
            }
        }

        if (_windows.Count > 0)
        {
            _windows.Clear();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
