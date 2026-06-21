import sqlite3, os

db = "work/override_ability.neuter.sqlite"
print("db exists:", os.path.exists(db))
con = sqlite3.connect(db)
cur = con.cursor()

cur.execute("SELECT name FROM sqlite_master WHERE type='table'")
print("tables:", [r[0] for r in cur.fetchall()])

cur.execute("PRAGMA table_info(OverrideAbilityActionData)")
cols = cur.fetchall()
colnames = [c[1] for c in cols]
print("\ncolumns (%d):" % len(colnames))
for c in cols:
    print("  %-3d %-28s %s" % (c[0], c[1], c[2]))

cur.execute("SELECT COUNT(*), MIN(Key), MAX(Key) FROM OverrideAbilityActionData")
print("\nrows/min/max Key:", cur.fetchone())

print("\nsample rows (non-zero fields only):")
for k in (1, 16, 20, 24, 30, 173, 180, 181, 182, 183, 184, 185, 186, 187, 188):
    cur.execute("SELECT * FROM OverrideAbilityActionData WHERE Key=?", (k,))
    row = cur.fetchone()
    if row:
        d = dict(zip(colnames, row))
        nz = ", ".join("%s=%s" % (kk, vv) for kk, vv in d.items() if vv not in (0, None, ""))
        print("  Key=%-4d %s" % (k, nz))
    else:
        print("  Key=%-4d <absent>" % k)

con.close()
