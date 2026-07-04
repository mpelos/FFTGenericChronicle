# FFT Combat Avoidance Mechanics — Research for DCL Evade Reimplementation

Scope: how hit / miss / block / parry are computed and RESOLVED in FFT (PSX) and War of the Lions (WotL), to inform authoring outcomes in the mod. FFHacktics is behind bot protection (403 to WebFetch); verbatim quotes below are reconstructed from search-result snippets, cross-corroborated across the wiki + AeroStar guide. Byte addresses/values are the load-bearing ROM facts.

---

## 1. Evasion resolution ORDER

The engine resolves a physical attack in two big phases: (A) **reaction abilities** may pre-empt/negate the whole attack, then (B) if no reaction fired, the **four evade parameters** are each rolled in sequence and the FIRST to succeed makes the attack "miss" with that source's animation/message.

### Phase A — Reactions (evaluated before the normal evade rolls)

Activation-type reactions trigger with **chance = the reacting unit's Brave%** (Brave/100). Source: AeroStar guide; Caves of Narshe reaction list; Final Fantasy Wiki.

- **Blade Grasp (Shirahadori)** — triggers at Brave% and negates ANY physical attack (including bows/crossbows, despite its text), except attacks flagged unevadable. It pre-empts normal evasion entirely; a successful Blade Grasp is the outcome. This is why high-Brave Blade Grasp trivializes physical damage (100 Brave → ~all physical negated).
  - Source: https://finalfantasy.fandom.com/wiki/Shirahadori ; https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
- **Hamedo (First Strike)** — triggers at Brave%; on trigger the defender strikes FIRST and the incoming melee attack never lands (only melee/normal-attack range). Pre-empts the attack.
- **Arrow Guard** — Brave%; negates ranged (bow/crossbow/thrown) physical only.
- **Weapon Guard** — this is the SUPPORT ability that ENABLES weapon (parry) evade. It is NOT an activation-chance reaction; it simply makes the unit's Weapon Evade (W.Ev) count in Phase B. Without Weapon Guard, W.Ev = 0. (In WotL the equivalent gate is the class Parry ability; the top "S-EV" number is weapon parry.)
  - Source: https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876 ; https://gamefaqs.gamespot.com/boards/937312-final-fantasy-tactics-the-war-of-the-lions/59806685
- **Catch** — Brave%; catches thrown/throwable projectiles (does not use normal evade).

Key point for the mod: reaction negation is a SEPARATE gate that fires before, and independently of, the four evade rolls. Its probability source is Brave%, not the evade% stats.

### Phase B — The four evade parameters (rolled in sequence; first success wins)

Ability Base Hit byte `0x801938dc` is set to **100**, then four consecutive "Ability Evade" bytes are each rolled SEPARATELY against it, in this slot order (FFHacktics Physical Evade Calculation):

1. **Accessory Evade (A.Ev)** — from accessory/cloak. Byte `0x801938de`.
2. **Right-Hand Evade (RH.Ev)** — weapon parry OR shield, whichever is in the right hand. Byte `0x801938df`.
3. **Left-Hand Evade (LH.Ev)** — weapon parry OR shield in the left hand. Byte `0x801938e0`.
4. **Class/Physical Evade (C.Ev)** — the job's innate evade. Byte `0x801938e1`.

Each source is rolled ONE AT A TIME against base-hit 100 (snippet, verbatim: *"Each Ability Evade byte is then rolled against the Ability Base Hit"*; and *"something halving Ability Base Hit will effectively double all Evade bytes"* — i.e. blind/confuse halves base hit). The attacks that get past source N are checked against source N+1. This is a chain of independent breakpoints (multiplicative survival), NOT an additive total. The FIRST source that succeeds sets the on-screen evade TYPE/message and stops the chain.

