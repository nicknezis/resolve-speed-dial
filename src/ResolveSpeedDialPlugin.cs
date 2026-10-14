namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;

    // Prototype plugin: makes the MX Creative Dialpad wheel behave like the DaVinci Resolve
    // Speed Editor's three dial modes (JOG / SHTL / SCRL) by sending Resolve keystrokes.
    public class ResolveSpeedDialPlugin : Plugin
    {
        // Keystroke-only plugin: no Resolve-side API.
        public override Boolean UsesApplicationApiOnly => true;

        // Universal plugin: its actions are offered in every application's profile, including Logitech's
        // own "DaVinci Resolve" one. Declaring it as an application plugin instead would register a second
        // application matching the Resolve process, and the service would then switch devices between
        // the two profiles unpredictably when Resolve comes to the front.
        public override Boolean HasNoApplication => true;

        public ResolveSpeedDialPlugin()
        {
            PluginLog.Init(this.Log);
            PluginResources.Init(this.Assembly);
        }

        public override void Load()
        {
            DialState.Load(this);
            KeySender.Start(this);
            TickTelemetry.Init(this);
            PluginLog.Info($"ResolveSpeedDial loaded. mode={DialState.Mode} telemetry={DialState.TelemetryEnabled}");
        }

        public override void Unload()
        {
            TickTelemetry.Flush();
        }
    }
}
