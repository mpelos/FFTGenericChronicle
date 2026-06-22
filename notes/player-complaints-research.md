# Player Complaints: FFT Equipment & Combat Mechanics (web research)

Research scratch (not a design doc). Collected 2026-06-20 to ground Generic Chronicle's
formula-balance work in what players actually criticize. Sources at the bottom. Treat community
tier lists as opinion, not verified IVC data; they still map the perceived problem space well.

## A. Weapon-family imbalance (directly confirms the mod's premise)

Community weapon tier list (Game8, IVC):

- S: **Knight's Sword** - "significantly higher damage" + frequent special effects; called the
  best weapon overall. **Gun** - longest flat range, 100% accuracy, weather-immune, high power.
- A: **Sword** - accessible/versatile but "becomes obsolete when Knight's Swords become
  available." **Rod/Staff** - valued for spell *effects/potency*, not damage. **Polearm** -
  Dragoon-only niche (range 2 + Jump).
- B (niche): **Bow** - weather hurts accuracy. **Ninja Blade** - "incredibly niche", Ninja-only,
  expensive. **Katana** - Samurai-only.
- C (weak/situational): **Pole, Crossbow** - minimal damage, few jobs. **Dagger/Knife, Bag,
  Book** - outclassed. **Axe/Flail** - unreliable random damage. **Cloth/Instrument** - locked to
  underperforming jobs (Dancer/Bard).

Takeaway: exactly the mod's target. The failure modes that push a family down-tier:
1. **Raw outclassing** - knife/crossbow/pole/book just do less than a sword for the same slot.
2. **Random unreliability** - axe/flail/bag are "unreliable trash" despite high ceiling.
3. **Job-lock niche** - ninja blade/katana/cloth/instrument tied to one (often weak) job.
4. **Sword/knight-sword convergence** - the universal best physical answer, swallowing the rest.

## B. Dominance engines (the break combos we MUST balance around)

These are why "a balanced base attack" is not enough - support/equipment multipliers collapse the game:

- **Two Hands + Knight Sword (e.g. Excalibur)** - huge single hits.
- **Two Swords / Dual Wield** - doubles attacks; turns high-WP or on-hit weapons dominant.
- **Brave manipulation** - every 4 pts shifted = 1 permanent; push Brave ~97 + **Blade Grasp**
  (reaction, Brave-scaled) = near-immune to physical. Add Two Swords + Excalibur = one-shots most things.
- **Faith manipulation** - push Faith ~3 = shrug off magic.
- **Calculator / Arithmetician** - cast spells with no charge time / no MP on units matching
  CT/Level/Prime/etc.; widely cited as the single most broken thing in FFT.
- **Accumulate/Yell** snowball; **Monk + Martial Arts (fists)** strong with little investment.

Implication for us: the dominance-risk section in 03 (Two Hands/Two Swords/Brave) is confirmed as
first-order. Any per-family balance must be checked against these multipliers, not in isolation.

## C. Magic vs physical imbalance (the coexistence problem in 01 is real)

- **Charge time** - mages stand still and vulnerable; the slowest spells can let the mage's turn
  come up again before the spell fires (2 turns for 1 action).
- **Speed/turn order** hobbles casters generally.
- **Faith is double-edged** - raises magic dealt AND magic taken; **Brave only ups physical dealt
  with no downside**. So physical is strictly safer - a core asymmetry.
- **Silence** disables magic but not physical (extra disable vector only casters suffer).
- Instant, guaranteed physical (archers, chemists, guns) is strategically superior in clutch moments.

Net: outside the broken Calculator route, magic underperforms physical. Our magic/weapon
coexistence metric is a real, classic problem - not theoretical.

## D. Opaque "feel" mechanics (legibility cost - validates 02 design-role)

- **Zodiac compatibility** - hidden, unintuitive; swing is large (x0.5 bad to x1.5 good) yet never
  surfaced; players report not noticing it for decades. Confirms our `avoid-for-legibility` label.
- **Brave/Faith permanent drift** exploit - a hidden optimization most players never learn.

## E. Progression/accessibility (out of formula scope, but context)

- Missable **rare treasure tiles** (common vs rare, one pick per tile), hidden gear in Midlight's
  Deep. Equipment relevance is also gated by access, not only by formulas.

## F. IVC-specific signals (what SE itself targeted)

- IVC patches did rebalancing: shortened charge/cast times, Squire-mode difficulty tuning,
  Chemist gets Treasure Hunter innate. SE itself treated **charge time** as a pain point - aligns
  with C above.

## How this feeds Generic Chronicle

- Validates the premise (sword/knight-sword + gun dominance; most families situational).
- Sharpens family-identity targets: fix the 4 failure modes in A (outclassing, unreliable random,
  job-lock niche, convergence) - not just "different numbers".
- Confirms accuracy as a real identity axis (gun 100%/weather-immune vs bow weather penalty) ->
  supports the Accuracy/Hit Rate domain (M-acc) in 02.
- Confirms dominance-multiplier review (03) and magic/physical coexistence (01) as first-order.
- Confirms legibility guard (02): keep opaque levers (Zodiac, raw stats) out of visible identity.

## Sources

- Game8 - Best Weapons Tier List: https://game8.co/games/Final-Fantasy-Tactics/archives/555215
- TV Tropes - GameBreaker / FFT (broken combos): https://tvtropes.org/pmwiki/pmwiki.php/GameBreaker/FinalFantasyTactics
- NeoGAF - "How do I utterly destroy FFT's balance": https://www.neogaf.com/threads/how-do-i-utterly-destroy-final-fantasy-tactics-balance.563745/
- GameFAQs - "What things are considered overpowered/broken?": https://gamefaqs.gamespot.com/boards/197339-final-fantasy-tactics/68311513
- Thinking Inside the Box - "FFT's Mages" (magic problems): https://wwwthinkinginsidethebox.blogspot.com/2022/02/final-fantasy-tacticss-mages.html
- TheGamer - Zodiac Compatibility guide: https://www.thegamer.com/final-fantasy-tactics-zodiac-compatibility-guide/
- Steam - IVC Zodiac compatibility discussions: https://steamcommunity.com/app/1004640/discussions/0/595161733884100115/
- TwistedVoxel - IVC 1.3.0 balance update: https://twistedvoxel.com/final-fantasy-tactics-the-ivalice-chronicles-update-1-3-0-adjusts-game-balance-and-fixes-minor-issues/
- FF Wiki - FFT weapons: https://finalfantasy.fandom.com/wiki/Final_Fantasy_Tactics_weapons