Pre-roll modifiers to these four bytes (FFHacktics pipeline, order): Equipment Evasion Setting sets the 4 bytes + base-hit 100 → **Concentrate** (attacker) nullifies all 4 evade bytes → **Dark/Confuse** halves base hit if attacker blind/confused, and nullifies the 4 bytes if attacker is transparent → **Abandon** (defender reaction) doubles evade → **Facing** nullifies by direction (below).

- Source: https://ffhacktics.com/wiki/Physical_Evade_Calculation ; https://gamefaqs.gamespot.com/boards/937312-final-fantasy-tactics-the-war-of-the-lions/59806685 (menu adds weapon+shield evade, but "weapon evasion happens only when the shield evasion fails")

Note the older AeroStar mental model orders them as C.Ev / S.Ev / A.Ev / W.Ev; the ROM-accurate order is by equip SLOT (accessory, RH, LH, class), with shield vs weapon determined by which hand holds it. For the mod's purposes: shield-block is checked before weapon-parry when both exist (shield typically occupies one hand; weapon the other; hands are read RH then LH).

---

## 2. Final hit% formula (WotL / PSX)

Evade sources are applied MULTIPLICATIVELY, each as an independent survival factor, gated by direction (see §3). With Wp = weapon/ability base accuracy (usually 100 for normal attacks):

Front:
`hit% = Wp * (100 - C.Ev) * (100 - S.Ev) * (100 - A.Ev) * (100 - W.Ev) / 10^8`

Side:
`hit% = Wp * (100 - S.Ev) * (100 - A.Ev) * (100 - W.Ev) / 10^6`

Rear:
`hit% = Wp * (100 - A.Ev) / 100`

(Equivalently, as survival fractions: hit = Wp% × ∏(1 − Ev_i/100) over the sources active for that direction.)

Worked example (AeroStar, side attack, shield 40 + cloak 25):
`hit% = [100 * (1-.40) * (1-.25)] = [100 * .6 * .75] = 45%`

- Source: https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876 ; https://www.neoseeker.com/finalfantasytactics/faqs/26855-final-fantasy-tactics-battle-mechanics.html

Magic uses its OWN evade product — magical shield + magical accessory evade only, **no class evade** (verbatim, AeroStar):
`magic hit% = [(base hit%) * (100 - M.SEv) * (100 - M.AEv) / 10000]`

### Faith
- **Physical**: Faith does NOT enter the physical hit or physical damage formula. Physical accuracy is Wp vs the evade product above.
- **Magic**: Faith DOES factor in, as a two-stage multiplier on Faith-based spells (both accuracy and damage). Verbatim (FFHacktics Faith Calculation): *"Caster's Faith * Target's Faith / 100²"* and *"HP Damage = Damage * [Target Faith]/100 * [Attacker Faith]/100"*. So high-Faith targets are easier to hit AND take more magic; Faith 0 / Innocent status makes a unit nearly immune to Faith-based magic.
  - Source: https://ffhacktics.com/wiki/Faith_Calculation ; https://ffhacktics.com/wiki/Formulas
- **Guns** ignore evasion entirely (evade product = 1); magic-gun attacks still go through the Blade Grasp/Hamedo/Counter Magic reaction gate.

---

## 3. Direction multipliers (which sources apply)

Direction does not scale a single number by ½; it turns whole sources ON/OFF:

- **Front**: all four apply (Accessory `..de` + RH `..df` + LH `..e0` + Class `..e1`).
- **Side (flank)**: **Class Evade `0x801938e1` is nullified**. Accessory + RH + LH still apply.
- **Back (rear)**: **Class `..e1`, RH `..df`, and LH `..e0` are all nullified**. Only Accessory `0x801938de` applies.

Verbatim (FFHacktics Facing Evade Calculation): *"If attacked from the side, Class Evade is nullified (0x801938e1). If attacked from the back, Class Evade, Right and Left Hands Evade are nullified (0x801938e1, 0x801938df, 0x801938e0)."*

