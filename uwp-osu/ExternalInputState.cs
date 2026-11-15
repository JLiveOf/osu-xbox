using System;
using System.Collections.Generic;

namespace UwpOsu;

// CoreWindow and XAML can report the same transition. Forward physical changes once,
// and explicitly release them when Xbox's Guide or another app takes focus.
internal sealed class ExternalInputState
{
    private readonly HashSet<int> keys = new();
    private readonly HashSet<int> buttons = new();

    public bool SetKey(int key, bool pressed)
    {
        // Gamepad virtual keys are already handled by UwpGamepadHandler.
        if (key >= 0xC3 && key <= 0xDA)
            return false;
        return pressed ? keys.Add(key) : keys.Remove(key);
    }

    public bool SetButton(int button, bool pressed) => pressed ? buttons.Add(button) : buttons.Remove(button);

    public void ReleaseAll(Action<int> releaseKey, Action<int> releaseButton)
    {
        foreach (int key in keys) releaseKey(key);
        foreach (int button in buttons) releaseButton(button);
        keys.Clear();
        buttons.Clear();
    }

    public static (double X, double Y) Move(double x, double y, double dx, double dy, double width, double height)
        => (Math.Clamp(x + dx, 0, Math.Max(0, width - 1)), Math.Clamp(y + dy, 0, Math.Max(0, height - 1)));
}
