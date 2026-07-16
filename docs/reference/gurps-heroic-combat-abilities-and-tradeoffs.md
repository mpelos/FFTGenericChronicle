# GURPS Heroic Combat Abilities and Tradeoffs

This document is a research reference for understanding how GURPS 4e and its heroic supplements
represent combat abilities. It distinguishes the rules categories that are often flattened into a
single idea such as "skill" or "special move," explains how each category is acquired and used, and
records the tradeoffs that keep combat choices meaningful.

This is not a DCL rules document and does not assign abilities to FFT jobs. A GURPS mechanic becomes
part of the DCL only when an owning DCL specification adopts it explicitly. The DCL does not
silently inherit a rule merely because this reference describes it.

Magic is outside this reference except where a nonmagical or weapon-based subsystem is compared to
magic to clarify its structure.

## Central design lesson

GURPS is not built around a rotation of limited-use combat buttons. Most martial options consume no
mana-like resource and have no cooldown. Their balance instead comes from exchanging one valuable
combat property for another:

- action now for a stronger action later;
- accuracy for more attacks, a harder defense, or a more valuable target location;
- offense for active defense;
- movement for preparation or weapon readiness;
- reliable output for a resisted or conditional payoff;
- broad competence for deep specialization;
- character points for permanent access or mastery;
- Fatigue Points for exceptional effort or supernatural output;
- freedom of choice for a required trigger, weapon, posture, target, or sequence.

Repeated use is not automatically a balance failure. If a character invests heavily in a narrow
specialization and the battlefield repeatedly presents its ideal situation, GURPS permits the
character to keep using it. The mechanic remains healthy when another situation makes a normal
Attack, movement, defense, preparation, or different option preferable.

Cooldowns and per-battle limits are therefore not the default GURPS answer to repetition. The
default answer is a visible tactical tradeoff.

## Rules vocabulary

The categories below are mechanically different even when their names all sound like abilities.

| Category | What it represents | Acquired through | Runtime resolution | Typical runtime cost |
| --- | --- | --- | --- | --- |
| Maneuver | The unit's overall commitment for a turn | Universally available rule | Choose one for the turn | Turn, movement allowance, and defense restrictions |
| Combat option | A modifier applied to a legal maneuver or attack | Usually universal; sometimes gated | Modify the relevant roll | Accuracy, damage, defense, movement, risk, or FP |
| Standard skill | Broad learned competence | Character points against an attribute and Difficulty | Roll against effective skill | Action plus situational modifiers |
| Technique | A trained special use of a prerequisite skill | Character points buying up from a default | Roll the specialized use | Usually the parent action and a residual penalty |
| Cinematic skill | An extraordinary learned feat | Character points plus campaign permission and prerequisites | Separate skill roll, sometimes followed by an attack | Preparation, FP, failure risk, or narrow conditions |
| Perk | A small and narrow rule exception | Usually one character point | Often passive or conditional | Opportunity cost and narrow applicability |
| Advantage | A substantial capability or access gate | Character points | Passive, activated, or a prerequisite for another rule | Build budget; sometimes activation cost |
| Imbuement skill | A learned way to empower a weapon or attack | Imbue access plus a specialized skill | Activation roll attached to an attack | FP, skill penalty, and the attack itself |
| Power | One or more advantages organized by a source and countermeasures | Character points and campaign framework | Depends on the underlying advantages | Defined limitations, activation, FP, or counterplay |
| Wildcard skill | Cinematic consolidation of many related skills | High character-point cost | Broad skill roll under its defined concept | Expensive specialization and campaign permission |
| Template | A point-budgeted role construction package | Campaign character-creation rules | No runtime resolution of its own | Commits the build budget and protects a niche |
| Lens | A smaller package that modifies or combines templates | Additional character points | No runtime resolution of its own | Opportunity cost and a narrower or hybrid identity |
| Power-up | A curated advancement option | Earned character points and access rules | Uses the rules category of its components | Whatever its component traits require |
| Style | A training curriculum and access structure | Style Familiarity and listed traits | Uses skills, techniques, perks, and options separately | Training breadth and specialization choices |

The name of a rule is not enough to classify it. For example, Feint exists as a maneuver and can
also be improved as a technique; Dual-Weapon Attack is a combat option whose penalty can be bought
off with techniques; Power Blow is a cinematic skill; Weapon Master is an advantage; and
Penetrating Strike is an Imbuement skill.

## Acquisition cost versus runtime cost

GURPS balances characters across two different economies.

### Acquisition economy

Character points buy attributes, skills, techniques, perks, advantages, and powers. Spending points
on one option means not spending them on another. A specialist can make one move highly reliable,
but pays by having less breadth, lower attributes, weaker defenses, or fewer unrelated abilities.

