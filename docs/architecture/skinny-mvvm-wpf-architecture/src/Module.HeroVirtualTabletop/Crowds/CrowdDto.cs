using Module.HeroVirtualTabletop.Characters;

namespace Module.HeroVirtualTabletop.Crowds;

/// <summary>
/// Serialization-only data transfer object for a Crowd.
/// Keeps JSON concerns out of the Crowd domain class.
/// </summary>
internal record CrowdDto(
    string        Name,
    string?       SourceFile,
    List<string>  MemberNames,
    List<CrowdDto> NestedCrowds)
{
    internal static CrowdDto From(Crowd crowd) =>
        new(
            crowd.Name,
            crowd.SourceFile,
            crowd.Members.Select(m => m.Name).ToList(),
            crowd.NestedCrowds.Select(From).ToList());
}