So the "front full / side half / back zero" intuition is really: back removes everything but the accessory; side removes only the class innate evade; front keeps all four. Direction zeroes whole source bytes — it does not scale a single evade number.

- Source: https://ffhacktics.com/wiki/Facing_Evade_Calculation ; https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876

---

## 4. Evade-TYPE / animation selection

The on-screen outcome is NOT a separate roll: it is stored as a single **Evade Type byte at battle-data offset `0x018e`**, set to WHICHEVER of the sequential sources succeeded first (Accessory → RH → LH → Class, plus the reaction/other tags). That byte then selects the animation + battle-log message.

Verbatim value table (FFHacktics "Set evade special effects based on Evade Type"):

| Value | Meaning |
|-------|---------|
| 0x00 | Hit |
| 0x01 | Accessory Evaded |
| 0x02 | Right Hand Guarded (shield/weapon in RH) |
| 0x03 | Left Hand Guarded (shield/weapon in LH) |
| 0x04 | Arrow Guard / Class Evade |
| 0x05 | Nullified |
| 0x06 | Miss (accuracy miss, no evade source) |
| 0x07 | Float+Earth / Finger Guard / Maintenance / Steal-nothing |
| 0x08 | Canceled by status |
| 0x09 | Reflected |
| 0x0a | Golem |
| 0x0b | Blade Grasp |
| 0x0d | Catch (reaction) |

So: shield block and weapon parry share the same "Hand Guarded" type (0x02 RH / 0x03 LH) — the distinction shield-vs-weapon is by which item occupies that hand, not a separate type. Class/arrow evade = 0x04. A pure accuracy failure with no evade source = 0x06 Miss. Reactions get their own types (0x0b Blade Grasp, 0x0d Catch). Glow/message mapping (verbatim): accessory + RH/LH + arrow guard → hit-glow `0x0003`; nullified/missed/caught/status-nullified → hit-glow `0x0004`.

- Source: https://ffhacktics.com/wiki/Set_evade_special_effects_based_on_Evade_Type ; https://ffhacktics.com/wiki/Set_evade_type_data_and_weapon_element_effect ; https://ffhacktics.com/wiki/Physical_Evade_Calculation

Implication for the mod: to author an outcome you (a) decide whether a reaction pre-empts (Brave gate) → set type 0x0b/0x0d etc., else (b) walk the four evade bytes in slot order; the first that "succeeds" both makes it a miss AND sets `0x018e` to that source's type. To force a specific animation, make the corresponding source the first to succeed (or zero out earlier ones / write the type byte directly).

---

## 5. "Evadeable" ability flag

Each ability/action carries a flag deciding whether it can be evaded at all. In FFHacktics references it is the **[Evadeable]** flag = bit **0x02** within **AI Behavior Flags 2**, at ability-flag **byte offset 0x05**. Verbatim (snippet): *"In AI Behavior Flags 2 at byte offset 0x05, the 0x02 flag represents 'Evadeable'"*, set via `ori r2,r2,0x0002 | Enable [Evadeable] Flag` then `sb r2,0x0002(r16)`.

- When SET: the action runs through the full Physical Evade Calculation (equipment + class evasion, direction gating, reactions).
- When CLEAR: evasion is skipped entirely (unblockable — bypasses the four evade bytes AND reactions like Blade Grasp).
- The same [Evadeable] semantics feed the AI's target scoring ("Evade Mod = 4 * Evadable abilities / Total abilities").
- The Formulas page also tags each ability's evasion domain: **PE** = physical evasion applies, **ME** = magical evasion applies, **NE** = no evasion applies.
- Source: https://ffhacktics.com/wiki/Evadeable ; https://ffhacktics.com/wiki/Ability_Data ; https://ffhacktics.com/wiki/Set_AI_Flags/Usable_Abilities ; https://ffhacktics.com/wiki/Formulas

---

## 6. Ivalice Chronicles (IVC, 2025 remaster) — specific notes (lower confidence)