This produces a valid form of permanent mastery. Once a character has paid to remove the penalties
from Dual-Weapon Attack, the system does not normally charge a second resource every time the
character uses two weapons.

Templates, lenses, styles, and power-up lists organize this economy. They do not all create new
runtime resource systems.

### Runtime economy

At runtime, the principal resources are:

1. the maneuver or action chosen for the turn;
2. movement and position;
3. effective skill and margin of success;
4. active defenses retained until the next turn;
5. weapon readiness, ammunition, and required hands;
6. preparation already invested;
7. a legal trigger, target, range, posture, or sequence;
8. HP or FP when a rule explicitly spends them;
9. exposure to resistance rolls, Quick Contests, or failure consequences;
10. the opportunity to attack a different target or solve a different problem.

The two economies must not be conflated. A technique can be expensive to learn but free to repeat.
A universal maneuver can cost no points but consume an entire turn. A cinematic skill can require
both a large build investment and FP or preparation during combat.

## Maneuvers: the turn-level decision

A maneuver defines what a combatant commits to for a GURPS turn and which movement and defenses
remain legal. It is not normally learned from a class or skill list.

### Attack

Attack is the baseline commitment: use a ready weapon, make the attack roll, and retain the active
defenses allowed by an ordinary attack. Other choices are measured against this reliable baseline.

### Ready

Ready changes the physical state of equipment or the combatant. It draws a weapon, changes grip,
recovers a weapon that became unready, reloads, or otherwise prepares something for use.

Its tradeoff is explicit action economy. A weapon that delivers exceptional output can require a
Ready before it can deliver that output again. Fast-Draw and similar skills matter because they can
convert a Ready from an automatic action cost into a roll with a failure consequence.

### Aim

Aim invests time into a named target with a ready ranged weapon. The weapon's Accuracy and further
aiming benefits improve the later shot, while movement, defense, injury, loss of target, or firing
can end the preparation.

Aim is a setup-to-payoff exchange:

```text
turn now without a shot -> better shot against one tracked target later
```

Repeated Aim has a cap. Continued aiming after the cap produces no further benefit, and never firing
fails to convert preparation into output.

### Evaluate

Evaluate is the melee analogue of studying a target. Consecutive turns improve a later attack or
Feint against that opponent, up to the rule's limit.

Its cost is lost immediate offense and target commitment. It is valuable against a difficult
defender and inefficient when several ordinary attacks would already be reliable.

### Feint

Feint uses a contest of combat skill to create an opening. The margin of victory penalizes the
target's defense against the appropriate follow-up attack. It does not consume an active defense by
itself and does no damage.

The benefit is short-lived and cannot be banked indefinitely. A later Feint replaces the useful
window rather than building an unlimited defense penalty. The attacker must eventually cash the
opening in with an attack.

This creates a complete anti-spam loop without a consumable resource:

```text
Feint -> opening -> attack consumes the opportunity
Feint -> Feint -> no damage and no unlimited accumulation
```

### Wait

Wait trades immediate action for control over a declared future trigger. It can interrupt an
approach, guard an area, or react to a specific event. If the trigger never becomes true, the
reserved opportunity can be wasted. Its strength therefore depends on prediction and positioning.

### All-Out Attack

All-Out Attack raises offensive output through its selected mode but removes all active defenses
until the combatant's next turn. It is strong against an enemy unable to retaliate and dangerous
when enemies remain capable of acting.

The option is allowed to be repeatable because its vulnerability repeats as well.

### All-Out Defense

All-Out Defense sacrifices offense to improve survival. It is useful while stalling, holding a
position, waiting for help, or surviving a dangerous attack sequence. Repeating it can keep a
combatant alive but does not progress toward defeating an opponent by itself.

### Change Posture and Concentrate

Change Posture spends time moving among standing, kneeling, crouching, sitting, crawling, and prone
states. Concentrate commits the turn to a mental or supernatural activation. Both demonstrate the
same rule: changing a valuable state is itself an action unless another trait explicitly makes it
faster.

## Combat options: trade one property for another

Combat options modify an already legal attack or maneuver. They are a major reason that high skill
remains valuable: surplus accuracy can be converted into a different advantage.

### Deceptive Attack

Deceptive Attack converts attack skill into a defense penalty. In the core exchange, every `-2` to
the attack gives `-1` to the target's active defense.

```text
high chance to present a defendable attack
    versus
lower chance to present a harder-to-defend attack
```

The option is self-limiting. Excessive deception lowers effective skill until the attack becomes
unreliable. A highly trained fighter can use it frequently because converting superior skill into
pressure against active defenses is one of the rewards for that investment.

### Rapid Strike

Rapid Strike replaces one attack with two attacks, normally at `-6` to each. Trained by a Master or
Weapon Master reduces the penalty to `-3`; those reductions do not stack with one another.

