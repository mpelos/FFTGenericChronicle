#!/usr/bin/env python3
"""Reviewer-side (claude-opus-4-8) independent T2 build-incidence counter.

Implements the pinned contract in work/job-build-incidence-benchmark-v0.json
(doc 08, patches A-F). Intentionally a SEPARATE implementation from GPT's writer
tool so the doc-07 dual-independent acceptance gate (0 row mismatch) is meaningful.

Contract honored:
  A. coverage = marginal per dimension; presence over ALL records, incidence over
     counted records only.
  B. phase coverage scope = mandatory_phases_only (late, stress).
  C. record_statuses[status].counted is source of truth; counting.include must equal
     it -> else hard validation error.
  D. incidence denominator = counted late/stress builds; numerator per value; array
     slots dedupe per build; threshold compare on raw fraction, exclusive-gt.
  E. denominator==0 -> canonical token NO_COUNTED_BUILDS, empty tables, 0 warn/fail.
     NONE excluded; TBD in counted record's counted slot -> hard validation error.
  F. canonical output: rows sorted alpha by key; ignored grouped by exact reason;
     display percentages 6 decimals; comparison basis = raw numerator/denominator.
"""
import json
import sys
from collections import defaultdict

BUNDLE = "work/job-build-incidence-benchmark-v0.json"
COUNTED_SLOTS = None  # from bundle
NO_DATA = "NO_COUNTED_BUILDS"


def load(path):
    with open(path) as f:
        return json.load(f)


def validate(bundle):
    """Patch C + E hard-error checks. Returns list of error strings (empty = ok)."""
    errs = []
    statuses = bundle["record_statuses"]
    counted_slots = bundle["counted_slots"]
    for rec in bundle["benchmark_slots"]:
        st = rec["status"]
        if st not in statuses:
            errs.append(f"{rec['id']}: unknown status '{st}'")
            continue
        status_counted = statuses[st]["counted"]
        include = rec["counting"]["include"]
        if include != status_counted:
            errs.append(
                f"{rec['id']}: counting.include={include} != "
                f"record_statuses['{st}'].counted={status_counted} (Patch C)"
            )
        if status_counted:  # counted record -> no TBD in counted slots (Patch E)
            for slot in counted_slots:
                val = rec["slots"].get(slot)
                vals = val if isinstance(val, list) else [val]
                if "TBD" in vals:
                    errs.append(
                        f"{rec['id']}: TBD in counted slot '{slot}' of a counted "
                        f"record (Patch E)"
                    )
    return errs


def coverage(bundle):
    """Patch A/B: marginal per-dimension presence over ALL records."""
    rc = bundle["required_coverage"]
    recs = bundle["benchmark_slots"]
    present = {
        "primary_roles": {r["primary_role"] for r in recs},
        "armor_profiles": {r["coverage"]["armor_profile"] for r in recs},
        "damage_modes": {m for r in recs for m in r["coverage"]["damage_modes"]},
        "sensitivity_tags": {t for r in recs for t in r["coverage"]["sensitivity_tags"]},
        "phases": {r["phase"] for r in recs},
    }
    missing = {}
    def miss(dim, required, pool):
        gone = sorted(v for v in required if v not in pool)
        if gone:
            missing[dim] = gone
    miss("primary_roles", rc["primary_roles"], present["primary_roles"])
    miss("armor_profiles", rc["armor_profiles"], present["armor_profiles"])
    miss("physical_damage_modes", rc["physical_damage_modes"], present["damage_modes"])
    miss("nonphysical_pressure", rc["nonphysical_pressure"], present["damage_modes"])
    miss("sensitivity_tags", rc["sensitivity_tags"], present["sensitivity_tags"])
    miss("mandatory_phases", rc["mandatory_phases"], present["phases"])  # Patch B
    return missing


def incidence(bundle):
    """Patch D/E: counted late/stress builds only."""
    counted_slots = bundle["counted_slots"]
    scope_phases = set(bundle["counting_contract"]["incidence_scope"]["phases"])
    statuses = bundle["record_statuses"]
    counted = [
        r for r in bundle["benchmark_slots"]
        if statuses[r["status"]]["counted"] and r["phase"] in scope_phases
    ]
    denom = len(counted)
    tables = {slot: {} for slot in counted_slots}
    if denom == 0:
        return denom, {slot: NO_DATA for slot in counted_slots}, [], []
    for slot in counted_slots:
        counts = defaultdict(int)
        for r in counted:
            val = r["slots"].get(slot)
            vals = val if isinstance(val, list) else [val]
            seen = set()
            for v in vals:
                if v in ("NONE", "TBD") or v in seen:
                    continue
                seen.add(v)
                counts[v] += 1
        tables[slot] = {
            k: {"numerator": n, "denominator": denom,
                "pct_display": round(n / denom * 100, 6)}
            for k, n in counts.items()
        }
    # thresholds (raw fraction, exclusive-gt)
    th = bundle["thresholds"]
    th_map = {"reaction": "reaction_support_movement",
              "support": "reaction_support_movement",
              "movement": "reaction_support_movement",
              "secondary": "secondary",
              "equipment_unlocks": "equipment_unlock"}
    warns, fails = [], []
    for slot in counted_slots:
        key = th_map.get(slot)
        if key not in th:
            continue
        warn_gt = th[key]["warning_exclusive_gt"]
        fail_gt = th[key]["fail_exclusive_gt"]
        for v, cell in tables[slot].items():
            frac = cell["numerator"] / cell["denominator"]
            if frac > fail_gt:
                fails.append({"slot": slot, "value": v, "fraction": frac})
            elif frac > warn_gt:
                warns.append({"slot": slot, "value": v, "fraction": frac})
    return denom, tables, warns, fails


def ignored_by_reason(bundle):
    statuses = bundle["record_statuses"]
    groups = defaultdict(list)
    for r in bundle["benchmark_slots"]:
        if not statuses[r["status"]]["counted"]:
            groups[r["counting"]["reason"]].append(r["id"])
    return {k: sorted(v) for k, v in sorted(groups.items())}


def canonical(bundle):
    errs = validate(bundle)
    if errs:
        return {"validation_errors": sorted(errs)}
    denom, tables, warns, fails = incidence(bundle)
    # Patch F: sort rows alpha by key
    sorted_tables = {
        slot: (tables[slot] if tables[slot] == NO_DATA
               else {k: tables[slot][k] for k in sorted(tables[slot])})
        for slot in sorted(tables)
    }
    out = {
        "tool": "claude-opus-4-8/work/t2_incidence_check.py",
        "bundle": bundle["schema_version"],
        "total_counted_builds": denom,
        "ignored_records_by_reason": ignored_by_reason(bundle),
        "incidence_by_secondary": sorted_tables.get("secondary"),
        "incidence_by_reaction": sorted_tables.get("reaction"),
        "incidence_by_support": sorted_tables.get("support"),
        "incidence_by_movement": sorted_tables.get("movement"),
        "incidence_by_equipment_unlock": sorted_tables.get("equipment_unlocks"),
        "missing_required_coverage": coverage(bundle),
        "threshold_warnings": sorted(warns, key=lambda x: (x["slot"], x["value"])),
        "threshold_failures": sorted(fails, key=lambda x: (x["slot"], x["value"])),
    }
    return out


if __name__ == "__main__":
    bundle = load(sys.argv[1] if len(sys.argv) > 1 else BUNDLE)
    print(json.dumps(canonical(bundle), indent=2, sort_keys=True))
