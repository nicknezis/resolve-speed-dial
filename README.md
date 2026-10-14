# Resolve Speed Dial (prototype)

A Logi Actions SDK plugin (C#, .NET 10) that makes the MX Creative Dialpad wheel behave like the
Blackmagic Speed Editor's three dial modes in DaVinci Resolve, using Resolve's default keyboard shortcuts.
It exists to answer one question: **can the Logitech wheel feel good enough as a Speed Editor jog wheel?**

| Mode | Dial turn | Resolve keys | Dial press |
|---|---|---|---|
| **JOG** | frames = ticks / `JogTicksPerFrame`; above `JogFastTicksPerSec` the step becomes one second | `←` / `→`, `Shift+←` / `Shift+→` | `K` |
| **SHTL** | wheel *position* (clamped to ±half a turn) sets speed zone −4..+4; zone n = 2^(n−1)x; centre zone (±⅛ turn) = stop | `K` then `L`×n or `J`×n | `K` and re-centre |
| **SCRL** | seconds = ticks / `ScrollTicksPerSecond`, up to 2 per event | `Shift+←` / `Shift+→` | `K` |

Every keystroke is dropped unless Resolve is the frontmost app (checked through NSWorkspace on macOS,
the foreground window on Windows). Keys are sent from a dedicated thread,
paced ≥ 25 ms apart, with a single pending slot: a new request replaces one still waiting, so nothing
queues and the playhead stops when the hand does. Knobs live in `src/Actions/Tunables.cs`.

### What the Dialpad delivers (measured 2026-10-13)

The Plugin Service emits dial events at a steady ~45/s while the wheel moves, and `diff` per event grows
with speed: 1-3 slow, ~10 medium, 20-30 on a hard spin. Velocity is therefore `ticks/s` (diff × 45),
not events/s. The dial speed slider in Options+ scales `diff`.

## Build and dev-install

Requirements: Logi Options+ with Logi Plugin Service 6.4+, .NET 10 SDK, `dotnet tool install --global LogiPluginTool`.

```sh
cd src
dotnet build -c Debug      # writes ~/Library/Application Support/Logi/LogiPluginService/Plugins/ResolveSpeedDialPlugin.link
                           # and asks the service to reload the plugin
```

If the plugin does not show up: Options+ ▸ Settings ▸ **Restart Logi Plugin Service**.
Plugin log: `~/Library/Application Support/Logi/LogiPluginService/Logs/plugin_logs/ResolveSpeedDial.log`.

## Assign the actions in Options+ (manual, once)

The plugin is **universal**: its actions appear in every application's profile, so assign them inside
Logitech's **DaVinci Resolve** profile on each device. Each device (Keypad, Dialpad, Actions Ring) has its own
profile per application, and the service switches every device to the matching profile automatically when that
application is frontmost. (An earlier build registered its own "Resolve Speed Dial" application; if it still
shows up in Options+, delete it and re-assign in the DaVinci Resolve profile.)

1. Open Logi Options+ ▸ **MX Creative Dialpad** ▸ profile **DaVinci Resolve**.
2. Dial (big wheel) ▸ Rotate action ▸ **Resolve Speed Dial ▸ Dial ▸ "Speed Editor Dial (follows mode buttons)"**.
   The dial press becomes the mode's stop action automatically.
3. Optional: assign **Dial: JOG / SHTL / SCRL** directly to the roller or a second profile for A/B tests.
4. **MX Creative Keypad** ▸ profile **DaVinci Resolve** ▸ three keys ▸ **Resolve Speed Dial ▸ Dial ▸ Dial mode: JOG / SHTL / SCRL**,
   or one key ▸ **Cycle dial mode: …** (any pair, or all three; each press steps to the next mode);
   one key ▸ **Telemetry**. The active mode's tile is lit blue; telemetry is orange when on.
   The SDK also lists **Reset** commands for the dial action: those send K (stop) and re-centre SHTL, which is what
   the Dialpad's dial press does by default. One can go on a key as a stop button.
5. Note the Dialpad **dial speed** slider value (it scales the `diff` the plugin receives; stored as
   `Extended/ClicksMultiplier71_0` in `LoupedeckSettings.ini`).

macOS: Logi Plugin Service needs Accessibility permission to send keystrokes (already granted if
Logitech's own Resolve plugin works).

## Calibration (once)

Telemetry on, JOG mode, Resolve frontmost. Turn the wheel **exactly one full revolution, slowly**, then pause
2 s. Run `python3 tools/analyze_ticks.py`: the `sum` column of that spin is the encoder ticks per revolution.
Then set in `src/Actions/Tunables.cs`:

- `JogTicksPerFrame` = ticks/rev ÷ frames you want per revolution (Speed Editor feel: roughly one second of
  footage per revolution, i.e. ÷ 24 or ÷ 30);
- `ShuttleHalfRevTicks` = ticks/rev ÷ 2;
- `ScrollTicksPerSecond` = ticks/rev ÷ seconds you want per revolution (try ÷ 10).

Rebuild (`dotnet build -c Debug`) and the service reloads the plugin.

## Feel test script

Resolve Studio, Cut page, a timeline ≥ 10 minutes, viewer timecode visible, Resolve frontmost.

1. **Telemetry on.** For each mode: spin slow (~1 tick/s) 5 s, medium 5 s, as hard as you can 5 s, pause 2 s between.
2. Run `python3 tools/analyze_ticks.py` and record peak events/s, the `|diff|` distribution, and the sent %.
3. **JOG**: slow turn → exactly one frame per tick? Stop the hand → playhead stops within one frame?
   Hard spin → visibly faster (1-second steps kick in)?
4. **SHTL**: turn ~⅛ turn → 1x play; further → 2x, 4x, 8x; back toward the start → slows, then stops in the
   centre band; press the dial → stops and re-centres. Leaving the wheel at +2 keeps playing at 2x by design.
5. **SCRL**: hard spin → traverses the 10-minute timeline in a few seconds?
6. **Safety**: spin, Cmd-Tab to another app mid-spin → no stray keystrokes there; no stuck playback.
7. **Latency**: press a mode key → tile lights and the next tick already uses the new mode.

### Pass / fail

| Test | Pass |
|---|---|
| Telemetry | ≥ ~30 events/s on a hard spin with non-coalesced diffs (or diffs that scale with speed). If the service coalesces to a slow, rate-blind stream, velocity-based jog is impossible through this API. |
| JOG | One frame per tick at slow speed; no overrun; fast spin is noticeably faster. |
| SHTL | Stable speed per zone, centring stops, press stops, no runaway. |
| SCRL | 10-minute timeline in a few seconds. |
| Safety | Nothing lands in other apps; no stuck J/L. |
| Latency | Mode change is immediate. |

### Results

**2026-10-13, first session** (dial speed slider at default):
- Service delivers ~45 events/s while moving; `diff` per event scales with speed (1-3 / ~10 / 20-30). Telemetry gate: **pass**
  (velocity is available as ticks/s).
- Calibration: one slow revolution = **~1230 ticks** (spin 18, 175 events, 100% sent). Constants set:
  `JogTicksPerFrame` 26 (~48 frames/turn), `ShuttleHalfRevTicks` 615, `ScrollTicksPerSecond` 41 (~30 s/turn).
- SHTL (position) was unusable in the first build: ±24-tick range meant any flick hit max speed and the centre
  "stop" zone was unreachable. Rescaled to the calibrated half-turn with an eighth-turn centre band.
- First build blocked the service callback while sending bursts, which made events arrive in bunches
  (2500 ev/s spikes); fixed by moving sending to its own thread with a single pending slot.
- Feel test with calibrated constants: **JOG, SHTL, SCRL all pass** ("they all feel good"). A velocity-based
  shuttle variant (speed from turning rate, auto-stop) was tried and rejected: it stopped too early and the
  position semantics of the real SHTL were preferred. Decision: **the wheel is good enough to
  continue with the rest of the Speed Editor mapping.**

## Layout

```
src/ResolveSpeedDialPlugin.cs        plugin entry (application plugin, keystroke-only)
src/ResolveApplication.cs            links to Resolve (process "Resolve", bundle com.blackmagic-design.DaVinciResolve)
src/Actions/SpeedEditorDialAdjustment.cs   the dial action (AUTO + fixed JOG/SHTL/SCRL parameters)
src/Actions/DialModeCommand.cs       JOG / SHTL / SCRL keypad buttons
src/Actions/CycleDialModeCommand.cs  one key that steps through a chosen pair or all three modes
src/Actions/TelemetryToggleCommand.cs
src/Actions/Modes/*.cs               JogMode, ShuttleMode, ScrollMode
src/Actions/KeySender.cs             frontmost guard + pacing
src/Actions/RateMeter.cs             events/s and ticks/s estimate
src/Actions/TickTelemetry.cs         log + ticks.csv
src/Actions/Tunables.cs              all knobs
src/package/metadata/LoupedeckPackage.yaml
tools/analyze_ticks.py               summarises ticks.csv per spin
```

Packaging for distribution (not needed for the prototype): `dotnet build -c Release` then
`logiplugintool pack ./bin/Release ./ResolveSpeedDial.lplug4`.

## References

Not vendored here; read them for the background:

- [Logitech/actions-sdk](https://github.com/Logitech/actions-sdk) — official Logi Actions SDK examples (csproj and dev-install pattern).
- [benjaminmiles/logic-pro-mx-console](https://github.com/benjaminmiles/logic-pro-mx-console) — community MX Creative Console plugin for Logic Pro; source of the keystroke pacing idea.
- [smunaut/blackmagic-misc](https://github.com/smunaut/blackmagic-misc) — Speed Editor protocol reverse engineering (key codes, jog modes).
- [KipJM/blackmacro-lib](https://github.com/KipJM/blackmacro-lib) — Speed Editor emulation on a Raspberry Pi Pico; documents the device-side protocol.

Not affiliated with Blackmagic Design or Logitech.
