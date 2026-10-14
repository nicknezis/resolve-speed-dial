namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;
    using System.Collections.Generic;

    // SHTL: the wheel's position sets playback speed. Ticks are integrated into a virtual angle clamped
    // to +/- ShuttleHalfRevTicks (the real Speed Editor reports +/- half a turn in shuttle mode),
    // split into ShuttleMaxZone zones per side; zone 0 = stop, zone n = L (or J for negative) pressed n times
    // after K, which Resolve interprets as 1x, 2x, 4x, 8x. The wheel stays where it is left, like the
    // real one; pressing the dial stops and re-centres.
    public sealed class ShuttleMode : IDialMode
    {
        private readonly Object _lock = new();
        private Int32 _angle;
        private Int32 _sentZone;

        public DialMode Mode => DialMode.Shuttle;

        public String Status
        {
            get
            {
                var zone = this._sentZone;
                var speed = zone == 0 ? 0 : 1 << (Math.Abs(zone) - 1);
                return zone == 0 ? "SHTL stop" : $"SHTL {(zone > 0 ? "+" : "-")}{speed}x";
            }
        }

        public Boolean Apply(Plugin plugin, Int32 diff, RateSample rate)
        {
            Int32 zone;
            lock (this._lock)
            {
                this._angle = Math.Clamp(this._angle + diff, -Tunables.ShuttleHalfRevTicks, Tunables.ShuttleHalfRevTicks);
                var zoneTicks = Math.Max(1, Tunables.ShuttleHalfRevTicks / Tunables.ShuttleMaxZone);
                zone = Math.Clamp(this._angle / zoneTicks, -Tunables.ShuttleMaxZone, Tunables.ShuttleMaxZone);
                if (zone == this._sentZone)
                {
                    return false;
                }
            }

            var burst = new List<ResolveKey> { ResolveKeys.Stop };
            var key = zone > 0 ? ResolveKeys.PlayForward : ResolveKeys.PlayReverse;
            for (var i = 0; i < Math.Abs(zone); i++)
            {
                burst.Add(key);
            }

            var sent = KeySender.SendBurst(plugin, burst);
            if (sent)
            {
                lock (this._lock)
                {
                    this._sentZone = zone;
                }

                PluginLog.Info($"shuttle zone {zone} ({this.Status})");
                DialState.NotifyStatus();
            }

            return sent;
        }

        public void Press(Plugin plugin)
        {
            if (KeySender.Send(plugin, ResolveKeys.Stop))
            {
                lock (this._lock)
                {
                    this._angle = 0;
                    this._sentZone = 0;
                }

                DialState.NotifyStatus();
            }
        }

        public void Reset()
        {
            Boolean playing;
            lock (this._lock)
            {
                playing = this._sentZone != 0;
                this._angle = 0;
                this._sentZone = 0;
            }

            if (playing)
            {
                KeySender.Send(DialState.Plugin, ResolveKeys.Stop);
            }
        }
    }
}
