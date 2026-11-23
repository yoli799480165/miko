using Miko.Events;
using Miko.Platform;

namespace Miko.HarmonyOS;

// XComponent coordinates are physical pixels. The engine owns one pointer; secondary fingers
// must never replace the active one or synthesize a click when the OS cancels a gesture.
internal sealed class HarmonyTouchTracker
{
    public int? ActivePointer { get; private set; }

    public void Dispatch(MikoInteractionController controller, int action, int id, float x, float y, float density)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(density) || density <= 0) return;
        x /= density;
        y /= density;
        switch (action)
        {
            case 0 when ActivePointer is null:
                ActivePointer = id;
                controller.OnPointerDown(x, y, MouseButton.Left, PointerType.Touch, id);
                break;
            case 1 when ActivePointer == id:
                controller.OnPointerUp(x, y, MouseButton.Left, PointerType.Touch, id);
                ActivePointer = null;
                break;
            case 2 when ActivePointer == id:
                controller.OnPointerMove(x, y);
                break;
            case 3 when ActivePointer is { } active:
                controller.OnPointerCancel(x, y, active);
                ActivePointer = null;
                break;
        }
    }

    public void Cancel(MikoInteractionController controller)
    {
        if (ActivePointer is { } id) controller.OnPointerCancel(0, 0, id);
        ActivePointer = null;
    }
}