Rapid Strike exchanges reliability for attack count. It is attractive against vulnerable or
low-defense targets and poor when both penalized rolls are unlikely to connect. It does not cost FP
under the basic option.

Flurry of Blows is an Extra Effort option that can spend FP to reduce the Rapid Strike penalty. That
is a separate heroic rule, not an inherent cost of Rapid Strike.

### Hit location

A called shot accepts an accuracy penalty to aim at a specific body part. The payoff can be a better
wounding modifier, a vulnerable location, reduced armor coverage, or a functionally important limb
or sense.

The target's anatomy, armor, current condition, and the attacker's effective skill determine whether
the trade is worthwhile. A center-mass attack remains the reliable alternative.

### Extra Effort in combat

Extra Effort is one of the places where heroic GURPS deliberately introduces a consumable runtime
resource. Representative options include:

- Mighty Blows, spending FP to improve one attack's damage;
- Flurry of Blows, spending FP to reduce a Rapid Strike penalty;
- Feverish Defense, spending FP to improve one active-defense roll.

Extra Effort is constrained by compatibility rules. A combatant cannot freely stack every powerful
offensive modifier into one action, and FP is paid at the granularity specified by the option. The
system uses FP here because the fiction is temporary exertion beyond ordinary capability.

## Techniques: trained special uses of skills

A technique starts from a prerequisite skill at a defined penalty called its default. Character
points improve the technique from that default toward its stated maximum.

The common point progressions are:

- Average: one point per `+1` above default;
- Hard: two points for the first `+1`, then one point per additional `+1`.

Individual techniques can have different maxima, special prerequisites, or restrictions. A
technique normally improves only its named use. Raising Broadsword improves all Broadsword uses;
raising Targeted Attack (Broadsword Swing/Skull) improves only that attack-location combination.

Techniques generally do not spend FP and do not create cooldowns. Their acquisition cost is narrow
specialization, while their runtime cost remains whatever penalty, trigger, action, or risk the
special use retains.

### Precision techniques

#### Targeted Attack

Targeted Attack specializes in one attack mode and one hit location. Its default incorporates the
location penalty, so the penalty is not applied a second time. Training normally removes only part
of that penalty rather than turning every targeted attack into a full-skill attack.

Examples of distinct specializations include:

```text
Broadsword Swing / Skull
Karate Kick / Leg
Bow Shot / Vitals
```

Each is a different investment. Mastery of one weapon, mode, and location does not imply mastery of
all called shots.

#### Disarming and Disarming Shot

Disarming attacks the opponent's control of a weapon rather than HP. It requires a legal weapon
target and exposes the attempt to the appropriate attack, contest, or resistance rules. The payoff
is tactical removal of enemy capability; the cost is using an attack that may cause little or no
direct injury.

### Multiple-attack techniques

#### Dual-Weapon Attack

Dual-Weapon Attack is a combat option supported by techniques for the participating weapon skills.
Without the technique, both attacks take the option's `-4` penalty. The off-hand attack suffers a
separate `-4` unless Ambidexterity or Off-Hand Weapon Training removes it.

Using different weapon families can therefore require:

```text
Dual-Weapon Attack (first weapon skill)
Dual-Weapon Attack (second weapon skill)
off-hand training or Ambidexterity
two ready weapons and legal attack lines
```

The permanent investment is intentionally substantial. Once paid, repeated two-weapon fighting is
the character's trained combat mode rather than a limited-use super move.

#### Combinations

Martial Arts can package a practiced sequence of attacks as a Combination. The character buys down
the penalties of that exact sequence. The gain is efficient execution of a rehearsed pattern; the
cost is narrow applicability, predictable sequencing, and the normal defense and attack economy of
the component moves.

#### Whirlwind Attack

Whirlwind Attack is cinematic and attacks multiple surrounding foes under strict positioning and
attack-economy rules. It gives up flexibility and other extra attacks to produce area-like melee
pressure. Its value appears when surrounded and falls sharply when enemies are dispersed.

### Defense and response techniques

#### Counterattack

Counterattack requires a preceding successful defense against the same opponent and uses the
resulting timing window to make the opponent's next defense harder. It remains an attack with a
technique penalty.

Its gate is a battle event, not a cooldown:

```text
enemy attacks -> defender successfully answers -> counterattack window exists
```

The technique cannot be selected freely when no one has created the trigger.

#### Riposte

Riposte voluntarily penalizes the user's current Parry. If that more difficult Parry succeeds, the
user earns a defense penalty against the opponent on the follow-up. The character risks being hit
now to create a stronger attack later.

#### Retain Weapon

Retain Weapon improves resistance to being disarmed. It is reactive and only matters when an enemy
attacks weapon control. It consumes character points for protection against a narrow threat but no
active combat rotation slot.

#### Aggressive Parry

