namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;

    // One Speed Editor dial behaviour.
    public interface IDialMode
    {
        DialMode Mode { get; }

        // Called for every dial event. Returns true if at least one keystroke was sent.
        Boolean Apply(Plugin plugin, Int32 diff, RateSample rate);

        // Dial press (the adjustment's reset command).
        void Press(Plugin plugin);

        // Short status shown next to the dial in Options+ / on device.
        String Status { get; }

        // Forget accumulated state (mode switch).
        void Reset();
    }

    public static class DialModes
    {
        public static readonly JogMode Jog = new();
        public static readonly ShuttleMode Shuttle = new();
        public static readonly ScrollMode Scroll = new();

        public static IDialMode Get(DialMode mode) => mode switch
        {
            DialMode.Shuttle => Shuttle,
            DialMode.Scroll => Scroll,
            _ => Jog,
        };
    }
}
