# Vanilla (IVC) generic-job skill manifest

Extracted grounding data (working artifact) for downstream job-design analysis. Facts are pulled
from `docs/reference/fft-vanilla-command-skillset-effect-map.md`,
`docs/reference/fft-vanilla-ability-effect-index.md`, and `work/baseline_jobs.csv`. Effect
one-liners are condensed from the reference docs' effect summaries (research/design tags, not
byte-accurate formulas). "(not in sources)" marks data absent from these four sources.

## Scope & method

- **Generic playable roster = baseline_jobs.csv rows Id 74-93** (JobCommandId 5-24, InnateStatus
  None, no Float/Undead enemy flags). Each generic job has exactly **one** row — there is no
  male/female split in this table. Same-named extra rows (e.g., Knight Id 61, Black Mage Id 66) are
  undead/enemy variants; Squire Ids 1/2/4/7 are Ramza's specials. All out of scope.
- **Stats line** = multipliers (percent of base) + Move / Jump / C-Ev, exactly as requested. Growth
  values also exist in the CSV but are omitted here for compactness.
- **JobCommandId → command** mapping is inferred from the 1:1 ordering 5-24 over the canonical
  generic-job list; command identities/abilities themselves are taken from the skillset map.
- **Innate** = the InnateAbilityId columns, resolved against the Support/Movement/Reaction pools
  (Appendix A). InnateStatus is None for every generic job.
- **Per-job Reaction/Support/Movement teach-lists are NOT in these sources.** The skillset map only
  groups R/S/M into global pools (no per-job ownership), and the CSV only carries *innate* abilities.
  The full global R/S/M pools are listed once in **Appendix A**.
- Onion Knight, Dark Knight: **absent** from all sources. Arithmetician, Mime, Ramza: **present**.

---

## Squire (classic: Squire) — command: Fundaments

Stats: HP 100 / MP 75 / Speed 100 / PA 90 / MA 80; Move 4, Jump 3, C-Ev 5%
Innate: none (InnateStatus None)
Action skills:
- Focus (300 JP): boost own Physical Attack
- Rush (80 JP): physical dash damage (random)
- Throw Stone (90 JP): ranged thrown-rock damage (random)
- Salve (150 JP): clear negative statuses
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Chemist (classic: Chemist) — command: Items

Stats: HP 80 / MP 75 / Speed 100 / PA 75 / MA 80; Move 3, Jump 3, C-Ev 5%
Innate: Throw Items (Support, id 474)
Action skills:
- Attack (0 JP): basic HP damage (item-command attack entry)
- Potion (30 JP): restore small HP
- High Potion (200 JP): restore medium HP
- X-Potion (300 JP): restore large HP
- Ether (300 JP): restore MP
- High Ether (400 JP): restore more MP
- Elixir (900 JP): fully restore HP and MP
- Antidote (70 JP): cure status (Poison)
- Eye Drops (80 JP): cure status (Blind)
- Echo Herbs (120 JP): cure status (Silence)
- Maiden's Kiss (200 JP): cure status (Toad)
- Gold Needle (250 JP): cure status (Petrify)
- Holy Water (400 JP): cure status; undead interaction
- Remedy (700 JP): cure many statuses
- Phoenix Down (90 JP): revive (random; harms undead)
Reaction: (not in sources — see Appendix A)
Support: innate Throw Items; learnable teach-list (not in sources)
Movement: (not in sources — see Appendix A)

## Knight (classic: Knight) — command: Arts of War

Stats: HP 120 / MP 80 / Speed 100 / PA 120 / MA 80; Move 3, Jump 3, C-Ev 10%
Innate: none
Action skills:
- Rend Helm (300 JP): break target's headgear
- Rend Armor (400 JP): break target's body armor
- Rend Shield (300 JP): break target's shield
- Rend Weapon (400 JP): break target's weapon
- Rend MP (250 JP): reduce target MP
- Rend Speed (250 JP): reduce target Speed
- Rend Power (250 JP): reduce target Physical Attack
- Rend Magick (250 JP): reduce target Magick Attack
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Archer (classic: Archer) — command: Aim