Aggressive Parry converts a successful unarmed defense into a chance to injure the attacking limb.
It only operates when the combatant is attacked in a compatible way. Its offensive value is tied to
defensive exposure and cannot be generated on demand.

#### Roll with Blow

Roll with Blow is a cinematic defensive technique that changes how the character receives a
successful impact. It trades position or secondary consequences for reduced injury. It does not
prevent every cost of being hit; it changes which cost is paid.

### Position, timing, and control techniques

#### Sweep

Sweep tries to knock an opponent down instead of maximizing direct injury. Its value rises against
high-DR targets, dangerous defenders, or enemies whose posture matters. A successful knockdown
creates later positional advantage, while failure spends an attack without ordinary damage.

#### Stop Hit

Stop Hit contests an incoming attack with an interrupting attack. It embraces simultaneous risk:
the user attempts to exploit reach, timing, or superior skill but accepts the possibility that the
enemy's attack still lands. It is valuable against commitment and dangerous against a faster or
more skilled opponent.

#### Evade and Acrobatic movement techniques

Evade, Acrobatic Stand, and related techniques convert skill rolls into improved movement through
threatened space or faster recovery from bad posture. Failure can preserve the original action cost
or produce a worse positional state. They trade certainty for tempo.

## Cinematic skills

Cinematic skills are complete skills, not techniques bought up from a normal weapon skill. Many
have no default and require one or more of:

- Trained by a Master;
- Weapon Master;
- a minimum prerequisite skill;
- special instruction or a style;
- explicit permission for cinematic abilities in the campaign.

They commonly add an activation roll, concentration, FP, a resistance roll, or a severe situational
condition. They can modify an attack but do not automatically replace the attack roll and active
defense sequence.

### Power Blow

Power Blow converts concentration and a successful cinematic-skill roll into greatly increased ST
for a single strike. Rushing the preparation imposes a penalty. The user still needs a legal attack
to deliver the result.

The complete cost chain is therefore not merely "deal more damage":

```text
access prerequisite -> Power Blow investment -> preparation -> activation roll -> delivery attack
```

### Breaking Blow

Breaking Blow prepares a strike to overcome material protection more effectively. Its value is
specifically armor or object penetration rather than universal damage. Preparation, activation,
FP, and the delivery attack prevent it from being equivalent to a free armor divisor on every hit.

### Kiai

Kiai uses a shout and a resisted contest to stun or disrupt a target. It sacrifices guaranteed
weapon damage for a control attempt whose value depends on the target's resistance and eligibility.

### Pressure Points and Pressure Secrets

These skills add specialized physiological effects to precise unarmed attacks. They depend on
anatomy, a successful delivery, and additional skill or resistance mechanics. Targets without the
right physiology can invalidate the specialization.

### Blind Fighting

Blind Fighting uses a separate roll to operate under sensory restrictions. It is not permanent
omniscience and does not remove every consequence of darkness. It is valuable precisely when normal
vision-based combat is impaired.

### Flying Leap

Flying Leap extends movement or jumping into cinematic territory through skill, preparation, and
effort. Its payoff is position and reach rather than direct damage, and a failed or poorly planned
leap can leave the user exposed.

### Parry Missile Weapons

Parry Missile Weapons permits extraordinary active defenses against eligible projectiles. It is
reactive, depends on the incoming attack, and participates in the normal economy of repeated
defenses. It cannot be spammed when no projectile arrives.

### Mental Strength

Mental Strength is a specialized defense against mental or supernatural influence. It converts
training into resistance but has no offensive value against an enemy who does not use those effects.

### Zen Archery and Throwing Art

Zen Archery and Throwing Art raise ranged or thrown-weapon performance beyond realistic limits.
Their prerequisites and narrow domains make them character-defining investments. They do not grant
general mastery of every ranged attack.

## Perks and advantages

Perks and advantages often explain why two characters with the same weapon skill use the combat
rules differently.

### Perks

Perks are small, narrow permissions or exceptions. Martial Arts and Gun Fu use them to personalize
styles without raising every relevant skill. Typical functions include:

- removing a minor situational penalty;
- permitting a technique with an unusual prerequisite skill;
- improving one narrow Ready, grip, stance, or defensive interaction;
- authorizing a specific cinematic flourish;
- expanding what counts as familiar equipment or a legal style option.

Their anti-dominance mechanism is narrow scope. A perk that helps one weapon, posture, or situation
does not improve the entire combat engine.

### Access-gate advantages

Some advantages are valuable partly because they unlock a higher rules ceiling:

- Trained by a Master permits cinematic unarmed abilities and reduces Rapid Strike penalties;
- Weapon Master does the same for its defined weapons and also improves weapon performance;
- Heroic Archer changes the preparation and penalty economy of bows;
- Gunslinger changes the preparation and penalty economy of cinematic gunplay;
- Imbue permits learning Imbuement skills;
- Extra Attack adds attacks but remains subject to restrictions on limbs, weapons, and compatible
  options;
