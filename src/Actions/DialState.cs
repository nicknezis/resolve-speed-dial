namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;

    public enum DialMode
    {
        Jog,
        Shuttle,
        Scroll,
    }

    // Shared state: which Speed Editor dial mode is active, and whether telemetry is on.
    // Persisted through plugin settings so it survives a service restart.
    public static class DialState
    {
        private const String ModeSetting = "DialMode";
        private const String TelemetrySetting = "Telemetry";
        private static Plugin _plugin;

        public static Plugin Plugin => _plugin;

        public static DialMode Mode { get; private set; } = DialMode.Jog;

        public static Boolean TelemetryEnabled { get; private set; } = true;

        // Raised after Mode or TelemetryEnabled changes, so actions can refresh their images.
        public static event Action Changed;

        public static void Load(Plugin plugin)
        {
            _plugin = plugin;
            if (plugin.TryGetPluginSetting(ModeSetting, out var mode) && Enum.TryParse<DialMode>(mode, out var parsed))
            {
                Mode = parsed;
            }

            if (plugin.TryGetPluginSetting(TelemetrySetting, out var telemetry))
            {
                TelemetryEnabled = telemetry == "on";
            }
        }

        public static void SetMode(DialMode mode)
        {
            if (Mode != mode)
            {
                DialModes.Get(Mode).Reset();
            }

            Mode = mode;
            _plugin?.SetPluginSetting(ModeSetting, mode.ToString());
            PluginLog.Info($"Dial mode -> {Label(mode)}");
            Changed?.Invoke();
        }

        // Raise Changed without altering state (e.g. shuttle zone changed) so tiles can redraw.
        public static void NotifyStatus() => Changed?.Invoke();

        public static void SetTelemetry(Boolean on)
        {
            TelemetryEnabled = on;
            _plugin?.SetPluginSetting(TelemetrySetting, on ? "on" : "off");
            PluginLog.Info($"Telemetry -> {(on ? "on" : "off")}");
            Changed?.Invoke();
        }

        public static String Label(DialMode mode) => mode switch
        {
            DialMode.Jog => "JOG",
            DialMode.Shuttle => "SHTL",
            DialMode.Scroll => "SCRL",
            _ => mode.ToString(),
        };

        public static Boolean TryParse(String text, out DialMode mode)
        {
            switch (text?.ToUpperInvariant())
            {
                case "JOG": mode = DialMode.Jog; return true;
                case "SHTL": mode = DialMode.Shuttle; return true;
                case "SCRL": mode = DialMode.Scroll; return true;
                default: mode = DialMode.Jog; return false;
            }
        }
    }
}
