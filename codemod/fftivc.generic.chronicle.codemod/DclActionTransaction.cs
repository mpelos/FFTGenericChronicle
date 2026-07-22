namespace fftivc.generic.chronicle.codemod;

internal readonly record struct DclUnitKey(int BattleGeneration, int UnitSlot, int CharacterId)
{
    public bool IsValid => BattleGeneration > 0 && UnitSlot is >= 0 and < 64 && CharacterId >= 0;
}

internal readonly record struct DclBattleTile(int X, int Y, int Layer);

internal sealed record DclActionDeclaration(
    long ActionInstanceId,
    DclUnitKey Source,
    string ActionId,
    int ProfileRevision,
    DclTargetMode TargetMode,
    DclUnitKey? TrackedTarget,
    DclBattleTile? FixedTile,
    DclBattleTile DeclarationTile,
    bool PassedRangeCheck,
    bool PassedVerticalCheck,
    int FinalMpCost,
    int ApprovedHpCap,
    int CastCt,
    long DeclaredAtGlobalCt,
    long ResolvesAtGlobalCt);

internal sealed class DclActionInstanceSequence
{
    private int _battleGeneration;
    private long _nextId = 1;

    public DclActionInstanceSequence(int battleGeneration)
    {
        Reset(battleGeneration);
    }

    public int BattleGeneration => _battleGeneration;
    internal long NextId => _nextId;

    public long Next()
    {
        if (_nextId == long.MaxValue)
            throw new OverflowException("The DCL ActionInstance sequence is exhausted for this battle generation.");
        return _nextId++;
    }

    public void Reset(int battleGeneration)
    {
        if (battleGeneration <= 0)
            throw new ArgumentOutOfRangeException(nameof(battleGeneration));
        _battleGeneration = battleGeneration;
        _nextId = 1;
    }

    internal void RestoreNext(long nextId)
    {
        if (nextId <= 0) throw new ArgumentOutOfRangeException(nameof(nextId));
        if (_nextId != 1) throw new InvalidOperationException("ActionInstance sequence restore requires a fresh battle sequence.");
        _nextId = nextId;
    }
}

internal sealed record DclDefenseResourceSnapshot(
    IReadOnlyDictionary<string, int> ParryAttemptCounts,
    bool BlockAvailable,
    long Revision = 0);

internal sealed record DclTargetResolutionSnapshot(
    DclUnitKey Target,
    int CurrentHp,
    long CombatStateRevision,
    DclDefenseResourceSnapshot DefenseResources);

internal sealed class DclTargetBatch
{
    private readonly DclTargetResolutionSnapshot[] _targets;
    private readonly Dictionary<DclUnitKey, int> _ordinals;

    public DclTargetBatch(int battleGeneration, IEnumerable<DclTargetResolutionSnapshot> targets)
    {
        if (battleGeneration <= 0)
            throw new ArgumentOutOfRangeException(nameof(battleGeneration));
        ArgumentNullException.ThrowIfNull(targets);
        _targets = targets.ToArray();
        _ordinals = new Dictionary<DclUnitKey, int>();
        var occupiedSlots = new HashSet<int>();
        for (int index = 0; index < _targets.Length; index++)
        {
            DclTargetResolutionSnapshot snapshot = _targets[index];
            if (!snapshot.Target.IsValid || snapshot.Target.BattleGeneration != battleGeneration)
                throw new ArgumentException("Every target must have a valid UnitKey in the action's battle generation.", nameof(targets));
            if (snapshot.CurrentHp < 0)
                throw new ArgumentException("A target snapshot cannot contain negative HP.", nameof(targets));
            if (!_ordinals.TryAdd(snapshot.Target, index))
                throw new ArgumentException("A TargetBatch cannot contain the same UnitKey twice.", nameof(targets));
            if (!occupiedSlots.Add(snapshot.Target.UnitSlot))
                throw new ArgumentException("A battle-generation TargetBatch cannot contain two identities for one unit slot.", nameof(targets));
            foreach ((string key, int count) in snapshot.DefenseResources.ParryAttemptCounts)
            {
                if (string.IsNullOrWhiteSpace(key) || count < 0)
                    throw new ArgumentException("Parry resource keys must be named and their attempt counts nonnegative.", nameof(targets));
            }
            if (snapshot.DefenseResources.Revision < 0)
                throw new ArgumentException("A defense-resource revision cannot be negative.", nameof(targets));
        }
    }