- Enhanced Defenses improve a named defense but consume a significant build budget.

These advantages cost points before combat and usually do not charge per use. Their purpose is to
distinguish a heroic specialist from an ordinary user of the same equipment.

## Imbuements

Imbuements are a distinct Power-Ups subsystem. They occupy a space between cinematic combat skills
and spells: the ability belongs to the character but is delivered through whatever compatible
weapon the character wields.

The structure is:

```text
Imbue advantage
    -> permission to learn individual Imbuement skills
        -> activation roll for a selected weapon modification
            -> FP cost or an accepted activation penalty
                -> ordinary attack delivers the modified effect
```

Representative functions include:

- Penetrating Strike: improve armor penetration;
- Forceful Blow: increase knockback or physical force;
- Multi-Shot: multiply projectiles;
- Far Shot: extend useful range;
- Guided Weapon: improve guidance toward the target;
- Shattershot: create fragments or an area-like secondary effect;
- Shockwave: project force beyond the immediate weapon contact;
- Telescoping Weapon: extend effective reach.

These are not ordinary techniques. Their explicit activation and energy economy supports effects
that would be too broad or supernatural to obtain merely by buying off a weapon-skill penalty.

The source of the power is also separate from the weapon. A character can empower different
compatible weapons rather than depending on one unique magic item.

## Powers

GURPS Powers builds extraordinary abilities primarily from advantages, enhancements, and
limitations. A power groups related abilities under a common source, talent, and countermeasures.

Limitations are central tradeoff tools. They can require:

- FP;
- preparation or recharge;
- a specific trigger or environment;
- a visible or interruptible activation;
- limited uses;
- a resistance roll;
- a particular weapon, body part, or state;
- vulnerability to a countermeasure or power source failure.

This is the appropriate GURPS layer for a genuinely superhuman capability that is not well modeled
as a difficult use of an ordinary skill. Powers and Supers show that higher power does not mean
discarding cost; it means authoring explicit limitations and counterplay at a higher scale.

## Wildcard skills

Wildcard skills replace many related standard skills with one broad cinematic competence, written
with an exclamation mark such as `Detective!`. They cost much more than an ordinary skill because
they compress breadth and reduce character-sheet overhead.

Optional wildcard rules can also relax penalties, improve criticals, or support story-level feats.
The tradeoff is deliberate abstraction: the game loses distinctions among individual skills in
exchange for a clear heroic concept and faster play.

Wildcard Skills is therefore a campaign-level complexity choice, not merely a stronger Difficulty
band for one ordinary skill.

## Styles, templates, lenses, and power-ups

GURPS core is classless, but its heroic lines organize choices into class-like structures without
turning every listed ability into a hardcoded class command.

### Styles

A martial-arts style is a curriculum containing required skills and a menu of techniques, cinematic
skills, perks, and optional traits. It describes what the school teaches and what a practitioner may
develop. Purchasing the style does not automatically grant maximum level in every listed technique.

Official style examples visibly separate ordinary skills, techniques, cinematic skills, cinematic
techniques, perks, advantages, and optional traits. This separation is part of the balance model:
the practitioner still chooses where to invest.

### Templates

A template is a curated point budget for a role. It sets required foundations and offers controlled
choices for customization. Dungeon Fantasy, Action, and Monster Hunters use templates to make
high-point characters faster to build and to protect distinct party niches.

A template is not itself a runtime restriction. Its mechanical effect occurs primarily when the
character spends the build budget.

### Lenses

A lens modifies a template or combines part of one profession with another. It offers a priced path
to hybridization without granting the full strength of two complete templates for free.

The Next Level, Ninja, Henchmen, and Monster Hunters: Sidekicks use lenses to represent mixed
training, lower-power starting points, and later transformation into a more capable role.

### Power-ups

A power-up is a curated advancement purchase. It may be a perk, advantage, technique, cinematic
skill, power, spell, Ally, or package. The label says where the option appears in character
advancement; it does not define a single runtime mechanic.

Dungeon Fantasy 11 ranges from small perks to abilities costing up to 100 points. This wide range
demonstrates that "power-up" means approved advancement content, not necessarily a limited-use
combat action.

## How heroic supplements extend the system