- **Displayed hit% vs real roll**: The action-preview shows an accuracy % (top-left) and a separate zodiac-compatibility % with a star triangle (top-right); the displayed accuracy already bakes zodiac into it. Community testing suggests the DISPLAYED "Chance of Success" may be conservative/lower than observed outcomes for some abilities: an informal 30-trial Entice test got 43.3% actual vs ~24% displayed (only 1.75% of a 1M-trial sim did that well), hinting the display understates the true rate — but this is anecdotal, not a datamine, and unconfirmed.
  - Source: https://epyoblog.com/2025/10/19/testing-if-the-final-fantasy-tactics-chance-to-hit-predictions-are-lying/ ; https://steamcommunity.com/app/1004640/discussions/0/595162650440202184/
- **Zodiac** is auto-applied into the shown accuracy (75% = penalty, 125% = bonus).
  - Source: https://steamcommunity.com/app/1004640/discussions/0/595162650440202184/
- **Blade Grasp "nerf" claims**: Steam threads claim IVC changed Blade Grasp (e.g., no longer stopping counters/ranged), but the community reached NO consensus and no patch notes confirm it; it still appears to be Brave%-driven and still catches ranged in others' testing. Treat as unverified.
  - Source: https://steamcommunity.com/app/1004640/discussions/0/595161733884389781/
- No authoritative datamine yet shows IVC changing the core evade RESOLUTION model (order, direction gating, multiplicative product). Best current assumption: the PSX/WotL evade pipeline above still holds in IVC, with UI/zodiac presentation differences and possible balance tweaks to specific reactions.

---

## Sources
- FFHacktics — Physical Evade Calculation: https://ffhacktics.com/wiki/Physical_Evade_Calculation
- FFHacktics — Facing Evade Calculation: https://ffhacktics.com/wiki/Facing_Evade_Calculation
- FFHacktics — Faith Calculation: https://ffhacktics.com/wiki/Faith_Calculation
- FFHacktics — Evadeable flag: https://ffhacktics.com/wiki/Evadeable
- FFHacktics — Set evade special effects based on Evade Type: https://ffhacktics.com/wiki/Set_evade_special_effects_based_on_Evade_Type
- FFHacktics — Set evade type data and weapon element effect: https://ffhacktics.com/wiki/Set_evade_type_data_and_weapon_element_effect
- FFHacktics — Formulas: https://ffhacktics.com/wiki/Formulas
- FFHacktics — Ability Data (Evadeable flag): https://ffhacktics.com/wiki/Ability_Data
- FFHacktics — Set AI Flags / Usable Abilities (Evade Mod): https://ffhacktics.com/wiki/Set_AI_Flags/Usable_Abilities
- AeroStar Battle Mechanics Guide (PSX/WotL): https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876
- Neoseeker Battle Mechanics Handbook v6.1: https://www.neoseeker.com/finalfantasytactics/faqs/26855-final-fantasy-tactics-battle-mechanics.html
- GameFAQs WotL — Questions about Weapon Evasion (shield-before-weapon roll): https://gamefaqs.gamespot.com/boards/937312-final-fantasy-tactics-the-war-of-the-lions/59806685
- Final Fantasy Wiki — Evasion: https://finalfantasy.fandom.com/wiki/Evasion
- Final Fantasy Wiki — Shirahadori (Blade Grasp): https://finalfantasy.fandom.com/wiki/Shirahadori
- epyoblog — Testing IVC "Chance to Hit" predictions: https://epyoblog.com/2025/10/19/testing-if-the-final-fantasy-tactics-chance-to-hit-predictions-are-lying/
- Steam — IVC accuracy/zodiac display discussion: https://steamcommunity.com/app/1004640/discussions/0/595162650440202184/
- Steam — IVC Blade Grasp "nerf" discussion: https://steamcommunity.com/app/1004640/discussions/0/595161733884389781/
