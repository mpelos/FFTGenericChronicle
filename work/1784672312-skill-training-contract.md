# Weapon Skill training contract

## Context

The DCL already had the A-E aptitude Rank table and GURPS score conversion, but those functions were separate. The remaining offline gap was a single canonical contract for the explicit inputs needed to produce a Weapon Skill or Shield Skill score without reading draft job design.

## Conclusion

- Added `DclSkillTrainingInput`.
- Added `DclSkillTrainingResult`.
- Added `DclSkillRules.ResolveTraining`.
- The resolver combines controlling attribute, weapon/shield Difficulty, explicit job/family Aptitude Tier, native Job Level, and explicit post-training modifiers into one retained Rank, base score, and final score.
- Missing/unknown Difficulty and invalid Tier/Job Level fail closed through the existing underlying validation.
- No job content was authored; the job/family Aptitude Tier remains explicit policy input.

## Evidence

- `codemod/fftivc.generic.chronicle.codemod/DclPhysicalRules.cs`
- `codemod/fftivc.generic.chronicle.codemod.smoketests/Program.cs`
- `dotnet build codemod/fftivc.generic.chronicle.codemod.smoketests/fftivc.generic.chronicle.codemod.smoketests.csproj -c SkillTraining1 --no-restore -m:1 --nologo -v:minimal`
