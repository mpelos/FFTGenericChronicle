# End-Game Meta — Generics vs. Special Characters (Vanilla / IVC)

This is the central balance fact the per-job analyses (`01-squire.md` … `21-ramza.md`) circle around:
by the late game, a vanilla party converges on **special story characters plus a handful of generic
specialists**, and most generic *commands* quietly stop being worth using. This document states which
jobs survive, which characters are always fielded, and — the part that matters most — **why the
generics get replaced.** Closing that gap is the design problem a generic-focused rebalance exists to
solve.

A caveat up front: uniques are *not strictly required* — a disciplined player can finish with an
all-generic team. The claim here is the meta one: the specials are so much stronger **for free** that
the rational party leans on them, and the generic commands they outclass fall out of use.

## The shape of an end-game party

FFT fields ~5 units per battle out of a larger roster. A typical optimized late party is:

- **Ramza** (forced, and genuinely strong — see below);
- **one to three premier special characters** (Orlandeau above all);
- **one or two generic specialists** whose *mechanic* no unique can replicate (a Ninja striker body, an
  Arithmetician);
- everyone, unique or generic, running a small set of **exported generic support abilities**
  (Dual Wield, Concentration, Auto-Potion, Short Charge…).

Crucially, the "generic jobs" still on the field at end-game are often staffed by **unique characters**
(who out-stat generics on the same job). So even where a generic *job* survives, the generic *unit*
frequently does not.

## Generic jobs that still see end-game use

### As a primary body / for the command itself
- **Ninja** — the default striker chassis: highest Speed + innate Dual Wield (two hits, acts first).
- **Arithmetician (Arithmeticks / Math Skill)** — the broken standout: any learned spell cast
  **instant, MP-free, map-wide**. The body is slow and fragile, so the *command* is the prize — and no
  unique character has it.
- **Monk** — the most self-sufficient body: ranged AoE, Chakra self-heal, revive, elite reactions.
- **Mime** — free copy-turns that scale with ally count. *(Tier is genuinely contested: casual/IVC play
  rates it low; hardcore PSX play rates it top.)*
- **Samurai** — Iaido is excellent, but usually run as a **secondary** command on a sturdier body, not
  as a Samurai primary.

### Only as exported abilities (grind the job, equip the skill on a better unit)
This is how most generics "stay relevant": you mine one ability and bench the job.
- **Dual Wield** (Ninja) — the most-exported physical support.
- **Concentration** (Archer, ignore evasion), **Brawler / Attack Boost** (Monk/Geomancer).
- **Blade Grasp / Shirahadori** and **Hamedo / First Strike** (Monk) — Brave-based negation of physical
  attacks; the best defensive reactions.
- **Short Charge / Swiftspell**, **Halve MP**, **Teleport** (Time Mage); **Magick Boost** (Black Mage).
- **Auto-Potion** (Chemist), **Move +3** (Bard), **JP Boost** (Squire), **Equip-[weapon]** passives
  (e.g. Equip Sword to put a great sword on a Ninja).

### Commands that essentially disappear at end-game
- **Bard** and **Dancer** (slow, gender-locked; kept only as hosts for Move +3 / map buffs),
  **Orator** (kept for Equip Gun + recruiting), **base Squire** (a throwaway valued for JP Boost and as
  Ramza's chassis), and **Archer / Thief / Chemist as damage bodies** (their *exports* live on; the jobs
  as fielded fighters do not).

## Special characters that are (near-)always used

Names below are the IVC job labels, confirmed against `work/baseline_jobs.csv`; the human story uniques
are marked in the data by **Traitor-status immunity** (every generic has none).

| Character | IVC job | Unique command (one line) | Why fielded |
|---|---|---|---|
| **Orlandeau** (Cidolfas / "T.G. Cid") | **Sword Saint** | All sword skills (Holy + Dark + Crush) — ranged, instant, PA-based, +status | **Consensus #1, universal.** Trivializes the end-game; can solo maps. Elite stats (HP160/PA122/MA122), lore Auto-Haste. |
| **Ramza** | Squire (unique, "Mettle") | Shout, Steel, Tailwind, Chant, Ultima | **Universal** (forced + excellent). Shout snowballs his own Brave/PA/MA/Speed; Traitor-immune. |
| **Agrias** | Holy Knight | Holy Sword: Judgment Blade, Hallowed Bolt, Divine Ruination | "Early Orlandeau" — ranged instant PA-based AoE; strong the moment she joins. |
| **Reis** | Dragonkin | Dragon breaths + Dragon's Charm/Gift/Might/Speed | Sleeper-S: **best growth in the game** + innate Dual Wield; repays setup. |
| **Beowulf** | Templar | Magic Sword: instant, Faith-ignoring Sleep/Silence/Confuse/Break/Petrify/Death | The premier disabler — reliable status that generic Mystics can't land. |
| **Mustadio** | Machinist | Snipe: Leg/Arm Shot (immobilize/disable), Seal Evil — ranged CC, guns | Strong early-A ranged control; falls off late. |
| **Meliadoul** | Divine Knight | Crush: Helm/Armor/Weapon/Accessory — ranged equipment-break + damage | Niche anti-equipment specialist; dead vs monsters/unarmed. |
| **Worker 8 / Construct 8** | Automaton | Work/Destroy physical actions | True damage + Faith/status immunity + elemental nullification; no growth, can't be magic-healed. |
| **Byblos** | Byblos | Unique heal/status/damage command | Versatile utility (innate Poach/Counter, high evade); recruited very late. |
| **Cloud** | Soldier | Limit: Braver … Omnislash (huge charge times, needs Materia Blade) | **Version-split:** a textbook trap in PSX/WotL; **rebuilt to A-tier in IVC** (joins at level, faster Limits). |
| **Rapha / Marach** | Skyseer / Netherseer | Sky/Nether "Truth/Untruth" random-target MA AoE | **Overrated:** random targeting is unreliable; Marach is the consensus worst recruit ("little more than a generic"). |

