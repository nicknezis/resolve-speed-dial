namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;
    using System.Globalization;
    using System.IO;

    // Records every dial event so we can measure what the Logitech wheel delivers:
    // raw diff, time since the previous event, smoothed events/s and ticks/s.
    // Written to the plugin log (Info) and to ticks.csv in the plugin data directory.
    internal static class TickTelemetry
    {
        private static readonly Object _lock = new();
        private static String _csvPath;

        public static String CsvPath => _csvPath;

        public static void Init(Plugin plugin)
        {
            try
            {
                var dir = plugin.GetPluginDataDirectory();
                Directory.CreateDirectory(dir);
                _csvPath = Path.Combine(dir, "ticks.csv");
                if (!File.Exists(_csvPath))
                {
                    File.WriteAllText(_csvPath, "elapsed_ms,mode,diff,dt_ms,events_per_s,ticks_per_s,sent\n");
                }

                PluginLog.Info($"Telemetry CSV: {_csvPath}");
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Telemetry CSV unavailable");
                _csvPath = null;
            }
        }

        public static void Record(DialMode mode, Int32 diff, RateSample s, Boolean sent)
        {
            if (!DialState.TelemetryEnabled)
            {
                return;
            }

            var inv = CultureInfo.InvariantCulture;
            var dt = Double.IsNaN(s.DtMs) ? "" : s.DtMs.ToString("F1", inv);
            PluginLog.Info($"tick mode={DialState.Label(mode)} diff={diff} dt={dt}ms ev/s={s.EventsPerSec.ToString("F1", inv)} ticks/s={s.TicksPerSec.ToString("F1", inv)} sent={sent}");

            if (_csvPath == null)
            {
                return;
            }

            var line = String.Join(",", s.ElapsedMs.ToString(inv), DialState.Label(mode), diff.ToString(inv), dt,
                s.EventsPerSec.ToString("F2", inv), s.TicksPerSec.ToString("F2", inv), sent ? "1" : "0") + "\n";
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_csvPath, line);
                }
                catch (Exception ex)
                {
                    PluginLog.Verbose(ex, "CSV append failed");
                }
            }
        }

        public static void Flush()
        {
        }
    }
}
