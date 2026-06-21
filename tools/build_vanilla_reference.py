#!/usr/bin/env python3
"""Build vanilla FFT ability/status reference docs for Generic Chronicle."""
from __future__ import annotations

import csv
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
WORK = ROOT / "work"
DOCS = ROOT / "docs" / "reference"


GROUPS = [
    (1, 15, "White Magicks", "White Mage"),
    (16, 31, "Black Magicks", "Black Mage"),
    (32, 44, "Time Magicks", "Time Mage"),
    (45, 59, "Mystic Arts", "Mystic"),
    (60, 75, "Summon", "Summoner"),
    (76, 85, "Iaido", "Samurai"),
    (86, 92, "Bardsong", "Bard"),
    (93, 99, "Dance", "Dancer"),
    (100, 107, "Martial Arts", "Monk"),
    (108, 115, "Steal", "Thief"),
    (116, 125, "Speechcraft", "Orator"),
    (126, 137, "Geomancy", "Geomancer"),
    (138, 145, "Arts of War", "Knight"),
    (146, 149, "Fundaments", "Squire"),
    (150, 154, "Ramza Squire", "Ramza"),
    (155, 166, "Holy/Dark sword skills", "Unique sword jobs"),
    (167, 180, "Unique magicks", "Unique jobs"),
    (181, 211, "Enemy/unique statuses and Bio", "Enemy/unique"),
    (212, 230, "Boss/guest/unique actions", "Boss/guest/unique"),
    (231, 247, "Templar/status actions", "Templar/unique"),
    (248, 255, "Dragonkin", "Dragonkin"),
    (256, 264, "Limit", "Cloud"),
    (265, 355, "Monster actions", "Monsters"),
    (367, 381, "Items", "Chemist"),
    (382, 393, "Throw", "Ninja"),
    (394, 405, "Jump unlocks", "Dragoon"),
    (406, 413, "Aim", "Archer"),
    (414, 421, "Arithmeticks selectors", "Arithmetician"),
    (422, 453, "Reaction abilities", "Reaction"),
    (454, 483, "Support abilities", "Support"),
    (486, 509, "Movement abilities", "Movement"),
]


TAG_DESCRIPTIONS = {
    "accuracy": "accuracy/evasion bypass",
    "ally_buff": "ally buff",
    "aoe": "area effect",
    "arithmeticks_selector": "arithmeticks selector",
    "brave_down": "Bravery reduction",
    "brave_up": "Bravery increase",
    "caster_support": "caster support",
    "ct_action": "CT/action timing",
    "damage": "HP damage",
    "damage_boost": "damage boost",
    "defense": "defense/evasion",
    "drain": "drain",
    "economy": "campaign/economy",
    "elemental": "elemental",
    "equipment_break": "equipment break/control",
    "equipment_unlock": "equipment unlock",
    "faith_down": "Faith reduction",
    "faith_up": "Faith increase",
    "global": "global/mapwide",
    "healing": "HP healing",
    "instant_ko": "instant KO/death",
    "jp_exp": "JP/EXP economy",
    "jump": "jump/air timing",
    "magical": "magical",
    "movement": "movement/terrain",
    "mp": "MP effect",
    "physical": "physical",
    "reaction": "reaction",
    "recruit": "recruit/control",
    "revive": "revive/reraise",
    "random": "random effect",
    "special": "special-case behavior",
    "stat_down": "stat reduction",
    "stat_up": "stat increase",
    "status_add": "adds status",
    "status_clear": "clears status",
    "steal": "steal/plunder",
    "support": "support slot",
    "terrain": "terrain-dependent",
    "throw": "throw weapon/item",
    "timing": "timing/speed",
    "undead": "undead interaction",
    "unique": "unique/boss",
    "local_placeholder": "local placeholder/unknown record",
}


