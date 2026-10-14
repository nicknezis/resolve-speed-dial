namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;

    // The dial action. Assign the "AUTO" parameter to the Dialpad wheel and switch behaviour with the
    // JOG / SHTL / SCRL keypad buttons, or assign a fixed-mode parameter directly.
    public class SpeedEditorDialAdjustment : PluginDynamicAdjustment
    {
        private const String Auto = "AUTO";
        private readonly RateMeter _rate = new();

        public SpeedEditorDialAdjustment()
            : base(hasReset: true)
        {
            this.DisplayName = "Speed Editor Dial";
            this.GroupName = "Dial";
            this.Description = "JOG / SHTL / SCRL dial behaviour for DaVinci Resolve. The dial press / Reset command sends K (stop) and re-centres SHTL.";

            this.AddParameter(Auto, "Speed Editor Dial (follows mode buttons)", "Dial");
            this.AddParameter("JOG", "Dial: JOG (frames)", "Dial");
            this.AddParameter("SHTL", "Dial: SHTL (position sets speed)", "Dial");
            this.AddParameter("SCRL", "Dial: SCRL (seconds)", "Dial");

            DialState.Changed += () => this.AdjustmentValueChanged();
        }

        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            if (diff == 0)
            {
                return;
            }

            var sample = this._rate.Observe(diff);
            var mode = ModeFor(actionParameter);
            var sent = DialModes.Get(mode).Apply(this.Plugin, diff, sample);
            TickTelemetry.Record(mode, diff, sample, sent);
            this.AdjustmentValueChanged();
        }

        // Dial press.
        protected override void RunCommand(String actionParameter)
        {
            DialModes.Get(ModeFor(actionParameter)).Press(this.Plugin);
            this.AdjustmentValueChanged();
        }

        protected override String GetAdjustmentValue(String actionParameter) =>
            DialModes.Get(ModeFor(actionParameter)).Status;

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize) =>
            actionParameter == Auto ? "Speed Dial" : $"Dial {actionParameter}";

        private static DialMode ModeFor(String actionParameter) =>
            actionParameter != Auto && DialState.TryParse(actionParameter, out var fixedMode) ? fixedMode : DialState.Mode;
    }
}