Stats: HP 100 / MP 65 / Speed 100 / PA 110 / MA 80; Move 3, Jump 3, C-Ev 10%
Innate: none
Action skills:
- Aim +1 (100 JP): charged shot, short charge
- Aim +2 (150 JP): charged shot
- Aim +3 (200 JP): charged shot
- Aim +4 (250 JP): charged shot
- Aim +5 (300 JP): charged shot
- Aim +7 (400 JP): charged shot
- Aim +10 (700 JP): charged shot, long charge
- Aim +20 (900 JP): charged shot, longest charge
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Monk (classic: Monk) — command: Martial Arts

Stats: HP 135 / MP 80 / Speed 110 / PA 129 / MA 80; Move 3, Jump 4, C-Ev 20%
Innate: Brawler (Support, id 472)
Action skills:
- Cyclone (150 JP): physical damage, surrounding tiles
- Pummel (300 JP): physical damage (random hits)
- Aurablast (300 JP): ranged physical wave damage
- Shockwave (600 JP): ranged physical damage
- Doom Fist (300 JP): physical hit; inflicts status (Doom)
- Purification (200 JP): clear negative statuses
- Chakra (350 JP): restore HP and MP
- Revive (500 JP): revive a KO'd ally
Reaction: (not in sources — see Appendix A)
Support: innate Brawler; learnable teach-list (not in sources)
Movement: (not in sources — see Appendix A)

## Thief (classic: Thief) — command: Steal

Stats: HP 90 / MP 50 / Speed 110 / PA 100 / MA 60; Move 4, Jump 4, C-Ev 25%
Innate: none
Action skills:
- Steal Gil (10 JP): steal gil from target
- Steal Heart (150 JP): inflict Charm (recruit/control)
- Steal Helm (350 JP): steal target's headgear
- Steal Armor (450 JP): steal target's body armor
- Steal Shield (350 JP): steal target's shield
- Steal Weapon (600 JP): steal target's weapon
- Steal Accessory (500 JP): steal target's accessory
- Steal EXP (250 JP): steal target's EXP
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Orator (classic: Mediator) — command: Speechcraft

Stats: HP 80 / MP 70 / Speed 100 / PA 75 / MA 75; Move 3, Jump 3, C-Ev 5%
Innate: Beast Tongue (Support, id 473)
Action skills:
- Entice (100 JP): recruit/control target (invite)
- Stall (100 JP): reduce target Speed
- Praise (200 JP): raise target Bravery
- Intimidate (200 JP): lower target Bravery
- Preach (200 JP): raise target Faith
- Enlighten (200 JP): lower target Faith
- Condemn (500 JP): inflict death sentence (instant-KO timer)
- Defraud (100 JP): campaign/economy effect (gil)
- Insult (300 JP): inflict a status
- Mimic Darlavon (300 JP): inflict a status
Reaction: (not in sources — see Appendix A)
Support: innate Beast Tongue; learnable teach-list (not in sources)
Movement: (not in sources — see Appendix A)

## White Mage (classic: White Mage) — command: White Magicks

Stats: HP 80 / MP 120 / Speed 110 / PA 90 / MA 110; Move 3, Jump 3, C-Ev 5%
Innate: none
Action skills:
- Cure (50 JP): restore small HP (magical)
- Cura (180 JP): restore medium HP
- Curaga (450 JP): restore large HP
- Curaja (800 JP): restore massive HP
- Raise (200 JP): revive with partial HP
- Arise (600 JP): revive with full HP
- Reraise (1000 JP): grant auto-revive status
- Regen (350 JP): grant gradual HP regen
- Protect (70 JP): raise physical defense (status)
- Protectja (600 JP): party-wide Protect (AoE)
- Shell (70 JP): raise magick defense (status)
- Shellja (600 JP): party-wide Shell (AoE)
- Wall (400 JP): grant Protect + Shell
- Esuna (300 JP): cure several negative statuses
- Holy (600 JP): heavy holy magick damage
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Black Mage (classic: Black Mage) — command: Black Magicks

