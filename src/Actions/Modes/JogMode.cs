namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;
    using System.Collections.Generic;

    // JOG: relative, frame-accurate. Frames owed accumulate as diff / JogTicksPerFrame, so a slow turn
    // gives single frames and a medium turn several frames per event (sent as a short burst). Above
    // JogFastTicksPerSec the step becomes one second (Shift+Arrow), the only coarser playhead step
    // Resolve offers. Owed frames are clamped and cleared on a pause, so a stop is a stop.
    public sealed class JogMode : IDialMode
    {
        private readonly Object _lock = new();
        private Double _owedFrames;
        private String _last = "";

        public DialMode Mode => DialMode.Jog;

        public String Status => $"JOG {this._last}";

        public Boolean Apply(Plugin plugin, Int32 diff, RateSample rate)
        {
            var fast = rate.TicksPerSec > Tunables.JogFastTicksPerSec;
            Int32 count;
            Boolean forward;
            lock (this._lock)
            {
                if (Double.IsNaN(rate.DtMs) || rate.DtMs > Tunables.StopResetMs)
                {
                    this._owedFrames = 0;
                }

                this._owedFrames += diff / Tunables.JogTicksPerFrame;
                var clamp = fast ? Tunables.TimelineFps * 1.5 : Tunables.JogOwedClampFrames;
                this._owedFrames = Math.Clamp(this._owedFrames, -clamp, clamp);

                var unit = fast ? Tunables.TimelineFps : 1.0;
                count = (Int32)(Math.Abs(this._owedFrames) / unit);
                if (count == 0)
                {
                    return false;
                }

                count = Math.Min(count, fast ? 1 : Tunables.JogMaxBurst);
                forward = this._owedFrames > 0;
                this._owedFrames -= (forward ? count : -count) * unit;
            }

            var key = forward
                ? (fast ? ResolveKeys.SecondForward : ResolveKeys.FrameForward)
                : (fast ? ResolveKeys.SecondBack : ResolveKeys.FrameBack);

            var burst = new List<ResolveKey>(count);
            for (var i = 0; i < count; i++)
            {
                burst.Add(key);
            }

            var sent = KeySender.SendBurst(plugin, burst);
            if (sent)
            {
                this._last = $"{(forward ? "+" : "-")}{count}{(fast ? "s" : "f")}";
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
                this._owedFrames = 0;
                this._last = "";
            }
        }
    }
}