STATUS_EFFECTS = {
    "Atheist": ("other", "Faith multiplier effectively collapses; many faith-based effects fail or shrink.", "magic reliability, status immunity, Faith economy", "T3/T4"),
    "Berserk": ("control", "Unit is forced into basic attacks and loses normal player control.", "anti-caster control, forced offense", "T2/T4/T5"),
    "Blind": ("debuff", "Physical accuracy is heavily penalized through doubled target evasion-style behavior.", "accuracy pressure, anti-martial utility", "T4"),
    "Charging": ("action state", "Unit is preparing a delayed action; evasion is suspended and incoming physical pressure is boosted.", "delay risk, Aim/spell counterplay", "T4/T5"),
    "Charm": ("control", "Unit acts under enemy control until disrupted.", "hard control, damage-to-break counterplay", "T2/T5"),
    "Chest": ("defeated state", "Treasure state after a unit leaves combat.", "campaign loot only", "none"),
    "Chicken": ("control/stat", "Very low Brave state; unit flees and gradually restores Brave.", "Brave pressure, anti-Brave builds", "T2/T4"),
    "Confuse": ("control", "Unit takes random actions; reactions and some move effects are suppressed.", "soft control, disruption", "T2/T5"),
    "Critical": ("hp state", "Low HP state that can trigger critical reactions.", "reaction gating, comeback hooks", "T3/T5"),
    "Crystal": ("defeated state", "Post-KO crystal state used for loot/ability inheritance.", "campaign inheritance only", "none"),
    "Defending": ("action state", "Defensive stance, broadly doubling evasion until the next turn.", "defense stance, guard action", "T4/T5"),
    "Disable": ("debuff", "Unit cannot act, evade, or use reactions but can still move.", "action denial without full immobilization", "T4/T5"),
    "Doom": ("debuff", "Turn countdown toward KO unless removed or immunity intervenes.", "delayed lethal pressure", "T3/T5"),
    "Faith": ("other", "Unit becomes highly receptive to faith-based magic and status effects.", "magic vulnerability and potency", "T3/T4"),
    "Float": ("buff/movement", "Unit floats above terrain, ignores some terrain restrictions, and avoids earth-elemental effects.", "terrain/elevation utility", "T4/T5"),
    "Haste": ("buff/timing", "Unit gains CT faster and takes more turns over time.", "tempo, action economy", "T5"),
    "Immobilize": ("debuff", "Unit cannot move but can still act.", "position lock, ranged counterplay", "T5"),
    "Invisible": ("buff/targeting", "Unit is ignored by AI and bypasses evasion while attacking until action or damage breaks it.", "targeting, evasion bypass", "T4/T8"),
    "Jump": ("action state", "Unit is airborne and cannot be targeted until landing.", "untargetable timing, Dragoon identity", "T5"),
    "KO": ("defeated state", "Unit is down and subject to death/crystal/chest countdown rules.", "revive pressure", "T3/T5"),
    "Oil": ("debuff/element", "Next fire-elemental damage is amplified and consumes the vulnerability.", "element setup, fire combo", "T3/T4"),
    "Performing": ("action state", "Unit is singing or dancing and cannot evade while the performance persists.", "global song/dance risk", "T4/T5"),
    "Poison": ("debuff/attrition", "Unit loses a fraction of max HP at turn end for a timed duration.", "attrition, anti-tank pressure", "T3/T5"),
    "Protect": ("buff/mitigation", "Incoming physical pressure is reduced and some physical/equipment skills become harder to land.", "physical mitigation", "T3/T4"),
    "Reflect": ("other", "Reflectable spells bounce to another target.", "magic routing, targeting risk", "T4/T5"),
    "Regen": ("buff/attrition", "Unit restores a fraction of max HP at turn end for a timed duration.", "sustain loop, attrition counter", "T3/T5"),
    "Reraise": ("buff/revive", "Unit revives automatically once after KO trigger timing resolves.", "revive reliability", "T3/T5"),
    "Shell": ("buff/mitigation", "Incoming magical pressure and many magic/status formulas are reduced.", "magic mitigation", "T3/T4"),
    "Silence": ("debuff", "Unit cannot use most spell-like command sets.", "anti-caster control", "T2/T5"),
    "Sleep": ("debuff/control", "Unit stops gaining CT, cannot evade or react, and wakes on damage.", "hard control with damage-break counterplay", "T4/T5"),
    "Slow": ("debuff/timing", "Unit gains CT more slowly and takes fewer turns over time.", "tempo denial", "T5"),
    "Stone": ("debuff/defeat", "Unit is petrified, cannot act or gain CT, and is effectively removed until cured.", "hard disable, KO-adjacent state", "T3/T5"),
    "Stop": ("debuff/timing", "Unit's CT is frozen; evasion and reactions are suppressed.", "hard tempo stop", "T4/T5"),
    "Toad": ("debuff/form", "Unit is limited to weak attacks or Toad and cannot use reactions.", "form control, caster shutdown", "T4/T5"),
    "Traitor": ("control/flag", "Unit is treated as hostile/defected by allegiance logic.", "AI/allegiance edge case", "T8"),
    "Undead": ("other/undead", "Healing/revive interactions are inverted or altered for undead units.", "necromancy, healing reversal", "T3"),
    "Unused1": ("unused/local", "Local baseline vocabulary includes this status; behavior is not design-authoritative.", "do not use until proven", "proof first"),
    "Vampire": ("control/undead", "Unit is forced into vampire behavior and loses normal reactions/evasion.", "hard control, monster/undead flavor", "T4/T8"),
}


