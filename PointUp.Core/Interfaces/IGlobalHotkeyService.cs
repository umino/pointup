using PointUp.Core.Models;

namespace PointUp.Core.Interfaces;

public interface IGlobalHotkeyService
{
    event EventHandler ToggleRequested;
    event EventHandler ClearRequested;
    void Register(ShortcutDefinition toggle, ShortcutDefinition clear);
    void Unregister();
}