    public IReadOnlyList<DclTargetResolutionSnapshot> Targets => _targets;
    public int Count => _targets.Length;
    public bool TryGetOrdinal(DclUnitKey target, out int ordinal) => _ordinals.TryGetValue(target, out ordinal);
}

internal sealed class DclMutableDefenseResources
{
    private readonly Dictionary<string, int> _parryAttempts;
    private readonly long _revision;

    public DclMutableDefenseResources(DclDefenseResourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _parryAttempts = new Dictionary<string, int>(snapshot.ParryAttemptCounts, StringComparer.Ordinal);
        BlockAvailable = snapshot.BlockAvailable;
        _revision = snapshot.Revision;
    }

    public bool BlockAvailable { get; private set; }

    public int CurrentParryPenalty(string resourceKey, int repeatedParryStep = 4)
    {
        if (string.IsNullOrWhiteSpace(resourceKey)) throw new ArgumentException("A Parry resource key is required.", nameof(resourceKey));
        if (repeatedParryStep < 0) throw new ArgumentOutOfRangeException(nameof(repeatedParryStep));
        return checked(-GetParryAttempts(resourceKey) * repeatedParryStep);
    }

    public int SpendParryAttempt(string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey)) throw new ArgumentException("A Parry resource key is required.", nameof(resourceKey));
        int next = checked(GetParryAttempts(resourceKey) + 1);
        _parryAttempts[resourceKey] = next;
        return next;
    }

    public bool TrySpendBlock()
    {
        if (!BlockAvailable) return false;
        BlockAvailable = false;
        return true;
    }

    public DclDefenseResourceSnapshot CaptureSnapshot()
        => new(
            _parryAttempts
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            BlockAvailable,
            _revision);

    public void ApplyFinalSnapshot(DclDefenseResourceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Revision != _revision)
            throw new InvalidOperationException("A transaction cannot replace defense resources from another revision.");
        _parryAttempts.Clear();
        foreach ((string key, int count) in snapshot.ParryAttemptCounts)
        {
            if (string.IsNullOrWhiteSpace(key) || count < 0)
                throw new ArgumentException("Final Parry resources must be named and nonnegative.", nameof(snapshot));
            _parryAttempts.Add(key, count);
        }
        BlockAvailable = snapshot.BlockAvailable;
    }

    private int GetParryAttempts(string resourceKey)
        => _parryAttempts.TryGetValue(resourceKey, out int attempts) ? attempts : 0;
}

internal enum DclActionTransactionStage
{
    Declared,
    TargetBatchSnapshotted,
    Planning,
    PlanSealed,
    Committing,
    ReactionWindowReady,
    ReactionWindowOpen,
    Settled,
    ResourceFailed,
}

internal sealed record DclPlannedEffect(
    int EffectIndex,
    DclEffectProfile Effect,
    bool ImmediateWithinAction);

internal sealed record DclStrikePlan(
    DclUnitKey Target,
    int TargetOrdinal,
    int StrikeIndex,
    IReadOnlyList<DclPlannedEffect> Effects);

internal readonly record struct DclStrikeCommitDecision(
    DclStrikePlan Strike,
    bool ExecuteMechanics,
    string Reason)
{
    public bool Skipped => !ExecuteMechanics;
}

