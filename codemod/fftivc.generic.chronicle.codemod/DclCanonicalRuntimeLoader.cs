namespace fftivc.generic.chronicle.codemod;

internal sealed record DclCanonicalRuntimePaths(
    string AuthoringPath,
    string ItemMetadataPath,
    string AbilityBindingsPath,
    string ReactionBindingsPath,
    string StatePresentationProfilesPath,
    string PolicyTicketTemplatesPath = "");

internal static class DclCanonicalRuntimeLoader
{
    public static DclCanonicalRuntimeCatalog LoadFiles(
        DclCanonicalRuntimePaths paths,
        ItemCatalog nativeItems,
        AbilityCatalog nativeAbilities)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(nativeItems);
        ArgumentNullException.ThrowIfNull(nativeAbilities);
        RequireFile(paths.AuthoringPath, nameof(paths.AuthoringPath));
        RequireFile(paths.ItemMetadataPath, nameof(paths.ItemMetadataPath));
        RequireFile(paths.AbilityBindingsPath, nameof(paths.AbilityBindingsPath));
        RequireFile(paths.ReactionBindingsPath, nameof(paths.ReactionBindingsPath));
        RequireFile(paths.StatePresentationProfilesPath, nameof(paths.StatePresentationProfilesPath));
        if (!string.IsNullOrWhiteSpace(paths.PolicyTicketTemplatesPath))
            RequireFile(paths.PolicyTicketTemplatesPath, nameof(paths.PolicyTicketTemplatesPath));
        return LoadJson(
            File.ReadAllText(paths.AuthoringPath),
            File.ReadAllText(paths.ItemMetadataPath),
            File.ReadAllText(paths.AbilityBindingsPath),
            File.ReadAllText(paths.ReactionBindingsPath),
            File.ReadAllText(paths.StatePresentationProfilesPath),
            nativeItems,
            nativeAbilities,
            string.IsNullOrWhiteSpace(paths.PolicyTicketTemplatesPath)
                ? ""
                : File.ReadAllText(paths.PolicyTicketTemplatesPath));
    }

    public static DclCanonicalRuntimeCatalog LoadJson(
        string authoringJson,
        string itemMetadataJson,
        string abilityBindingsJson,
        string reactionBindingsJson,
        string statePresentationProfilesJson,
        ItemCatalog nativeItems,
        AbilityCatalog nativeAbilities,
        string policyTicketTemplatesJson = "")
    {
        ArgumentNullException.ThrowIfNull(nativeItems);
        ArgumentNullException.ThrowIfNull(nativeAbilities);
        DclAuthoringRegistry authoring = DclAuthoringJsonLoader.Load(authoringJson);
        DclItemMetadataRegistry items = DclItemMetadataJsonLoader.Load(itemMetadataJson, nativeItems);
        DclAbilityBindingRegistry abilities = DclAbilityBindingJsonLoader.Load(
            abilityBindingsJson,
            nativeAbilities,
            authoring);
        DclNativeReactionBindingRegistry reactions = DclNativeReactionBindingJsonLoader.Load(
            reactionBindingsJson,
            nativeAbilities,
            authoring,
            abilities);
        DclStatePresentationProfileRegistry statePresentations =
            DclStatePresentationProfileJsonLoader.Load(statePresentationProfilesJson);
        DclCanonicalNativePolicyTicketTemplateRegistry policyTicketTemplates =
            string.IsNullOrWhiteSpace(policyTicketTemplatesJson)
                ? new DclCanonicalNativePolicyTicketTemplateRegistry([])
                : DclCanonicalNativePolicyTicketTemplateJsonLoader.Load(policyTicketTemplatesJson);
        return new DclCanonicalRuntimeCatalog(
            authoring,
            items,
            abilities,
            reactions,
            policyTicketTemplates,
            statePresentations);
    }

    public static DateTime LastWriteUtc(string path)
        => File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;

    private static void RequireFile(string path, string parameter)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new DclAuthoringLoadException($"Canonical runtime path {parameter} is required.");
        if (!File.Exists(path))
            throw new DclAuthoringLoadException($"Canonical runtime file does not exist: {path}");
    }
}
