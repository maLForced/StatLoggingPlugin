using JetBrains.Annotations;

namespace StatLoggingPlugin;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public class StatLoggingConfiguration
{
    public bool? CommonDB { get; init; } = false;
    public string? CommonDBFileLocation { get; init; }
    public string? ServerName { get; init; }
}