Stats: HP 75 / MP 120 / Speed 100 / PA 60 / MA 150; Move 3, Jump 3, C-Ev 5%
Innate: none
Action skills:
- Fire (50 JP): fire magick damage
- Fira (200 JP): fire magick damage (stronger)
- Firaga (500 JP): fire magick damage (strong)
- Firaja (900 JP): fire magick damage (strongest)
- Thunder (50 JP): lightning magick damage
- Thundara (200 JP): lightning magick damage (stronger)
- Thundaga (500 JP): lightning magick damage (strong)
- Thundaja (900 JP): lightning magick damage (strongest)
- Blizzard (50 JP): ice magick damage
- Blizzara (200 JP): ice magick damage (stronger)
- Blizzaga (500 JP): ice magick damage (strong)
- Blizzaja (900 JP): ice magick damage (strongest)
- Poison (150 JP): inflict Poison; undead interaction
- Toad (500 JP): inflict Toad transform (magical)
- Death (600 JP): instant KO (magical)
- Flare (1000 JP): heavy non-elemental magick damage
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Time Mage (classic: Time Mage) — command: Time Magicks

Stats: HP 75 / MP 120 / Speed 100 / PA 50 / MA 130; Move 3, Jump 3, C-Ev 5%
Innate: none
Action skills:
- Haste (100 JP): raise target Speed/CT
- Hasteja (600 JP): party-wide Haste (AoE)
- Slow (80 JP): lower target Speed/CT
- Slowja (600 JP): party-wide Slow (AoE)
- Stop (350 JP): inflict Stop (frozen)
- Immobilize (100 JP): prevent target movement
- Float (200 JP): grant Float (levitate)
- Reflect (300 JP): grant magic-reflect status
- Quick (900 JP): grant target an immediate turn
- Gravity (250 JP): percent-HP magick damage
- Graviga (550 JP): AoE percent-HP magick damage
- Meteor (900 JP): massive AoE magick damage
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Mystic (classic: Oracle) — command: Mystic Arts

Stats: HP 75 / MP 110 / Speed 100 / PA 50 / MA 120; Move 3, Jump 3, C-Ev 5%
Innate: none
Action skills (NOTE: most are tagged only "adds status" in sources; exact inflicted status not in sources):
- Chant (0 JP): special-case behavior (not detailed)
- Umbra (100 JP): inflict a status
- Empowerment (200 JP): drain target MP
- Invigoration (350 JP): damage target, heal self (HP drain)
- Belief (400 JP): raise target Faith
- Disbelief (400 JP): lower target Faith
- Corruption (300 JP): inflict status; undead interaction
- Quiescence (170 JP): inflict a status
- Fervor (400 JP): inflict a status
- Trepidation (200 JP): lower target Bravery
- Delirium (400 JP): inflict a status
- Harmony (800 JP): clear status(es)
- Hesitation (100 JP): inflict a status
- Repose (350 JP): inflict a status
- Induration (600 JP): inflict a status
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Summoner (classic: Summoner) — command: Summon

Stats: HP 70 / MP 125 / Speed 90 / PA 50 / MA 125; Move 3, Jump 3, C-Ev 5%
Innate: none
Action skills:
- Moogle (110 JP): AoE HP heal
- Shiva (200 JP): AoE ice magick damage
- Ramuh (200 JP): AoE lightning magick damage
- Ifrit (200 JP): AoE fire magick damage
- Titan (220 JP): AoE earth magick damage
- Golem (500 JP): ally physical-damage barrier (buff)
- Carbuncle (350 JP): grant defensive status (AoE)
- Bahamut (1000 JP): large AoE magick damage
- Odin (900 JP): large AoE magick damage
- Leviathan (860 JP): AoE magick damage
- Salamander (860 JP): AoE magick damage
- Sylph (400 JP): AoE magick damage
- Faerie (400 JP): AoE HP heal
- Lich (600 JP): magick damage with HP drain
- Cyclops (1000 JP): large AoE magick damage
- Zodiark (9999 JP): massive AoE magick damage
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Geomancer (classic: Geomancer) — command: Geomancy

Stats: HP 110 / MP 95 / Speed 100 / PA 110 / MA 105; Move 4, Jump 3, C-Ev 10%
Innate: none
Action skills (all: terrain-dependent ranged damage + adds status; flavor varies by tile):
- Sinkhole (150 JP): terrain attack; damage + status
- Torrent (150 JP): terrain attack; damage + status
- Tanglevine (150 JP): terrain attack; damage + status
- Contortion (150 JP): terrain attack; damage + status
- Tremor (150 JP): terrain attack; damage + status
- Wind Slash (150 JP): terrain attack; damage + status
- Will-o'-the-Wisp (150 JP): terrain attack; damage + status
- Quicksand (150 JP): terrain attack; damage + status
- Sandstorm (150 JP): terrain attack; damage + status
- Snowstorm (150 JP): terrain attack; damage + status
- Wind Blast (150 JP): terrain attack; damage + status
- Magma Surge (150 JP): terrain attack; damage + status
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Dragoon (classic: Lancer) — command: Jump unlocks