| Source | Primary contribution to heroic ability design | Tradeoff lesson |
| --- | --- | --- |
| GURPS Basic Set / GURPS Lite | Attributes, skills, maneuvers, active defenses, injury, FP, and the core point economy | Heroic options remain grounded in the ordinary action and success-roll engine |
| GURPS Martial Arts | Realistic and cinematic techniques, expanded maneuvers and combat options, Combinations, Targeted Attacks, styles, perks, and cinematic skills | Depth comes from specialization, sequencing, defense exposure, and situational rules rather than cooldowns |
| Dungeon Fantasy 1: Adventurers | High-powered profession templates for dungeon combat | Class-like identity is a point-budget and option-list structure |
| Dungeon Fantasy 3: The Next Level | Racial templates, profession-mixing lenses, profession power-ups, psionics, and spell-archery | Hybrid capability is explicitly priced instead of granted by informal multiclassing |
| Dungeon Fantasy 11: Power-Ups | Perks, major profession abilities, acquisition guidance, and design guidance | Advancement options can use many rules categories and many point scales |
| Dungeon Fantasy 12: Ninja | Ninja and assassin templates, cross-profession lenses, special powers, combat abilities, and tools | A named heroic profession can combine mundane training, gear, and powers without treating them as the same mechanic |
| Dungeon Fantasy 15: Henchmen | Lower-point archetypes, callings, upgrade lenses, perks, and power-ups | Power level and role can be scaled through smaller templates rather than flattened statistics |
| Dungeon Fantasy Denizens: Barbarians | Variant templates, Rage, perks, feats, maneuvers, gear, and power-ups | A heroic state such as Rage belongs to a power package with explicit modifiers, not automatically to the weapon-skill system |
| Dungeon Fantasy Denizens: Swashbucklers | Variant swashbucklers, perks, feats, specialties, Allies, and gear | Defensive agility and flourish emerge from several small investments and tactical permissions |
| Dungeon Fantasy 20: Slayers | Specialized professions and power-ups against demons, magic-users, and undead | Narrow enemy specialization can justify strong effects because target eligibility is a real cost |
| Dungeon Fantasy Roleplaying Game | A streamlined standalone heroic selection of GURPS rules with profession templates | GURPS can be narrowed for a genre without replacing its point-build and tradeoff foundations |
| Action 1: Heroes | Action-movie templates, focused options, and gear | High competence is produced by curated builds and genre assumptions |
| Action 3: Furious Fists | Martial archetypes, special advantages, perks, techniques, exotic weapons, and combat options | Cinematic melee can be layered on a fast action framework without turning every move into a consumable power |
| Gun Fu | Cinematic firearm styles, shooting techniques, perks, tricky shots, and impossible feats | Ranged heroics use preparation, accuracy, weapon handling, target restrictions, and access gates |
| Monster Hunters 1: Champions | 400-point templates, wildcard skills, powers, and gear | Very high point totals still organize roles through expensive breadth and explicit power sources |
| Monster Hunters 4: Sidekicks | Lower-powered templates and upgrade lenses | A campaign can express growth by changing the point package rather than inventing hidden scaling |
| Monster Hunters Power-Ups 1 | Perks, wildcard skills, lenses, inventions, and advancement guidance | Non-supernatural heroes receive power-ups through expertise and resources as well as paranormal traits |
| GURPS Powers | Advantage-based construction, power sources, talents, enhancements, limitations, and variable abilities | Superhuman scale is balanced through priced construction and explicit limitations |
| GURPS Supers | Genre application of Powers, wildcard skills, templates, new trait interpretations, and superhuman combat guidance | Genre conventions may raise the ceiling but still require a declared campaign framework |
| Power-Ups 1: Imbuements | Weapon-channelled skills between cinematic combat and magic | Broad weapon transformations justify activation and energy costs beyond ordinary techniques |
| Power-Ups 7: Wildcard Skills | Broad cinematic skills and optional narrative benefits | Simplification and breadth are purchased at a premium and chosen for the whole campaign |
| Template Toolkit 1: Characters | Template construction, niche protection, choices, lenses, and point optimization | Class-like structure should protect roles while retaining priced customization |

## Examples that must not be flattened into one category