EXPLICIT_TAGS = {
    "Cure": ["healing", "magical"],
    "Cura": ["healing", "magical"],
    "Curaga": ["healing", "magical"],
    "Curaja": ["healing", "magical"],
    "Raise": ["revive", "magical"],
    "Arise": ["revive", "magical"],
    "Reraise": ["revive", "status_add"],
    "Regen": ["healing", "status_add"],
    "Protect": ["defense", "status_add"],
    "Protectja": ["defense", "status_add", "aoe"],
    "Shell": ["defense", "status_add"],
    "Shellja": ["defense", "status_add", "aoe"],
    "Wall": ["defense", "status_add"],
    "Esuna": ["status_clear", "magical"],
    "Holy": ["damage", "magical", "elemental"],
    "Fire": ["damage", "magical", "elemental"],
    "Fira": ["damage", "magical", "elemental"],
    "Firaga": ["damage", "magical", "elemental"],
    "Firaja": ["damage", "magical", "elemental"],
    "Thunder": ["damage", "magical", "elemental"],
    "Thundara": ["damage", "magical", "elemental"],
    "Thundaga": ["damage", "magical", "elemental"],
    "Thundaja": ["damage", "magical", "elemental"],
    "Blizzard": ["damage", "magical", "elemental"],
    "Blizzara": ["damage", "magical", "elemental"],
    "Blizzaga": ["damage", "magical", "elemental"],
    "Blizzaja": ["damage", "magical", "elemental"],
    "Poison": ["status_add", "undead"],
    "Toad": ["status_add", "magical"],
    "Death": ["instant_ko", "magical"],
    "Flare": ["damage", "magical"],
    "Haste": ["timing", "status_add"],
    "Hasteja": ["timing", "status_add", "aoe"],
    "Slow": ["timing", "status_add"],
    "Slowja": ["timing", "status_add", "aoe"],
    "Stop": ["timing", "status_add"],
    "Immobilize": ["status_add", "movement"],
    "Float": ["movement", "status_add"],
    "Reflect": ["defense", "status_add"],
    "Quick": ["ct_action", "timing"],
    "Gravity": ["damage", "magical"],
    "Graviga": ["damage", "magical", "aoe"],
    "Meteor": ["damage", "magical", "aoe", "ct_action"],
    "Umbra": ["status_add"],
    "Empowerment": ["mp", "drain"],
    "Invigoration": ["damage", "healing", "drain"],
    "Belief": ["faith_up"],
    "Disbelief": ["faith_down"],
    "Corruption": ["status_add", "undead"],
    "Quiescence": ["status_add"],
    "Fervor": ["status_add"],
    "Trepidation": ["brave_down"],
    "Delirium": ["status_add"],
    "Harmony": ["status_clear"],
    "Hesitation": ["status_add"],
    "Repose": ["status_add"],
    "Induration": ["status_add"],
    "Moogle": ["healing", "aoe"],
    "Faerie": ["healing", "aoe"],
    "Golem": ["defense", "ally_buff"],
    "Carbuncle": ["defense", "status_add"],
    "Lich": ["damage", "drain", "magical"],
    "Seraph Song": ["healing", "global"],
    "Life's Anthem": ["healing", "global"],
    "Rousing Melody": ["timing", "stat_up", "global"],
    "Battle Chant": ["brave_up", "global"],
    "Magickal Refrain": ["stat_up", "global"],
    "Nameless Song": ["ally_buff", "random", "global"],
    "Finale": ["instant_ko", "global"],
    "Witch Hunt": ["mp", "global"],
    "Mincing Minuet": ["damage", "global"],
    "Slow Dance": ["timing", "stat_down", "global"],
    "Polka": ["stat_down", "global"],
    "Heathen Frolic": ["stat_down", "global"],
    "Forbidden Dance": ["status_add", "random", "global"],
    "Last Waltz": ["instant_ko", "global"],
    "Cyclone": ["damage", "physical"],
    "Pummel": ["damage", "physical", "random"],
    "Aurablast": ["damage", "physical"],
    "Shockwave": ["damage", "physical"],
    "Doom Fist": ["status_add", "physical"],
    "Purification": ["status_clear"],
    "Chakra": ["healing", "mp"],
    "Revive": ["revive"],
    "Steal Gil": ["steal", "economy"],
    "Steal Heart": ["status_add", "recruit"],
    "Steal Helm": ["steal"],
    "Steal Armor": ["steal"],
    "Steal Shield": ["steal"],
    "Steal Weapon": ["steal"],
    "Steal Accessory": ["steal"],
    "Steal EXP": ["steal", "jp_exp"],
    "Entice": ["recruit", "status_add"],
    "Stall": ["timing", "stat_down"],
    "Praise": ["brave_up"],
    "Intimidate": ["brave_down"],
    "Preach": ["faith_up"],
    "Enlighten": ["faith_down"],
    "Condemn": ["status_add", "instant_ko"],
    "Defraud": ["economy"],
    "Insult": ["status_add"],
    "Mimic Darlavon": ["status_add"],
    "Rend Helm": ["equipment_break"],
    "Rend Armor": ["equipment_break"],
    "Rend Shield": ["equipment_break"],
    "Rend Weapon": ["equipment_break"],
    "Rend MP": ["mp", "stat_down"],
    "Rend Speed": ["timing", "stat_down"],
    "Rend Power": ["stat_down", "physical"],
    "Rend Magick": ["stat_down", "magical"],
    "Focus": ["stat_up", "physical"],
    "Rush": ["damage", "physical", "random"],
    "Throw Stone": ["damage", "physical", "random"],
    "Salve": ["status_clear"],
    "Tailwind": ["timing", "stat_up"],
    "Steel": ["brave_up"],
    "Shout": ["stat_up", "timing", "brave_up"],
    "Ultima": ["damage", "magical", "unique"],
    "Judgment Blade": ["damage", "status_add", "physical"],
    "Cleansing Strike": ["damage", "status_add", "physical"],
    "Northswain's Strike": ["damage", "status_add", "physical"],
    "Hallowed Bolt": ["damage", "status_add", "physical"],
    "Divine Ruination": ["damage", "status_add", "physical"],
    "Crush Armor": ["equipment_break", "damage"],
    "Crush Helm": ["equipment_break", "damage"],
    "Crush Weapon": ["equipment_break", "damage"],
    "Crush Accessory": ["equipment_break", "damage"],
    "Duskblade": ["damage", "mp", "drain"],
    "Shadowblade": ["damage", "healing", "drain"],
    "Unholy Darkness": ["damage", "physical", "aoe"],
    "Dispelna": ["status_clear"],
    "Celestial Stasis": ["status_add", "aoe"],
    "Petrify": ["status_add"],
    "Shadowbind": ["status_add"],
    "Suffocate": ["instant_ko"],
    "Revengeance": ["damage", "special"],
    "Manaburn": ["damage", "mp"],
    "Fowlheart": ["brave_down", "status_add"],
    "Embrace": ["status_add"],
    "Darkness": ["status_add"],
    "Aphony": ["status_add"],
    "Befuddle": ["status_add"],
    "Bind": ["status_add", "movement"],
    "Nightmare": ["status_add"],
    "Ague": ["status_add"],
    "Magicksap": ["mp", "stat_down"],
    "Speedsap": ["timing", "stat_down"],
    "Powersap": ["stat_down", "physical"],
    "Mindsap": ["stat_down", "magical"],
    "Blood Drain": ["damage", "healing", "drain"],
    "Charm": ["status_add", "recruit"],
    "Aegis": ["defense", "ally_buff"],
    "Leg Shot": ["status_add", "movement"],
    "Arm Shot": ["status_add"],
    "Seal Evil": ["status_add", "undead"],
    "Meltdown": ["damage", "magical", "elemental"],
    "Tornado": ["damage", "magical", "elemental"],
    "Quake": ["damage", "magical", "elemental"],
    "Toadja": ["status_add", "aoe"],
    "Gravija": ["damage", "magical", "aoe"],
    "Flareja": ["damage", "magical", "aoe"],
    "Blindja": ["status_add", "aoe"],
    "Confuseja": ["status_add", "aoe"],
    "Sleepja": ["status_add", "aoe"],
    "Divine Ultima": ["damage", "magical", "unique"],
    "Disempower": ["stat_down"],
    "Dispelja": ["status_clear", "aoe"],
    "Return": ["special"],
    "Blind": ["status_add"],
    "Syphon": ["mp", "drain"],
    "Drain": ["damage", "healing", "drain"],
    "Faith": ["faith_up", "status_add"],
    "Doubt": ["faith_down", "status_add"],
    "Zombie": ["status_add", "undead"],
    "Silence": ["status_add"],
    "Berserk": ["status_add"],
    "Chicken": ["brave_down", "status_add"],
    "Confuse": ["status_add"],
    "Dispel": ["status_clear"],
    "Disable": ["status_add"],
    "Sleep": ["status_add"],
    "Break": ["status_add"],
    "Dragon's Charm": ["recruit", "status_add"],
    "Dragon's Gift": ["status_clear", "healing"],
    "Dragon's Might": ["stat_up", "ally_buff"],
    "Dragon's Speed": ["timing", "ally_buff"],
    "Holy Breath": ["damage", "elemental"],
    "Vengeance": ["damage", "special"],
    "Finishing Touch": ["status_add", "random"],
    "Choco Esuna": ["status_clear"],
    "Choco Cure": ["healing"],
    "Self-Destruct": ["damage", "special"],
    "Bad Breath": ["status_add", "aoe"],
    "Bequeath Bacon": ["healing", "special"],
    "Guardian Nymph": ["defense", "ally_buff"],
    "Shell Nymph": ["defense", "ally_buff"],
    "Life Nymph": ["healing"],
    "Magick Nymph": ["mp"],
    "Beef Up": ["stat_up", "physical"],
    "Grand Cross": ["status_add", "random", "aoe"],
    "Destroy": ["instant_ko", "unique"],
    "Energize": ["healing", "special"],
    "Parasite": ["status_add", "unique"],
    "Potion": ["healing"],
    "High Potion": ["healing"],
    "X-Potion": ["healing"],
    "Ether": ["mp"],
    "High Ether": ["mp"],
    "Elixir": ["healing", "mp"],
    "Antidote": ["status_clear"],
    "Eye Drops": ["status_clear"],
    "Echo Herbs": ["status_clear"],
    "Maiden's Kiss": ["status_clear"],
    "Gold Needle": ["status_clear"],
    "Holy Water": ["status_clear", "undead"],
    "Remedy": ["status_clear"],
    "Phoenix Down": ["revive", "random"],
}