Stats: HP 120 / MP 50 / Speed 100 / PA 120 / MA 50; Move 3, Jump 4, C-Ev 15%
Innate: none
Action skills (JP-purchasable Jump range/height upgrades for the Jump command):
- Horizontal Jump +1 (150 JP): +1 horizontal jump range
- Horizontal Jump +2 (350 JP): +2 horizontal jump range
- Horizontal Jump +3 (550 JP): +3 horizontal jump range
- Horizontal Jump +4 (800 JP): +4 horizontal jump range
- Horizontal Jump +7 (1100 JP): +7 horizontal jump range
- Vertical Jump +/-2 (100 JP): +2 vertical jump reach
- Vertical Jump +/-3 (250 JP): +3 vertical jump reach
- Vertical Jump +/-4 (400 JP): +4 vertical jump reach
- Vertical Jump +/-5 (550 JP): +5 vertical jump reach
- Vertical Jump +/-6 (700 JP): +6 vertical jump reach
- Vertical Jump +/-7 (1000 JP): +7 vertical jump reach
- Vertical Jump +/-8 (1500 JP): +8 vertical jump reach
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Samurai (classic: Samurai) — command: Iaido

Stats: HP 75 / MP 90 / Speed 100 / PA 128 / MA 90; Move 3, Jump 3, C-Ev 20%
Innate: none
Action skills (NOTE: sources tag all uniformly "HP damage; magical; special"; per-katana effects not in sources):
- Ashura (100 JP): draw-out katana effect (magical/special)
- Kotetsu (200 JP): draw-out katana effect
- Bizen Osafune (300 JP): draw-out katana effect
- Murasame (400 JP): draw-out katana effect
- Ame-no-Murakumo (500 JP): draw-out katana effect
- Kiyomori (600 JP): draw-out katana effect
- Muramasa (700 JP): draw-out katana effect
- Kiku-ichimonji (800 JP): draw-out katana effect
- Masamune (900 JP): draw-out katana effect
- Chirijiraden (1000 JP): draw-out katana effect
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Ninja (classic: Ninja) — command: Throw

Stats: HP 70 / MP 50 / Speed 120 / PA 122 / MA 75; Move 4, Jump 4, C-Ev 30%
Innate: Dual Wield (Support, id 477)
Action skills (each unlocks throwing a weapon class for ranged physical damage):
- Throw Shuriken (50 JP): throw shuriken for damage
- Throw Daggers (100 JP): throw knives for damage
- Throw Swords (100 JP): throw swords for damage
- Throw Flails (100 JP): throw flails for damage
- Throw Katana (100 JP): throw katana for damage
- Throw Ninja Blades (100 JP): throw ninja blades for damage
- Throw Axes (120 JP): throw axes for damage
- Throw Polearms (100 JP): throw polearms for damage
- Throw Poles (100 JP): throw poles for damage
- Throw Knight's Swords (100 JP): throw knight swords for damage
- Throw Books (100 JP): throw books for damage
- Throw Bombs (70 JP): throw bombs for damage
Reaction: (not in sources — see Appendix A)
Support: innate Dual Wield; learnable teach-list (not in sources)
Movement: (not in sources — see Appendix A)

## Bard (classic: Bard) — command: Bardsong

Stats: HP 55 / MP 50 / Speed 100 / PA 30 / MA 115; Move 3, Jump 3, C-Ev 5%
Innate: none
Action skills (all mapwide/global, 100 JP each):
- Seraph Song (100 JP): global HP heal over turns
- Life's Anthem (100 JP): global HP heal
- Rousing Melody (100 JP): global Speed/stat increase
- Battle Chant (100 JP): global Bravery increase
- Magickal Refrain (100 JP): global magick stat increase
- Nameless Song (100 JP): global random ally buff
- Finale (100 JP): global instant-KO effect
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Dancer (classic: Dancer) — command: Dance

