using System.Reflection;

namespace Elementum.UnitTests.Architecture;

/// <summary>
/// Shared predicates for Inbound namespace locks. Production assemblies must yield an empty
/// misplaced list; probe types in this test project must appear in it.
/// </summary>
internal static class InboundNamespaceRules
{
    public const string ApiInboundControllers = "Elementum.Api.Inbound.Controllers";
    public const string ApplicationInboundUseCases = "Elementum.Application.Inbound.UseCases";

    public static IReadOnlyList<Type> Controllers(Assembly assembly) =>
        assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsPublic: true, IsNested: false }
                        && t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToList();

    public static IReadOnlyList<string> MisplacedControllers(Assembly assembly) =>
        Controllers(assembly)
            .Where(t => t.Namespace != ApiInboundControllers)
            .Select(t => t.FullName!)
            .ToList();

    public static IReadOnlyList<Type> UseCases(Assembly assembly) =>
        assembly.GetTypes()
            .Where(t => t is { IsPublic: true, IsNested: false }
                        && t.Name.EndsWith("UseCase", StringComparison.Ordinal))
            .ToList();

    public static IReadOnlyList<string> MisplacedUseCases(Assembly assembly) =>
        UseCases(assembly)
            .Where(t => t.Namespace is null
                        || !t.Namespace.StartsWith(ApplicationInboundUseCases, StringComparison.Ordinal))
            .Select(t => t.FullName!)
            .ToList();
}
