using System.Reflection;
using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.Outbound.Data.Resilience;
using Elementum.Infrastructure.Outbound.Data.Repositories;

namespace Elementum.UnitTests.Architecture;

/// <summary>
/// Detects deferred LINQ/EF queries on ports and the resilience decorator.
/// Public methods must return materialized collections so Polly wraps the actual database work.
/// </summary>
internal static class QueryableBoundaryRules
{
    public static IReadOnlyList<string> DeferredQueryMethods(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && IsDeferredQuery(m.ReturnType))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();

    public static IReadOnlyList<string> ProductionDeferredQueryMethods()
    {
        Type[] productionTypes =
        [
            typeof(IPriceHistoryReadRepository),
            typeof(IPriceHistoryWriteRepository),
            typeof(IPriceHistoryRepository),
            typeof(ResilientPriceHistoryReadRepository),
            typeof(ResilientPriceHistoryWriteRepository),
            typeof(ResilientElementumDbContext),
            typeof(PriceHistoryRepository)
        ];

        return productionTypes.SelectMany(DeferredQueryMethods).ToList();
    }

    public static bool IsDeferredQuery(Type returnType)
    {
        var type = UnwrapTask(returnType);

        if (typeof(IQueryable).IsAssignableFrom(type))
            return true;

        if (!type.IsGenericType)
            return false;

        var definition = type.GetGenericTypeDefinition();
        if (definition != typeof(IEnumerable<>) && definition != typeof(IQueryable<>))
            return false;

        var argument = type.GetGenericArguments()[0];
        return argument.Namespace is not null
               && argument.Namespace.StartsWith("Elementum.Domain", StringComparison.Ordinal);
    }

    private static Type UnwrapTask(Type type)
    {
        if (!type.IsGenericType)
            return type;

        var definition = type.GetGenericTypeDefinition();
        if (definition == typeof(Task<>) || definition == typeof(ValueTask<>))
            return type.GetGenericArguments()[0];

        return type;
    }
}