internal sealed class DclActionTransaction
{
    private readonly DclActionProfile _profile;
    private readonly Dictionary<DclUnitKey, List<DclStrikePlan>> _plans = new();
    private readonly Dictionary<DclUnitKey, DclMutableDefenseResources> _defenseResources = new();
    private readonly HashSet<DclUnitKey> _koTargets = new();
    private readonly HashSet<(DclUnitKey Target, int StrikeIndex)> _completedStrikes = new();
    private readonly HashSet<(DclUnitKey Target, int StrikeIndex)> _executedStrikes = new();
    private readonly List<DclPlannedEffect> _visibleEffects = new();
    private DclStrikePlan[] _commitSequence = [];
    private int _nextCommitIndex;
    private DclStrikePlan? _activeStrike;
    private bool _reactionWindowOpened;

    public DclActionTransaction(DclActionDeclaration declaration, DclActionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        ArgumentNullException.ThrowIfNull(profile);
        DclAuthoringValidation validation = DclAuthoringContract.Validate(profile);
        if (!validation.IsValid)
            throw new ArgumentException($"The action profile is not normalized: {string.Join("; ", validation.Findings)}", nameof(profile));
        ValidateDeclaration(declaration, profile);
        Declaration = declaration;
        _profile = profile;
        Stage = DclActionTransactionStage.Declared;
    }

    public DclActionDeclaration Declaration { get; }
    public DclActionTransactionStage Stage { get; private set; }
    public DclTargetBatch? TargetBatch { get; private set; }
    public IReadOnlyList<DclPlannedEffect> VisibleEffects => _visibleEffects;
    public bool ReactionWindowOpened => _reactionWindowOpened;

    public void SnapshotTargetBatch(DclTargetBatch batch)
    {
        RequireStage(DclActionTransactionStage.Declared);
        ArgumentNullException.ThrowIfNull(batch);
        if (batch.Targets.Any(target => target.Target.BattleGeneration != Declaration.Source.BattleGeneration))
            throw new InvalidOperationException("TargetBatch and source must belong to the same battle generation.");
        TargetBatch = batch;
        foreach (DclTargetResolutionSnapshot target in batch.Targets)
        {
            _plans.Add(target.Target, []);
            _defenseResources.Add(target.Target, new DclMutableDefenseResources(target.DefenseResources));
            bool profileCanActOnKo = _profile.Effects.Any(effect =>
                effect.EligibleTargetStates.HasFlag(DclEligibleTargetStates.Ko));
            if (target.CurrentHp == 0 && !profileCanActOnKo) _koTargets.Add(target.Target);
        }
        Stage = DclActionTransactionStage.TargetBatchSnapshotted;
    }

    public void BeginPlanning()
    {
        RequireStage(DclActionTransactionStage.TargetBatchSnapshotted);
        Stage = DclActionTransactionStage.Planning;
    }

    public void PlanStrike(DclUnitKey target, int strikeIndex, IReadOnlyList<DclPlannedEffect> effects)
    {
        RequireStage(DclActionTransactionStage.Planning);
        if (TargetBatch is null || !TargetBatch.TryGetOrdinal(target, out int targetOrdinal))
            throw new ArgumentException("The Strike target is not a member of this TargetBatch.", nameof(target));
        if (strikeIndex < 0 || strikeIndex >= _profile.TransactionProfile.StrikeCount)
            throw new ArgumentOutOfRangeException(nameof(strikeIndex));
        ArgumentNullException.ThrowIfNull(effects);
        if (effects.Select(effect => effect.EffectIndex).Distinct().Count() != effects.Count)
            throw new ArgumentException("One Strike cannot contain duplicate effect indexes.", nameof(effects));
        foreach (DclPlannedEffect effect in effects)
        {
            if (effect.EffectIndex < 0 || effect.EffectIndex >= _profile.Effects.Count ||
                effect.Effect != _profile.Effects[effect.EffectIndex])
                throw new ArgumentException("Every planned effect must reference the matching normalized profile effect.", nameof(effects));
            bool expectedImmediate = IsNativeImmediate(effect.Effect.Kind) ||
                _profile.TransactionProfile.WithinActionApplication == DclWithinActionApplication.Immediate;
            if (effect.ImmediateWithinAction != expectedImmediate)
                throw new ArgumentException("Planned effect timing must preserve native-immediate effects and the normalized state timing.", nameof(effects));
        }
        List<DclStrikePlan> targetPlans = _plans[target];
        if (targetPlans.Any(plan => plan.StrikeIndex == strikeIndex))
            throw new InvalidOperationException("The same target/Strike identity cannot be planned twice.");
        targetPlans.Add(new DclStrikePlan(target, targetOrdinal, strikeIndex, effects.ToArray()));
    }