Stats: HP 60 / MP 50 / Speed 100 / PA 110 / MA 95; Move 3, Jump 3, C-Ev 5%
Innate: none
Action skills (all mapwide/global, 100 JP each):
- Witch Hunt (100 JP): global MP drain
- Mincing Minuet (100 JP): global HP damage
- Slow Dance (100 JP): global Speed/stat reduction
- Polka (100 JP): global stat reduction (PA)
- Heathen Frolic (100 JP): global stat reduction (MA)
- Forbidden Dance (100 JP): global random status
- Last Waltz (100 JP): global instant-KO effect
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Arithmetician (classic: Calculator) — command: Arithmeticks

Stats: HP 65 / MP 80 / Speed 50 / PA 50 / MA 70; Move 3, Jump 3, C-Ev 5%
Innate: none
Action skills (selectors that cast eligible magic free on matching targets; spells come from other learned skillsets):
- Target CT (250 JP): hit units whose CT matches selector
- Target Level (350 JP): hit units whose Level matches
- Target EXP (200 JP): hit units whose EXP matches
- Target Elevation (250 JP): hit units at matching height
- Prime (300 JP): selector value = prime number
- Multiple of 5 (200 JP): selector value = multiple of 5
- Multiple of 4 (400 JP): selector value = multiple of 4
- Multiple of 3 (600 JP): selector value = multiple of 3
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

## Mime (classic: Mime) — command: Mimic

Stats: HP 140 / MP 50 / Speed 120 / PA 120 / MA 115; Move 4, Jump 4, C-Ev 5%
Innate: Concentration (Support, id 469), Brawler (Support, id 472), Beastmaster (Support, id 478)
Action skills: (not in sources — Mime's command is Mimic, which copies allies' actions; no learnable ability list exists in these sources)
Reaction: (not in sources — see Appendix A)
Support: innate Concentration + Brawler + Beastmaster; learnable teach-list (not in sources)
Movement: (not in sources — see Appendix A)

---

## Ramza (special Squire) — command: Ramza Squire (+ Fundaments)

Optional entry. baseline_jobs.csv carries four Ramza-special "Squire" rows (Traitor immunity,
elevated stats vs. the generic Squire); chapter labels are not in the CSV. The skillset map's
"Ramza Squire" command bucket (ids 150-154) holds his unique skills.

Stats (the four special-Squire rows; all Move 4, Jump 3):
- Id 1 (cmd 25): HP 120 / MP 105 / Speed 100 / PA 110 / MA 100; C-Ev 10%
- Id 2 (cmd 26): HP 120 / MP 105 / Speed 100 / PA 110 / MA 100; C-Ev 10% (adds Armor/Helmet/Shield to equip list)
- Id 4 (cmd 28): HP 120 / MP 100 / Speed 100 / PA 110 / MA 100; C-Ev 5%
- Id 7 (cmd 31): HP 120 / MP 100 / Speed 100 / PA 110 / MA 100; C-Ev 15%
Innate: none (InnateStatus None; Traitor status immunity)
Action skills (Ramza Squire bucket):
- Tailwind (200 JP): raise target Speed (stat/timing)
- Steel (200 JP): raise Bravery
- Chant (0 JP): HP heal (special-case)
- Shout (500 JP): raise Bravery + stats (self-buff)
- Ultima (9999 JP): heavy non-elemental magick damage
(In vanilla Ramza also has the four Squire Fundaments — Focus, Rush, Throw Stone, Salve — via the
Squire base; see the Squire section above.)
Reaction: (not in sources — see Appendix A)
Support: (not in sources — see Appendix A)
Movement: (not in sources — see Appendix A)

---

## Appendix A — Global Reaction / Support / Movement pools

Per-job teach assignment is not in these sources; this is the full cross-job-portable pool
(from the skillset map). Use it as the R/S/M vocabulary bank.