REACTION_TAGS = {
    "Strength Surge": ["reaction", "stat_up", "physical"],
    "Magick Surge": ["reaction", "stat_up", "magical"],
    "Speed Surge": ["reaction", "timing", "stat_up"],
    "Vanish": ["reaction", "status_add", "defense"],
    "Vigilance": ["reaction", "defense"],
    "Dragonheart": ["reaction", "revive", "status_add"],
    "Regenerate": ["reaction", "healing", "status_add"],
    "Bravery Surge": ["reaction", "brave_up"],
    "Faith Surge": ["reaction", "faith_up"],
    "Critical: Recover HP": ["reaction", "healing"],
    "Critical: Recover MP": ["reaction", "mp"],
    "Critical: Quick": ["reaction", "ct_action", "timing"],
    "Bonecrusher": ["reaction", "damage", "physical"],
    "Magick Counter": ["reaction", "magical"],
    "Counter Tackle": ["reaction", "damage", "physical"],
    "Nature's Wrath": ["reaction", "damage", "special"],
    "Absorb MP": ["reaction", "mp", "drain"],
    "Gil Snapper": ["reaction", "economy"],
    "Auto-Potion": ["reaction", "healing"],
    "Counter": ["reaction", "damage", "physical"],
    "Cup of Life": ["reaction", "revive"],
    "Mana Shield": ["reaction", "mp", "defense"],
    "Soulbind": ["reaction", "damage", "special"],
    "Parry": ["reaction", "defense"],
    "Earplugs": ["reaction", "defense"],
    "Reflexes": ["reaction", "defense"],
    "Sticky Fingers": ["reaction", "steal"],
    "Shirahadori": ["reaction", "defense"],
    "Archer's Bane": ["reaction", "defense"],
    "First Strike": ["reaction", "damage", "physical"],
}


