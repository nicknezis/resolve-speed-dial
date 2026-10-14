namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;

    // All prototype knobs in one place. Promote to plugin settings only once the feel test passes.
    //
    // Measured on the MX Creative Dialpad (Plugin Service 6.4.2, dial speed slider at its default):
    // events arrive at a steady ~45/s while the wheel moves, and `diff` per event grows with speed
    // (1-3 slow, ~10 medium, 20-30 hard). So velocity = ticks/s, not events/s.
    internal static class Tunables
    {
        // Minimum spacing between keystrokes sent to Resolve. Resolve needs real time per key; sending
        // faster builds a backlog and the playhead keeps moving after the hand stops.
        public const Int32 PacingMs = 25;

        // Gap between keys inside one burst (e.g. shuttle "K, L, L").
        public const Int32 BurstGapMs = 12;

        // Events further apart than this start a new spin: accumulators are cleared so nothing owed
        // from the previous spin can leak into the next one.
        public const Int32 StopResetMs = 150;

        // EMA smoothing for the rate meter (0..1, higher = more reactive).
        public const Double RateSmoothing = 0.3;

        // Timeline frame rate, used to convert between frame steps and 1-second steps.
        public const Double TimelineFps = 24.0;

        // JOG: encoder ticks per one-frame step (CALIBRATE: see README), max frame keys per event,
        // ticks/s above which JOG switches from frame steps to 1-second steps, and how many frames may
        // be owed before the rest is dropped (keeps a stop a stop).
        // Calibrated 2026-10-13: one slow revolution = ~1230 ticks. 26 ticks/frame = ~48 frames per turn.
        public const Double TicksPerRevolution = 1230.0;
        public const Double JogTicksPerFrame = 26.0;
        public const Int32 JogMaxBurst = 2;
        public const Double JogFastTicksPerSec = 1000.0;
        public const Double JogOwedClampFrames = 3.0;

        // SHTL: the virtual wheel is clamped to +/- HalfRevTicks (the real Speed Editor reports half a
        // turn in shuttle mode; CALIBRATE), split into MaxZone zones per side.
        // Zone n => Resolve speed 2^(n-1)x (K, then L / J pressed n times).
        public const Int32 ShuttleHalfRevTicks = 615;
        public const Int32 ShuttleMaxZone = 4;

        // SCRL: encoder ticks per one-second step, max second keys per event, seconds that may be owed.
        public const Double ScrollTicksPerSecond = 41.0; // ~30 s of timeline per revolution
        public const Int32 ScrollMaxBurst = 2;
        public const Double ScrollOwedClampSeconds = 2.0;
    }
}
