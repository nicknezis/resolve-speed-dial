namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // One keypad key that steps through a chosen set of dial modes, for people who would rather spend one
    // tile than three. Each parameter is a cycle order: every pair, and all three. Pressing advances to
    // the next mode in the cycle; if the current mode is not part of the cycle, it jumps to the first.
    // The tile shows the active mode and the cycle it belongs to.
    public class CycleDialModeCommand : PluginDynamicCommand
    {
        private static readonly BitmapColor Background = new(0, 122, 204);
        private static readonly BitmapColor Foreign = new(60, 60, 64);

        private static readonly IReadOnlyDictionary<String, DialMode[]> Cycles = new Dictionary<String, DialMode[]>
        {
            ["JOG+SHTL"] = new[] { DialMode.Jog, DialMode.Shuttle },
            ["JOG+SCRL"] = new[] { DialMode.Jog, DialMode.Scroll },
            ["SHTL+SCRL"] = new[] { DialMode.Shuttle, DialMode.Scroll },
            ["JOG+SHTL+SCRL"] = new[] { DialMode.Jog, DialMode.Shuttle, DialMode.Scroll },
        };

        public CycleDialModeCommand()
        {
            this.DisplayName = "Cycle Dial Mode";
            this.GroupName = "Dial";
            this.Description = "One key that steps through the chosen dial modes";

            foreach (var (id, modes) in Cycles)
            {
                var names = String.Join(" → ", modes.Select(DialState.Label));
                this.AddParameter(id, $"Cycle dial mode: {names}", "Dial");
            }

            DialState.Changed += () => this.ActionImageChanged();
        }

        protected override void RunCommand(String actionParameter)
        {
            if (!Cycles.TryGetValue(actionParameter ?? "", out var modes))
            {
                return;
            }

            var index = Array.IndexOf(modes, DialState.Mode);
            var next = index < 0 ? modes[0] : modes[(index + 1) % modes.Length];
            DialState.SetMode(next);
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            $"{DialState.Label(DialState.Mode)}\n{actionParameter}";

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var inCycle = Cycles.TryGetValue(actionParameter ?? "", out var modes) && modes.Contains(DialState.Mode);
            var builder = new BitmapBuilder(imageSize);
            builder.Clear(inCycle ? Background : Foreign);
            builder.DrawText($"{DialState.Label(DialState.Mode)}\n{actionParameter?.Replace("+", "·")}", BitmapColor.White);
            return builder.ToImage();
        }
    }
}