SUPPORT_TAGS = {
    "Equip Heavy Armor": ["support", "equipment_unlock"],
    "Equip Shields": ["support", "equipment_unlock"],
    "Equip Swords": ["support", "equipment_unlock"],
    "Equip Katana": ["support", "equipment_unlock"],
    "Equip Crossbows": ["support", "equipment_unlock"],
    "Equip Polearms": ["support", "equipment_unlock"],
    "Equip Axes": ["support", "equipment_unlock"],
    "Equip Guns": ["support", "equipment_unlock"],
    "Halve MP": ["support", "mp"],
    "JP Boost": ["support", "jp_exp"],
    "EXP Boost": ["support", "jp_exp"],
    "Attack Boost": ["support", "damage_boost", "physical"],
    "Defense Boost": ["support", "defense"],
    "Magick Boost": ["support", "damage_boost", "magical"],
    "Magick Defense Boost": ["support", "defense", "magical"],
    "Concentration": ["support", "accuracy"],
    "Tame": ["support", "recruit"],
    "Poach": ["support", "economy"],
    "Brawler": ["support", "damage_boost", "physical"],
    "Beast Tongue": ["support", "recruit"],
    "Throw Items": ["support", "throw"],
    "Safeguard": ["support", "defense", "equipment_break"],
    "Doublehand": ["support", "damage_boost", "physical"],
    "Dual Wield": ["support", "damage_boost", "physical"],
    "Beastmaster": ["support", "special"],
    "Evasive Stance": ["support", "defense"],
    "Reequip": ["support", "equipment_unlock"],
    "Swiftspell": ["support", "ct_action", "timing"],
}