    public void SealPlan()
    {
        RequireStage(DclActionTransactionStage.Planning);
        if (TargetBatch is null) throw new InvalidOperationException("The target batch is missing.");
        foreach (DclTargetResolutionSnapshot target in TargetBatch.Targets)
        {
            List<DclStrikePlan> plans = _plans[target.Target];
            if (plans.Count != _profile.TransactionProfile.StrikeCount ||
                plans.Select(plan => plan.StrikeIndex).Order().Where((strike, index) => strike != index).Any())
                throw new InvalidOperationException("Every TargetResult must contain exactly the declared Strike indexes.");
        }
        _commitSequence = _plans.Values.SelectMany(plans => plans)
            .OrderBy(plan => plan.TargetOrdinal)
            .ThenBy(plan => plan.StrikeIndex)
            .ToArray();
        Stage = DclActionTransactionStage.PlanSealed;
    }

    public void BeginCommit()
    {
        RequireStage(DclActionTransactionStage.PlanSealed);
        Stage = DclActionTransactionStage.Committing;
    }

    public DclStrikeCommitDecision? BeginNextStrike()
    {
        RequireStage(DclActionTransactionStage.Committing);
        if (_activeStrike is not null)
            throw new InvalidOperationException("The active Strike must complete before another begins.");
        if (_nextCommitIndex >= _commitSequence.Length) return null;

        DclStrikePlan strike = _commitSequence[_nextCommitIndex++];
        if (_koTargets.Contains(strike.Target))
        {
            _completedStrikes.Add((strike.Target, strike.StrikeIndex));
            return new DclStrikeCommitDecision(strike, ExecuteMechanics: false, "target-ko-short-circuit");
        }
        _activeStrike = strike;
        return new DclStrikeCommitDecision(strike, ExecuteMechanics: true, "execute");
    }

    public void CompleteActiveStrike(bool targetIsKoAfterStrike)
    {
        RequireStage(DclActionTransactionStage.Committing);
        DclStrikePlan strike = _activeStrike ?? throw new InvalidOperationException("There is no active Strike.");
        _activeStrike = null;
        _completedStrikes.Add((strike.Target, strike.StrikeIndex));
        _executedStrikes.Add((strike.Target, strike.StrikeIndex));
        if (targetIsKoAfterStrike) _koTargets.Add(strike.Target);
        foreach (DclPlannedEffect effect in strike.Effects)
            if (effect.ImmediateWithinAction) _visibleEffects.Add(effect);
    }

    public DclMutableDefenseResources DefenseResourcesFor(DclUnitKey target)
    {
        if (TargetBatch is null || !_defenseResources.TryGetValue(target, out DclMutableDefenseResources? resources))
            throw new ArgumentException("The target is not in this transaction's snapshot.", nameof(target));
        return resources;
    }

    public void ApplyFinalDefenseResources(DclUnitKey target, DclDefenseResourceSnapshot snapshot)
    {
        RequireStage(DclActionTransactionStage.ReactionWindowReady);
        DefenseResourcesFor(target).ApplyFinalSnapshot(snapshot);
    }

    public DclRollIdentity RollIdentityFor(DclStrikePlan strike, DclRollSite site, int drawIndex)
    {
        if (drawIndex < 0) throw new ArgumentOutOfRangeException(nameof(drawIndex));
        return new DclRollIdentity(
            Declaration.Source.BattleGeneration,
            Declaration.ActionInstanceId,
            Declaration.Source.UnitSlot,
            Declaration.Source.CharacterId,
            strike.Target.UnitSlot,
            strike.Target.CharacterId,
            strike.StrikeIndex,
            site,
            drawIndex);
    }

