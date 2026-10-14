namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;

    // Keypad button that turns tick telemetry (log + CSV) on and off.
    public class TelemetryToggleCommand : PluginDynamicCommand
    {
        private static readonly BitmapColor On = new(180, 90, 0);
        private static readonly BitmapColor Off = new(28, 28, 30);

        public TelemetryToggleCommand()
            : base(displayName: "Telemetry", description: "Log every dial tick to the plugin log and ticks.csv", groupName: "Dial")
        {
            DialState.Changed += () => this.ActionImageChanged();
        }

        protected override void RunCommand(String actionParameter)
        {
            DialState.SetTelemetry(!DialState.TelemetryEnabled);
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            DialState.TelemetryEnabled ? "Telemetry\nON" : "Telemetry\noff";

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var builder = new BitmapBuilder(imageSize);
            builder.Clear(DialState.TelemetryEnabled ? On : Off);
            builder.DrawText(DialState.TelemetryEnabled ? "TLM\nON" : "TLM\noff", BitmapColor.White);
            return builder.ToImage();
        }
    }
}