| Example | Correct rules category | What primarily limits it |
| --- | --- | --- |
| Evaluate | Maneuver | Turn investment, target commitment, capped preparation |
| Feint | Maneuver, with possible technique improvement | No immediate damage, resisted contest, short payoff window |
| Aim | Maneuver | Preparation time, tracked target, interruption |
| All-Out Attack | Maneuver | Loss of all active defenses |
| Wait | Maneuver | Declared trigger and risk of lost opportunity |
| Deceptive Attack | Combat option | Own attack penalty in exchange for defense penalty |
| Rapid Strike | Combat option | Severe penalty to every attack; optional FP through Extra Effort |
| Dual-Weapon Attack | Combat option plus techniques | Two weapon-skill penalties, off-hand penalty, ready equipment |
| Targeted Attack | Technique | Narrow attack/location specialization and residual accuracy penalty |
| Counterattack | Technique | Requires the enemy to attack and the user to defend successfully |
| Riposte | Combat option or technique context from Martial Arts | Worse current defense for a better follow-up opening |
| Retain Weapon | Defensive technique | Only operates against disarming attempts |
| Whirlwind Attack | Cinematic technique | Positioning, attack-economy restrictions, and specialization |
| Power Blow | Cinematic skill | Access gate, preparation, activation roll, and delivery attack |
| Kiai | Cinematic skill | Resisted control attempt and eligible target |
| Parry Missile Weapons | Cinematic skill | Incoming projectile and active-defense economy |
| Weapon Master | Advantage | Significant point cost and defined weapon scope |
| Heroic Archer | Advantage | Point cost and bow-specific scope |
| Penetrating Strike | Imbuement skill | Imbue access, activation, FP or penalty, and attack delivery |
| Shadow Walker / One With Shadows | Profession power from Dungeon Fantasy: Ninja | Template or lens access and the underlying power construction |
| Rage | Power package from Denizens: Barbarians | Defined activation, modifiers, state, and package cost |
| `Detective!` or another wildcard | Wildcard skill | High point cost, campaign permission, and conceptual scope |

## Why repetition is sometimes correct

GURPS distinguishes repetition from dominance.

### Healthy repetition

Repetition is healthy when:

- the character paid for a stable specialization;
- the same battlefield state remains present;
- the action continues to expose its normal counterplay;
- another target, range, posture, defense profile, or objective would change the best choice;
- failure still matters;
- the action does not also dominate mobility, defense, control, and damage.

A dual-weapon specialist repeatedly making Dual-Weapon Attacks is comparable to an archer repeatedly
shooting a bow. It is a combat mode, not necessarily a resource-limited special attack.

### Unhealthy dominance

An option is suspect when it:

- is strictly better than Attack in every legal situation;
- deals more damage without losing accuracy, defense, movement, setup, or another resource;
- bypasses both armor and active defense without a compensating cost;
- controls the target while matching the best direct damage;
- erases the purpose of Ready, Aim, position, facing, or equipment choice;
- receives both a low acquisition cost and no meaningful runtime constraint;
- uses an invisible cooldown only to conceal the absence of a natural tradeoff.

The first repair question is not "how many turns should its cooldown be?" It is "which combat
property is this ability converting, risking, consuming, or requiring?"

## Tradeoff patterns for future analysis

The following patterns cover most martial and heroic abilities found in the researched sources.

| Pattern | Cost side | Payoff side | Representative examples |
| --- | --- | --- | --- |
| Setup and payoff | Earlier action, target lock, interruption risk | Later accuracy or stronger effect | Aim, Evaluate, Feint, Power Blow |
| Accuracy conversion | Lower effective attack skill | More attacks, lower defense, valuable location | Rapid Strike, Deceptive Attack, Targeted Attack |
| Defense exposure | Reduced or lost active defense | Accuracy, damage, tempo, or counterpressure | All-Out Attack, Riposte, Aggressive Parry |
| Damage conversion | Lower or no direct injury | Knockdown, disarm, stun, control, forced state | Sweep, Disarming, Kiai |
| Triggered response | Enemy must create a legal event | Efficient reaction or counterattack | Counterattack, Retain Weapon, Parry Missile Weapons |
| Position commitment | Required adjacency, surround, line, or movement | Area pressure, reach, interception | Whirlwind Attack, Stop Hit, Flying Leap |
| Equipment commitment | Hands, ready state, ammunition, weapon family | Specialized output or action count | Dual-Weapon Attack, Gun Fu techniques, Fast-Draw |
| Energy expenditure | FP and sometimes activation roll | Exceptional temporary performance | Extra Effort, Imbuements, some cinematic skills |
| Narrow specialization | Character points and limited scope | High reliability in one situation | Targeted Attack, weapon-specific DWA, Style Perks |
| Access gate | Expensive advantage, template, style, or permission | Higher cinematic rules ceiling | Weapon Master, Trained by a Master, Heroic Archer, Imbue |
| Resistance exposure | Enemy contest or resistance roll | Control or non-damage victory | Feint, Kiai, Pressure Points |
| Breadth premium | High build cost and less detail | Fast, broad heroic competence | Wildcard skills |
| Target restriction | Only a creature type or vulnerability qualifies | Strong specialized effect | Dungeon Fantasy Slayers |

## Ability-analysis checklist

Before treating a GURPS-derived idea as a possible FFT ability, record:

```text
Source category:
Prerequisite skill or advantage:
Acquisition cost:
Chosen maneuver or action:
Attack or activation roll:
Target resistance or active defense:
Movement retained:
Defenses retained:
Preparation required:
Equipment, posture, range, and target restrictions:
FP, HP, ammunition, or other consumable cost:
Failure consequence:
Payoff window and expiration:
Compatible and incompatible options:
Why ordinary Attack remains useful:
Whether repeated use is acceptable when the ideal state persists:
Visible state the player must track:
```