MOVEMENT_TAGS = {
    "Movement +1": ["movement"],
    "Movement +2": ["movement"],
    "Movement +3": ["movement"],
    "Jump +1": ["movement"],
    "Jump +2": ["movement"],
    "Jump +3": ["movement"],
    "Ignore Elevation": ["movement"],
    "Lifefont": ["movement", "healing"],
    "Manafont": ["movement", "mp"],
    "Accrue EXP": ["movement", "jp_exp"],
    "Accrue JP": ["movement", "jp_exp"],
    "Cannot Enter Water": ["movement", "special"],
    "Teleport": ["movement", "special"],
    "Master Teleportation": ["movement", "special"],
    "Ignore Weather": ["movement"],
    "Ignore Terrain": ["movement"],
    "Waterwalking": ["movement"],
    "Swim": ["movement"],
    "Lavawalking": ["movement"],
    "Waterbreathing": ["movement"],
    "Levitate": ["movement", "status_add"],
    "Fly": ["movement", "special"],
    "Treasure Hunter": ["movement", "economy"],
}


def group_for(ability_id: int) -> tuple[str, str]:
    for start, end, command, owner in GROUPS:
        if start <= ability_id <= end:
            return command, owner
    return "Unmapped/local placeholder", "Unknown"


def normalize_name(name: str) -> str:
    return name.replace("\u00b1", "+/-")


def tags_for(row: dict[str, str]) -> list[str]:
    ability_id = int(row["Id"])
    name = row["Name"]
    tags = []
    if ability_id == 152:
        tags.extend(["healing", "special"])
    if ability_id in {440, 483, 508}:
        tags.extend(["local_placeholder"])
    for mapping in (EXPLICIT_TAGS, REACTION_TAGS, SUPPORT_TAGS, MOVEMENT_TAGS):
        tags.extend(mapping.get(name, []))

    if 60 <= ability_id <= 75 and not tags:
        tags.extend(["damage", "magical", "aoe"])
    if 76 <= ability_id <= 85 and not tags:
        tags.extend(["damage", "magical", "special"])
    if 126 <= ability_id <= 137 and not tags:
        tags.extend(["damage", "status_add", "terrain"])
    if 155 <= ability_id <= 230 and not tags:
        tags.extend(["unique", "damage"])
    if 248 <= ability_id <= 355 and not tags:
        tags.extend(["damage", "special"])
    if 382 <= ability_id <= 393:
        tags.extend(["throw", "damage", "physical"])
    if 394 <= ability_id <= 405:
        tags.extend(["jump", "movement"])
    if 406 <= ability_id <= 413:
        tags.extend(["damage", "physical", "ct_action"])
    if 414 <= ability_id <= 421:
        tags.extend(["arithmeticks_selector", "special"])
    if row.get("IsRandomDamage") == "1":
        tags.append("random")
    if row.get("IsRandomStatus") == "1":
        tags.append("random")
        tags.append("status_add")
    if row.get("ov_CT"):
        tags.append("ct_action")
    if row.get("ov_MPCost"):
        tags.append("mp")

    if not tags:
        lname = name.lower()
        if any(word in lname for word in ["breath", "beam", "attack", "punch", "bite", "claw", "gore", "slash", "blade", "meteor", "quake", "flare", "bomb", "anima"]):
            tags.append("damage")
        elif any(word in lname for word in ["shot", "leg", "arm"]):
            tags.extend(["status_add", "physical"])
        elif lname.startswith("throw "):
            tags.extend(["throw", "damage"])
        else:
            tags.append("special")
    return sorted(set(tags))


def summary_from_tags(tags: list[str]) -> str:
    primary = [TAG_DESCRIPTIONS.get(tag, tag) for tag in tags]
    return "; ".join(primary)


def load_abilities() -> list[dict[str, str]]:
    with (WORK / "baseline_abilities.csv").open(newline="", encoding="utf-8") as handle:
        rows = [row for row in csv.DictReader(handle) if row["Name"].strip()]
    return rows


def load_local_statuses() -> set[str]:
    statuses: set[str] = set()
    with (WORK / "baseline_jobs.csv").open(newline="", encoding="utf-8") as handle:
        for row in csv.DictReader(handle):
            for field in ["InnateStatus", "ImmuneStatus", "StartingStatus"]:
                for part in (row.get(field) or "").replace('"', "").split(","):
                    status = part.strip()
                    if status and status != "None":
                        statuses.add(status)
    return statuses


