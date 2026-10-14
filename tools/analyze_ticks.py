#!/usr/bin/env python3
"""Summarise ticks.csv written by the Resolve Speed Dial plugin's telemetry.

Usage: python3 tools/analyze_ticks.py [path/to/ticks.csv]
Default path: ~/Library/Application Support/Logi/LogiPluginService/PluginData/ResolveSpeedDial/ticks.csv

A "spin" is a run of events with gaps under 500 ms. For each spin it prints duration, event count,
mean events/s, the SUM of diffs (turn exactly one full revolution slowly to calibrate ticks per
revolution), peak smoothed ticks/s, how many events queued a keystroke, and the diff distribution.
"""
import csv
import os
import statistics
import sys
from collections import Counter

DEFAULT = os.path.expanduser(
    "~/Library/Application Support/Logi/LogiPluginService/PluginData/ResolveSpeedDial/ticks.csv")
GAP_MS = 500.0


def main(path):
    with open(path, newline="") as f:
        rows = list(csv.DictReader(f))
    if not rows:
        print("no events yet")
        return

    spins, cur = [], []
    for r in rows:
        dt = r["dt_ms"]
        if cur and (dt == "" or float(dt) > GAP_MS):
            spins.append(cur)
            cur = []
        cur.append(r)
    if cur:
        spins.append(cur)

    print(f"{len(rows)} events in {len(spins)} spins\n")
    print(f"{'#':>3} {'mode':<5} {'dur ms':>7} {'events':>6} {'ev/s':>6} {'sum':>6} {'pk t/s':>7} {'sent%':>6}  diffs")
    for i, s in enumerate(spins, 1):
        if len(s) < 2:
            continue
        dur = float(s[-1]["elapsed_ms"]) - float(s[0]["elapsed_ms"])
        dts = [float(r["dt_ms"]) for r in s if r["dt_ms"]]
        inst = [1000.0 / d for d in dts if d > 0]
        peak = max(inst) if inst else 0.0
        mean = (len(s) - 1) * 1000.0 / dur if dur > 0 else 0.0
        sent = sum(1 for r in s if r["sent"] == "1") * 100.0 / len(s)
        diffs = Counter(int(r["diff"]) for r in s)
        dist = " ".join(f"{d:+d}x{n}" for d, n in sorted(diffs.items()))
        total = sum(int(r["diff"]) for r in s)
        tps = [float(r["ticks_per_s"]) for r in s if r["ticks_per_s"]]
        peak_tps = max(tps) if tps else 0.0
        print(f"{i:>3} {s[0]['mode']:<5} {dur:>7.0f} {len(s):>6} {mean:>6.1f} {total:>+6d} {peak_tps:>7.0f} {sent:>5.0f}%  {dist}")

    all_dts = [float(r["dt_ms"]) for r in rows if r["dt_ms"] and float(r["dt_ms"]) <= GAP_MS]
    if all_dts:
        print(f"\noverall: min dt {min(all_dts):.1f} ms, median dt {statistics.median(all_dts):.1f} ms,"
              f" max rate {1000.0 / min(all_dts):.1f} ev/s")
    mags = Counter(abs(int(r['diff'])) for r in rows)
    print("|diff| distribution:", ", ".join(f"{k}: {v}" for k, v in sorted(mags.items())))


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else DEFAULT)