This checklist describes the source mechanic before any FFT adaptation. It prevents a technique
from being copied only as an effect name while losing the tradeoff that balanced it.

## Implications for DCL research

The research supports the following boundaries without adopting any new DCL rule:

- a GURPS maneuver is not automatically an FFT Action Skill;
- a technique is not automatically a command with MP cost or cooldown;
- a technique's defining feature can be trained penalty reduction rather than a new effect;
- cinematic access can be represented separately from ordinary weapon competence;
- perks and advantages can enable a rule without becoming buttons in battle;
- templates and lenses explain who may buy an ability, not necessarily how it is paid for at
  runtime;
- power-ups are advancement menus, not one mechanical family;
- Imbuements require separate analysis because the DCL deliberately has no FP;
- a mechanic that depends on grappling, Reach C, detailed hit locations, Perception, or another
  excluded subsystem cannot be imported without redesigning its cost and counterplay;
- the correct adaptation preserves the decision created by the source tradeoff, not necessarily the
  tabletop procedure or numerical modifier.

## Official sources

Core and combat structure:

- [GURPS Lite, Fourth Edition](https://warehouse23.com/products/gurps-lite-fourth-edition)
- [GURPS Fourth Edition Combat Cards](https://warehouse23.com/products/gurps-fourth-edition-combat-cards)
- [GURPS Basic Set FAQ](https://www.sjgames.com/gurps/faq/FAQ4-3.html)
- [GURPS Martial Arts](https://warehouse23.com/products/gurps-martial-arts)
- [GURPS Martial Arts index](https://www.sjgames.com/GURPS/books/martialarts/img/index.pdf)
- [Kendo and Iaido style sample](https://www.sjgames.com/gurps/books/martialarts/img/kendo.pdf)
- [Fictional style sample](https://www.sjgames.com/gurps/books/martialarts/img/freefighting.pdf)
- [GURPS Martial Arts Techniques Cheat-Sheet](https://warehouse23.com/products/gurps-martial-arts-techniques-cheat-sheet)

Heroic fantasy and profession structure:

- [Dungeon Fantasy Roleplaying Game](https://warehouse23.com/products/dungeon-fantasy-roleplaying-game-pdf)
- [GURPS Dungeon Fantasy 1: Adventurers](https://warehouse23.com/products/gurps-dungeon-fantasy-1-adventurers-1)
- [GURPS Dungeon Fantasy 3: The Next Level](https://warehouse23.com/products/gurps-dungeon-fantasy-3-the-next-level-1)
- [GURPS Dungeon Fantasy 11: Power-Ups](https://warehouse23.com/products/gurps-dungeon-fantasy-11-power-ups)
- [GURPS Dungeon Fantasy 12: Ninja](https://warehouse23.com/products/gurps-dungeon-fantasy-12-ninja)
- [GURPS Dungeon Fantasy 15: Henchmen](https://warehouse23.com/products/gurps-dungeon-fantasy-15-henchmen)
- [GURPS Dungeon Fantasy 20: Slayers](https://warehouse23.com/products/gurps-dungeon-fantasy-monsters-20-slayers)
- [GURPS Dungeon Fantasy Denizens: Barbarians](https://warehouse23.com/products/gurps-dungeon-fantasy-denizens-barbarians)
- [GURPS Dungeon Fantasy Denizens: Swashbucklers](https://warehouse23.com/products/gurps-dungeon-fantasy-denizens-swashbucklers)

Other heroic frameworks:

- [GURPS Action 1: Heroes](https://warehouse23.com/products/gurps-action-1-heroes)
- [GURPS Action 3: Furious Fists](https://warehouse23.com/products/gurps-action-3-furious-fists)
- [GURPS Gun Fu](https://warehouse23.com/products/gurps-gun-fu)
- [GURPS Monster Hunters 1: Champions](https://warehouse23.com/products/gurps-monster-hunters-1-champions)
- [GURPS Monster Hunters 4: Sidekicks](https://warehouse23.com/products/gurps-monster-hunters-4-sidekicks)
- [GURPS Monster Hunters Power-Ups 1](https://warehouse23.com/products/gurps-monster-hunters-power-ups-1)
- [GURPS Powers](https://warehouse23.com/products/gurps-powers)
- [GURPS Supers](https://warehouse23.com/collections/digital-downloads/products/gurps-supers-3)
- [GURPS Power-Ups 1: Imbuements](https://warehouse23.com/products/gurps-power-ups-1-imbuements-1)
- [GURPS Power-Ups 7: Wildcard Skills](https://warehouse23.com/products/gurps-power-ups-7-wildcard-skills)
- [GURPS Template Toolkit 1: Characters](https://warehouse23.com/products/gurps-template-toolkit-1-characters)