def ability_doc(rows: list[dict[str, str]]) -> str:
    lines = [
        "# Vanilla Ability Effect Index V0",
        "",
        "Status: Reference draft",
        "Date: 2026-06-20",
        "Generated by: `tools/build_vanilla_reference.py`",
        "Inputs:",
        "- `work/baseline_abilities.csv`",
        "- `tools/dump_baseline.py`",
        "- external references listed below",
        "",
        "## Purpose",
        "",
        "This document is a lookup atlas for existing Final Fantasy Tactics ability records before",
        "Generic Chronicle rewrites job skillsets.",
        "",
        "It is intentionally not a final balance document. It tells designers what each existing",
        "ability slot appears to do at a useful consultation level, then maps that ability to effect",
        "families that matter for the job-balance validation tracks.",
        "",
        "## Source And Trust Model",
        "",
        "- Local ability IDs, names, JP, random flags, CT overrides, and MP overrides come from",
        "  `work/baseline_abilities.csv`.",
        "- The local extraction does not contain every base formula, range, area, status, or X/Y value.",
        "  `tools/dump_baseline.py` explicitly notes that those base values are hardcoded in",
        "  `FFT_enhanced.exe` and must be checked against external formula data or proof patches.",
        "- Effect summaries below are design-reference tags, not byte-accurate formula records.",
        "- Some effect tags are curated from references; others are auto-derived from command ID",
        "  ranges, local flags, or name-keyword heuristics in `tools/build_vanilla_reference.py`.",
        "  Treat tags as consultation hints that must be verified before final formula-sensitive use.",
        "- Tags caused by local flags, such as `ct_action`, `mp`, and `random`, are exposed again in",
        "  the `Local flags` column. Other tags are research/design classification unless a later",
        "  proof artifact makes them authoritative.",
        "- Before a concrete redesign uses a formula-sensitive effect, use the matching validation",
        "  track and/or a local proof patch.",
        "",
        "## External References Consulted",
        "",
        "- GameFAQs, `Final Fantasy Tactics - Jobs/Abilities Chart` by just_call_me_ash:",
        "  https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3859",
        "- GameFAQs, `Final Fantasy Tactics - Battle Mechanics Guide` by AeroStar:",
        "  https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876",
        "- GameFAQs, `Final Fantasy Tactics: The War of the Lions` guide status/job pages:",
        "  https://gamefaqs.gamespot.com/psp/937312-final-fantasy-tactics-the-war-of-the-lions/faqs/76070/status-effects",
        "- Final Fantasy Wiki ability/status category pages:",
        "  https://finalfantasy.fandom.com/wiki/Final_Fantasy_Tactics_abilities",
        "  https://finalfantasy.fandom.com/wiki/Final_Fantasy_Tactics_statuses",
        "",
        "## Effect Tag Legend",
        "",
        "| Tag | Meaning |",
        "| --- | --- |",
    ]
    for tag, desc in sorted(TAG_DESCRIPTIONS.items()):
        lines.append(f"| `{tag}` | {desc} |")
    lines += [
        "",
        "## Complete Local Ability Index",
        "",
        "| Id | Ability | Command bucket | Vanilla owner | JP | Local flags | Effect summary | Tags |",
        "| ---: | --- | --- | --- | ---: | --- | --- | --- |",
    ]
    for row in rows:
        ability_id = int(row["Id"])
        command, owner = group_for(ability_id)
        tags = tags_for(row)
        flags = []
        if row["IsRandomDamage"] == "1":
            flags.append("random_damage")
        if row["IsRandomStatus"] == "1":
            flags.append("random_status")
        if row["ov_CT"]:
            flags.append(f"ct={row['ov_CT']}")
        if row["ov_MPCost"]:
            flags.append(f"mp={row['ov_MPCost']}")
        flag_text = ", ".join(flags) if flags else "-"
        tag_text = ", ".join(f"`{tag}`" for tag in tags)
        name = normalize_name(row["Name"])
        lines.append(
            f"| {ability_id} | `{name}` | {command} | {owner} | {row['JP']} | {flag_text} | "
            f"{summary_from_tags(tags)} | {tag_text} |"
        )
    lines += [
        "",
        "## Design Use",
        "",
        "- Treat `equipment_break`, `damage_boost`, `defense`, `ct_action`, `timing`, `status_add`,",
        "  `reaction`, and `movement` tags as validation-sensitive during job redesign.",
        "- Treat monster and boss rows as a vocabulary bank, not as mandatory player-skill patterns.",
        "- Treat duplicate names by ID, such as `Bio`, `Biora`, `Bioga`, `Ashura`, `Petrify`, and",
        "  `Ultima`, as separate records until the implementation pass confirms whether they share",
        "  formula data or only display names.",
    ]
    return "\n".join(lines) + "\n"


