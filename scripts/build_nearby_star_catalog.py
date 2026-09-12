#!/usr/bin/env python3
"""Build the pinned, deterministic nearest-500 HYG system catalog."""
import argparse, csv, hashlib, json, math
from collections import defaultdict

SOURCE_URL = "https://raw.githubusercontent.com/astronexus/HYG-Database/c7f7f883fe678cc7680169a50ccd7dcc49b060ce/hyg/CURRENT/hygdata_v41.csv"
LY_PER_PC = 3.26156

def text(row, key): return (row.get(key) or "").strip()
def number(row, key):
    try: return float(text(row, key))
    except ValueError: return None
def name_for(row):
    proper = text(row, "proper")
    if proper: return proper, "proper"
    bf = text(row, "bf")
    if bf: return bf, "catalogue"
    gl = text(row, "gl")
    if gl: return gl, "catalogue"
    hip = text(row, "hip")
    if hip: return "HIP " + hip, "catalogue"
    hd = text(row, "hd")
    if hd: return "HD " + hd, "catalogue"
    return "HYG " + text(row, "id"), "catalogue"

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--source", required=True)
    ap.add_argument("--output", default="data/astronomy/hyg-nearby-500-v1.json")
    ap.add_argument("--verify", action="store_true")
    args = ap.parse_args()
    raw = open(args.source, "rb").read()
    sha = hashlib.sha256(raw).hexdigest()
    rows = list(csv.DictReader(raw.decode("utf-8-sig").splitlines()))
    valid = []
    for row in rows:
        try: hid = int(text(row, "id")); primary = int(text(row, "comp_primary"))
        except ValueError: continue
        dist, x, y, z = (number(row, k) for k in ("dist", "x", "y", "z"))
        if dist is None or not math.isfinite(dist) or dist < 0 or dist >= 100000: continue
        if any(v is None or not math.isfinite(v) for v in (x, y, z)): continue
        valid.append((hid, primary, row, dist, x, y, z))
    groups = defaultdict(list)
    for item in valid: groups[item[1]].append(item)
    systems = []
    for primary, members in groups.items():
        p = next((m for m in members if m[0] == primary), min(members, key=lambda m:m[0]))
        _, _, row, dist, x, y, z = p
        name, kind = name_for(row)
        components = []
        for hid, _, cr, *_ in sorted(members, key=lambda m:m[0]):
            cn, _ = name_for(cr)
            components.append({"name": cn, "spectralType": text(cr, "spect"), "hygId": hid})
        systems.append({"hygId": primary, "name": name, "nameKind": kind,
                        "distanceParsecs": dist, "xLightYears": x*LY_PER_PC,
                        "yLightYears": y*LY_PER_PC, "zLightYears": z*LY_PER_PC,
                        "spectralType": text(row, "spect"), "components": components})
    systems.sort(key=lambda s:(s["distanceParsecs"], s["hygId"]))
    systems = systems[:500]
    used = defaultdict(int)
    for s in systems:
        used[s["name"]] += 1
        if used[s["name"]] > 1: s["name"] += " [HYG %d]" % s["hygId"]
    out = {"catalogVersion":"hyg-nearby-500-v1", "sourceUrl":SOURCE_URL,
           "sourceSha256":sha, "epoch":"J2000", "systems":systems}
    encoded = json.dumps(out, ensure_ascii=False, indent=2, sort_keys=False) + "\n"
    if args.verify:
        assert len(systems)==500 and len({s['hygId'] for s in systems})==500
        assert all(math.isfinite(s['distanceParsecs']) and s['distanceParsecs'] >= 0 for s in systems)
    import os
    os.makedirs(os.path.dirname(args.output) or ".", exist_ok=True)
    open(args.output, "w", encoding="utf-8", newline="\n").write(encoded)

if __name__ == "__main__": main()