    public void CompleteCommit()
    {
        RequireStage(DclActionTransactionStage.Committing);
        if (_activeStrike is not null || _nextCommitIndex != _commitSequence.Length ||
            _completedStrikes.Count != _commitSequence.Length)
            throw new InvalidOperationException("Every planned Strike must execute or receive the KO short-circuit before commit completes.");
        foreach (DclStrikePlan strike in _commitSequence)
        {
            if (!_executedStrikes.Contains((strike.Target, strike.StrikeIndex)))
                continue;
            foreach (DclPlannedEffect effect in strike.Effects)
                if (!effect.ImmediateWithinAction) _visibleEffects.Add(effect);
        }
        Stage = DclActionTransactionStage.ReactionWindowReady;
    }

    public void OpenReactionWindow()
    {
        RequireStage(DclActionTransactionStage.ReactionWindowReady);
        if (_reactionWindowOpened) throw new InvalidOperationException("An ActionInstance has only one Reaction window.");
        _reactionWindowOpened = true;
        Stage = DclActionTransactionStage.ReactionWindowOpen;
    }

    public void Settle()
    {
        if (Stage != DclActionTransactionStage.ReactionWindowOpen)
            throw new InvalidOperationException("A successful action settles only after its Reaction window.");
        Stage = DclActionTransactionStage.Settled;
    }

    public void FailResourceCommitment()
    {
        RequireStage(DclActionTransactionStage.Declared);
        Stage = DclActionTransactionStage.ResourceFailed;
    }

    private void RequireStage(DclActionTransactionStage required)
    {
        if (Stage != required) throw new InvalidOperationException($"Expected transaction stage {required}, found {Stage}.");
    }

    private static void ValidateDeclaration(DclActionDeclaration declaration, DclActionProfile profile)
    {
        if (declaration.ActionInstanceId <= 0) throw new ArgumentOutOfRangeException(nameof(declaration.ActionInstanceId));
        if (!declaration.Source.IsValid) throw new ArgumentException("The source UnitKey is invalid.", nameof(declaration));
        if (!StringComparer.Ordinal.Equals(declaration.ActionId, profile.ActionId) || declaration.ProfileRevision != profile.ProfileRevision)
            throw new ArgumentException("Declaration identity/revision does not match the normalized profile.", nameof(declaration));
        if (declaration.TargetMode != profile.TargetProfile.TargetMode)
            throw new ArgumentException("Declaration target mode does not match the normalized profile.", nameof(declaration));
        bool unitTarget = declaration.TargetMode == DclTargetMode.Unit;
        bool fixedTile = declaration.TargetMode == DclTargetMode.FixedTile;
        if (unitTarget != declaration.TrackedTarget.HasValue || fixedTile != declaration.FixedTile.HasValue)
            throw new ArgumentException("Declaration must contain exactly the target carrier required by its target mode.", nameof(declaration));
        if (declaration.TrackedTarget is { } tracked && (!tracked.IsValid || tracked.BattleGeneration != declaration.Source.BattleGeneration))
            throw new ArgumentException("Tracked target and source must have valid identities in the same battle generation.", nameof(declaration));
        if (!declaration.PassedRangeCheck || !declaration.PassedVerticalCheck)
            throw new ArgumentException("An illegal declaration cannot create an ActionInstance.", nameof(declaration));
        if (declaration.FinalMpCost < 0 || declaration.ApprovedHpCap < 0 || declaration.CastCt < 0 ||
            declaration.DeclaredAtGlobalCt < 0 ||
            declaration.ResolvesAtGlobalCt != checked(declaration.DeclaredAtGlobalCt + declaration.CastCt))
            throw new ArgumentException("Resource commitments and CastCT cannot be negative.", nameof(declaration));
    }

    private static bool IsNativeImmediate(DclEffectKind kind)
        => kind is DclEffectKind.Damage or DclEffectKind.Healing or DclEffectKind.ResourceChange or
            DclEffectKind.CtChange or DclEffectKind.Revive;
}