def status_doc(local_statuses: set[str]) -> str:
    lines = [
        "# Vanilla Status And Effect Map V0",
        "",
        "Status: Reference draft",
        "Date: 2026-06-20",
        "Generated by: `tools/build_vanilla_reference.py`",
        "Inputs:",
        "- `work/baseline_jobs.csv` for locally observed status vocabulary",
        "- external references listed below",
        "",
        "## Purpose",
        "",
        "This document gives Generic Chronicle a shared lookup map for vanilla statuses and status-like",
        "effects before skill redesign continues.",
        "",
        "The map exists so job proposals can quickly see whether an idea touches healing, attrition,",
        "evasion, CT timing, AI targeting, action denial, equipment pressure, or undead behavior.",
        "",
        "## External References Consulted",
        "",
        "- GameFAQs WotL status overview:",
        "  https://gamefaqs.gamespot.com/psp/937312-final-fantasy-tactics-the-war-of-the-lions/faqs/76070/status-effects",
        "- GameFAQs WotL buffs/debuffs pages:",
        "  https://gamefaqs.gamespot.com/psp/937312-final-fantasy-tactics-the-war-of-the-lions/faqs/76070/buffs",
        "  https://gamefaqs.gamespot.com/psp/937312-final-fantasy-tactics-the-war-of-the-lions/faqs/76070/debuffs",
        "- AeroStar Battle Mechanics Guide:",
        "  https://gamefaqs.gamespot.com/ps/197339-final-fantasy-tactics/faqs/3876",
        "- Final Fantasy Wiki status category page:",
        "  https://finalfantasy.fandom.com/wiki/Final_Fantasy_Tactics_statuses",
        "",
        "## Local Coverage",
        "",
        "Statuses observed in `work/baseline_jobs.csv`:",
        "",
        ", ".join(f"`{status}`" for status in sorted(local_statuses)),
        "",
        "Some playable ability statuses may not appear in JobData innate/immune/starting fields. This",
        "document therefore includes the broader vanilla status set from external references as design",
        "vocabulary.",
        "",
        "## Status Map",
        "",
        "| Status | Category | Core mechanical effect | Design hooks | Validation track | Local JobData vocab |",
        "| --- | --- | --- | --- | --- | --- |",
    ]
    for status, (category, effect, hooks, track) in sorted(STATUS_EFFECTS.items()):
        local = "yes" if status in local_statuses else "no"
        lines.append(f"| `{status}` | {category} | {effect} | {hooks} | {track} | {local} |")
    lines += [
        "",
        "## Cross-Status Interaction Notes",
        "",
        "- `Haste` and `Slow` are opposed tempo states; any redesign that touches either belongs in T5.",
        "- `Regen` and `Poison` are opposed attrition states; any redesign that touches either belongs",
        "  in T3 and usually T5.",
        "- `Protect` and `Shell` are baseline mitigation states; physical/magical damage assumptions",
        "  should keep them visible but not mandatory.",
        "- `Charging`, `Performing`, `Stop`, `Sleep`, `Disable`, `Toad`, and `Vampire` suppress evasion,",
        "  reactions, actions, or control in different ways. They are not interchangeable.",
        "- `Undead` is a major future hook for Necromancer because it changes healing/revive semantics.",
        "- `Invisible`, `Charm`, `Confuse`, `Berserk`, `Traitor`, and `Vampire` can change AI targeting",
        "  or control. Treat them as T8-sensitive unless the implementation is purely player-facing.",
        "- `Critical`, `KO`, `Crystal`, and `Chest` are not ordinary debuffs. They are HP/defeat states",
        "  and should not be used as casual skill riders.",
        "",
        "## Design Use",
        "",
        "- When a proposed skill adds, cancels, prevents, or converts a status, the job document should",
        "  name the status explicitly and cite the relevant validation track.",
        "- Status identity is allowed to change in Generic Chronicle, but broad control or mitigation",
        "  changes must be justified as system changes, not hidden inside a single job proposal.",
        "- For Necromancer, `Undead`, `Doom`, `Poison`, `Sleep`, `Charm`, `Confuse`, `Vampire`, and",
        "  corpse/KO-adjacent states are useful vocabulary, but the final job should not simply pack",
        "  every hard-control status into one kit.",
    ]
    return "\n".join(lines) + "\n"


def main() -> int:
    DOCS.mkdir(parents=True, exist_ok=True)
    ability_rows = load_abilities()
    local_statuses = load_local_statuses()
    (DOCS / "fft-vanilla-ability-effect-index.md").write_text(
        ability_doc(ability_rows), encoding="utf-8"
    )
    (DOCS / "fft-vanilla-status-effect-map.md").write_text(
        status_doc(local_statuses), encoding="utf-8"
    )
    print(f"wrote {DOCS / 'fft-vanilla-ability-effect-index.md'} ({len(ability_rows)} abilities)")
    print(f"wrote {DOCS / 'fft-vanilla-status-effect-map.md'} ({len(local_statuses)} local statuses)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
