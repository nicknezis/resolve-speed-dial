namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;
    using System.Collections.Generic;

    // SCRL: fast relative navigation in 1-second steps (Shift+Arrow). Seconds owed accumulate as
    // diff / ScrollTicksPerSecond; a hard spin sends up to ScrollMaxBurst seconds per event.
    public sealed class ScrollMode : IDialMode
    {
        private readonly Object _lock = new();
        private Double _owedSeconds;
        private String _last = "";

        public DialMode Mode => DialMode.Scroll;

        public String Status => $"SCRL {this._last}";

        public Boolean Apply(Plugin plugin, Int32 diff, RateSample rate)
        {
            Int32 count;
            Boolean forward;
            lock (this._lock)
            {
                if (Double.IsNaN(rate.DtMs) || rate.DtMs > Tunables.StopResetMs)
                {
                    this._owedSeconds = 0;
                }

                this._owedSeconds += diff / Tunables.ScrollTicksPerSecond;
                this._owedSeconds = Math.Clamp(this._owedSeconds, -Tunables.ScrollOwedClampSeconds, Tunables.ScrollOwedClampSeconds);

                count = (Int32)Math.Abs(this._owedSeconds);
                if (count == 0)
                {
                    return false;
                }

                count = Math.Min(count, Tunables.ScrollMaxBurst);
                forward = this._owedSeconds > 0;
                this._owedSeconds -= forward ? count : -count;
            }

            var key = forward ? ResolveKeys.SecondForward : ResolveKeys.SecondBack;
            var burst = new List<ResolveKey>(count);
            for (var i = 0; i < count; i++)
            {
                burst.Add(key);
            }

            var sent = KeySender.SendBurst(plugin, burst);
            if (sent)
            {
                this._last = $"{(forward ? "+" : "-")}{count}s";
            }

            return sent;
        }

        public void Press(Plugin plugin)
        {
            KeySender.Send(plugin, ResolveKeys.Stop);
        }

        public void Reset()
        {
            lock (this._lock)
            {
                this._owedSeconds = 0;
                this._last = "";
            }
        }
    }
}