Guests and limited units, for completeness: **Gaffgarion** (Fell Knight / Dark Sword — guest, then
leaves), **Ovelia** (Princess — guest), **Alma** (Cleric — barely playable in vanilla). **Balthier and
Luso do not exist in IVC** — they were WotL-only and are cut from this version; any tier list that
includes them is a WotL carryover error.

## Why generics get replaced — the core analysis

**1. Unique commands bundle every advantage at once (the decisive reason).** A Holy / Dark / Divine /
Magic Sword skill is, in a *single action*: **ranged** (skips the melee-approach problem),
**instant — no charge, no MP** (unlike interruptible generic spells), **PA-and-weapon scaled, not
Faith-scaled** (reliable even against low-Faith enemies, exactly where generic mages collapse),
**near-unevadable**, **status-inflicting**, and often **equipment-breaking**. No generic command offers
more than two of these traits; the unique offers all of them. Orlandeau's Night Sword alone is a
ranged, instant, self-healing HP+MP drain he can repeat every turn — nothing in the generic catalog
competes.

**2. Superior stats and growth.** The best uniques have the best growth curves in the game (Reis #1,
Orlandeau #2), so on an *identical* job a unique out-stats a generic over time. The repo data
corroborates the headline numbers (Sword Saint HP 160 / PA 122 / MA 122; Dragonkin the top growth). The
lone exception proves the rule: Cloud's Soldier job is genuinely weak, which is why pre-IVC Cloud is a
trap.

**3. They arrive pre-built.** A unique joins combat-ready — job already leveled, signature abilities
already set (Agrias as a Holy Knight with Move +1; Meliadoul with Attack Boost + Counter). A generic
starts from zero and needs FFT's notorious multi-job JP grind to reach a fraction of that. The unique's
power is *free*; the generic's must be *earned*.

**4. Innate passives and immunities.** Orlandeau's Auto-Haste (acts far more often), Worker 8's
Faith-and-status immunity plus elemental nullification, Beowulf's Faith-ignoring debuffs — generics buy
none of this innately.

### Why a few generics survive anyway
The survivors are explained by three forces, not by parity:
- **Non-replicable niches.** Math Skill (Arithmetician), Mime copy-turns, gender-locked Bard/Dancer map
  buffs (which matter when your best uniques are the wrong gender), the Ninja dual-wield striker body,
  and monster utility (Worker/Byblos immunities, Poach farming) — things no unique command provides.
- **Opportunity cost.** You don't spend a unique's signature kit on a command-agnostic utility role, so
  a generic fills the body/utility slots the stars don't want.
- **Body count.** ~5 units per battle and a large roster mean generics staff most of the campaign — but
  note again: uniques *can* switch into generic jobs, so even the surviving generic jobs are often
  fielded on unique characters, not generic ones.

## What IVC changes (and doesn't)

IVC is the baseline this project sits on, so its tuning matters. *(These rebalance specifics are
community-reported and not yet verified against the Windows build — treat the exact numbers as Strong,
not Proven.)*

- **Difficulty modes** (Squire / Knight / **Tactician**): Tactician gives smarter, faster, better-geared
  AI plus a blanket enemy damage-resistance, and a later patch hardened the AI further.
- **Charge/cast times cut ~25% and JP gain up ~30%** — mildly helps generics and chargers catch up.
- **Teleport heavily nerfed** (JP cost ~650 → ~3000); **Chemist gains innate Treasure Hunter**; **Archer
  (Aim) and Monk buffed**; **Cloud rebuilt** to A-tier.
- **Arithmetician** is only *partially* reined in — a Tactician-only damage filter; on Squire/Knight
  difficulty it stays instant, free, and busted.
- **The gap persists where it counts: Orlandeau is not meaningfully nerfed** (the developers stated
  strong characters were left strong, with difficulty added through modes instead). The weaker uniques
  got some pre-learned skills; Orlandeau got none — and none of the generic-vs-special structural
  reasons above were addressed.

## Conclusion

In vanilla and in IVC, the end-game converges on **special characters + Ramza + one or two
non-replicable generic specialists (Ninja, Arithmetician) + a thin layer of exported generic supports.**
Most generic *commands* — and nearly all generic *units* — drop out, not because they can't win, but
because uniques deliver more, more reliably, for free. The generic catalog survives mostly as a parts
bin (Dual Wield, Concentration, Auto-Potion…) and a body count, not as a set of identities worth
fielding on their own merits. That is precisely the gap a generic-first rebalance has to close.