### Reaction abilities (pool)
- Strength Surge (600 JP): on react, raise Physical Attack
- Magick Surge (500 JP): on react, raise Magick Attack
- Speed Surge (900 JP): on react, raise Speed
- Vanish (1000 JP): evade then gain Transparent
- Vigilance (200 JP): boost evasion when reacting
- Dragonheart (600 JP): chance to gain Reraise
- Regenerate (400 JP): gain Regen when hit
- Bravery Surge (700 JP): raise Bravery when hit
- Faith Surge (700 JP): raise Faith when hit
- Critical: Recover HP (500 JP): heal HP at critical
- Critical: Recover MP (400 JP): restore MP at critical
- Critical: Quick (800 JP): gain quick turn at critical
- Bonecrusher (200 JP): counter with physical damage
- Magick Counter (800 JP): counter with magick
- Counter Tackle (180 JP): counter with physical damage
- Nature's Wrath (300 JP): counter with special effect
- Absorb MP (250 JP): drain attacker MP when hit
- Gil Snapper (200 JP): gain gil when hit
- (id 440: marked-for-deletion placeholder)
- Auto-Potion (400 JP): auto-use a potion when hit
- Counter (300 JP): counter melee with an attack
- Cup of Life (200 JP): revive-related reaction
- Mana Shield (400 JP): spend MP to absorb damage
- Soulbind (300 JP): delayed-damage counter
- Parry (200 JP): raise evasion (react)
- Earplugs (300 JP): resist song/dance/speech effects
- Reflexes (400 JP): raise evasion vs attacks
- Sticky Fingers (200 JP): steal when hit
- Shirahadori (700 JP): evade physical attacks (high)
- Archer's Bane (450 JP): evade ranged attacks
- First Strike (1300 JP): strike before being hit

### Support abilities (pool)
- Equip Heavy Armor (500 JP): unlock heavy armor
- Equip Shields (250 JP): unlock shields
- Equip Swords (400 JP): unlock swords
- Equip Katana (400 JP): unlock katana
- Equip Crossbows (350 JP): unlock crossbows
- Equip Polearms (400 JP): unlock polearms
- Equip Axes (170 JP): unlock axes
- Equip Guns (800 JP): unlock guns
- Halve MP (1000 JP): halve spell MP cost
- JP Boost (250 JP): gain more JP
- EXP Boost (350 JP): gain more EXP
- Attack Boost (400 JP): raise physical damage dealt
- Defense Boost (400 JP): reduce physical damage taken
- Magick Boost (400 JP): raise magick damage dealt
- Magick Defense Boost (400 JP): reduce magick damage taken
- Concentration (400 JP): ignore enemy evasion
- Tame (500 JP): recruit/control (monsters)
- Poach (200 JP): poach defeated monsters for items
- Brawler (200 JP): boost bare-handed attack
- Beast Tongue (100 JP): communicate with/control monsters
- Throw Items (350 JP): use items at range
- Safeguard (250 JP): protect equipment from break/steal
- Doublehand (900 JP): two-hand a weapon for more damage
- Dual Wield (1000 JP): equip two weapons
- Beastmaster (200 JP): special (monster command/boost)
- Evasive Stance (50 JP): raise evasion
- Reequip (50 JP): equipment-swap support
- Swiftspell (1000 JP): reduce charge time (CT)
- (id 483: A483 placeholder)

### Movement abilities (pool)
- Movement +1 (200 JP): +1 move range
- Movement +2 (560 JP): +2 move range
- Movement +3 (1000 JP): +3 move range
- Jump +1 (200 JP): +1 jump height
- Jump +2 (500 JP): +2 jump height
- Jump +3 (600 JP): +3 jump height
- Ignore Elevation (700 JP): ignore height for movement
- Lifefont (300 JP): regen HP (movement trait)
- Manafont (350 JP): regen MP (movement trait)
- Accrue EXP (400 JP): gain EXP by moving
- Accrue JP (400 JP): gain JP by moving
- Cannot Enter Water (200 JP): special movement restriction
- Teleport (3000 JP): teleport-style movement
- Master Teleportation (0 JP): teleport anywhere
- Ignore Weather (200 JP): ignore weather effects on move
- Ignore Terrain (220 JP): ignore terrain penalties
- Waterwalking (420 JP): walk on water
- Swim (300 JP): move through deep water
- Lavawalking (150 JP): walk on lava
- Waterbreathing (200 JP): act in deep water
- Levitate (540 JP): float over terrain (status)
- Fly (900 JP): fly over terrain
- (id 508: A508 placeholder)
- Treasure Hunter (100 JP): find treasure on tiles
