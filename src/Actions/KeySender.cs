namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    // Sends keystrokes to Resolve from a dedicated thread, paced and gated.
    //
    // Gate: nothing is sent unless Resolve is the frontmost application (checked when queued and again
    // when sent, via FrontmostApp), so a dial still coasting after an app switch cannot type into
    // whatever is in front.
    // Pacing: one request (a key or a short burst) at a time, PacingMs apart. There is a single pending
    // slot: a request that arrives while the sender is busy replaces whatever was waiting. Nothing is
    // ever queued up, so the playhead stops when the hand does, and the service's dial callback never
    // blocks (blocking it makes the service deliver events in bunches).
    public static class KeySender
    {
        private static readonly Object _lock = new();
        private static readonly AutoResetEvent _signal = new(false);
        private static IReadOnlyList<ResolveKey> _pending;
        private static Plugin _plugin;
        private static Thread _thread;
        private static Int64 _dropped;

        public static Int64 Dropped => Interlocked.Read(ref _dropped);

        public static void Start(Plugin plugin)
        {
            _plugin = plugin;
            if (_thread != null)
            {
                return;
            }

            _thread = new Thread(Worker) { IsBackground = true, Name = "ResolveSpeedDial.KeySender" };
            _thread.Start();
        }

        public static Boolean IsResolveActive(Plugin plugin) => plugin != null && FrontmostApp.IsResolve();

        public static Boolean Send(Plugin plugin, ResolveKey key) => SendBurst(plugin, new[] { key });

        // Queues the keys to be sent back to back as one request. Returns false (and queues nothing)
        // if Resolve is not active. Returns true if queued; a later request may still replace it.
        public static Boolean SendBurst(Plugin plugin, IReadOnlyList<ResolveKey> keys)
        {
            if (keys == null || keys.Count == 0)
            {
                return false;
            }

            if (!IsResolveActive(plugin))
            {
                PluginLog.Verbose("Resolve not frontmost - dropping keys");
                return false;
            }

            lock (_lock)
            {
                if (_pending != null)
                {
                    Interlocked.Increment(ref _dropped);
                }

                _pending = keys;
            }

            _signal.Set();
            return true;
        }

        private static void Worker()
        {
            while (true)
            {
                _signal.WaitOne();

                IReadOnlyList<ResolveKey> keys;
                lock (_lock)
                {
                    keys = _pending;
                    _pending = null;
                }

                if (keys == null || !IsResolveActive(_plugin))
                {
                    continue;
                }

                try
                {
                    for (var i = 0; i < keys.Count; i++)
                    {
                        if (i > 0)
                        {
                            Thread.Sleep(Tunables.BurstGapMs);
                        }

                        _plugin.ClientApplication.SendKeyboardShortcut(keys[i].Key, keys[i].Modifiers);
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Warning(ex, "SendKeyboardShortcut failed");
                }

                Thread.Sleep(Tunables.PacingMs);
            }
        }
    }
}
