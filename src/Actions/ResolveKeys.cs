namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;

    // One Resolve keyboard shortcut.
    public sealed record ResolveKey(VirtualKeyCode Key, ModifierKey Modifiers = ModifierKey.None, String Name = null)
    {
        public override String ToString() => this.Name ?? $"{this.Modifiers}+{this.Key}";
    }

    // DaVinci Resolve default shortcuts (US keyboard), as also used by Logitech's own Resolve plugin.
    public static class ResolveKeys
    {
        public static readonly ResolveKey FrameBack     = new(VirtualKeyCode.ArrowLeft,  ModifierKey.None,  "Left");
        public static readonly ResolveKey FrameForward  = new(VirtualKeyCode.ArrowRight, ModifierKey.None,  "Right");
        public static readonly ResolveKey SecondBack    = new(VirtualKeyCode.ArrowLeft,  ModifierKey.Shift, "Shift+Left");
        public static readonly ResolveKey SecondForward = new(VirtualKeyCode.ArrowRight, ModifierKey.Shift, "Shift+Right");
        public static readonly ResolveKey PlayForward   = new(VirtualKeyCode.KeyL,       ModifierKey.None,  "L");
        public static readonly ResolveKey PlayReverse   = new(VirtualKeyCode.KeyJ,       ModifierKey.None,  "J");
        public static readonly ResolveKey Stop          = new(VirtualKeyCode.KeyK,       ModifierKey.None,  "K");
        public static readonly ResolveKey ClipPrevious  = new(VirtualKeyCode.ArrowUp,    ModifierKey.None,  "Up");
        public static readonly ResolveKey ClipNext      = new(VirtualKeyCode.ArrowDown,  ModifierKey.None,  "Down");
    }
}
