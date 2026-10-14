namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;
    using System.Diagnostics;

    public readonly record struct RateSample(Double DtMs, Double EventsPerSec, Double TicksPerSec, Int64 ElapsedMs);

    // Estimates how fast the dial is being turned from the stream of ApplyAdjustment calls.
    internal sealed class RateMeter
    {
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private Double _lastMs = Double.NaN;
        private Double _emaEvents;
        private Double _emaTicks;

        public RateSample Observe(Int32 diff)
        {
            var nowMs = this._clock.Elapsed.TotalMilliseconds;
            var dt = Double.IsNaN(this._lastMs) ? Double.NaN : nowMs - this._lastMs;
            this._lastMs = nowMs;

            if (Double.IsNaN(dt) || dt > Tunables.StopResetMs || dt <= 0)
            {
                // New spin: no usable rate yet.
                this._emaEvents = 0;
                this._emaTicks = 0;
                return new RateSample(dt, 0, 0, (Int64)nowMs);
            }

            var instEvents = 1000.0 / dt;
            var instTicks = Math.Abs(diff) * 1000.0 / dt;
            var a = Tunables.RateSmoothing;
            this._emaEvents = this._emaEvents == 0 ? instEvents : a * instEvents + (1 - a) * this._emaEvents;
            this._emaTicks = this._emaTicks == 0 ? instTicks : a * instTicks + (1 - a) * this._emaTicks;
            return new RateSample(dt, this._emaEvents, this._emaTicks, (Int64)nowMs);
        }
    }
}
