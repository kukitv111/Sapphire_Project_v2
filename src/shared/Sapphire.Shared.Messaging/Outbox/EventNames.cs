using System.Text.RegularExpressions;

namespace Sapphire.Shared.Messaging.Outbox;

public static class EventNames
{
    public static string For(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return FromLegacy(eventType.FullName ?? eventType.Name);
    }

    // Converts records written before the versioned envelope rollout without loading assemblies.
    public static string FromLegacy(string legacyType)
    {
        var qualifiedName = legacyType.Split(',', 2)[0];
        var parts = qualifiedName.Split('.');
        var root = parts.FirstOrDefault(part => part is "Auth" or "Billing" or "Session")
            ?? throw new InvalidOperationException($"Unrecognized event type: {qualifiedName}");
        var name = parts[^1];
        var simple = name.EndsWith("Event", StringComparison.Ordinal) ? name[..^5] : name;
        var kebab = Regex.Replace(simple, "([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant();
        if (root == "Session" && simple == "SessionCompleted") return "session.completed.v1";
        if (root == "Session" && simple == "SessionCancelled") return "session.cancelled.v1";
        return $"{root.ToLowerInvariant()}.{kebab}.v1";
    }
}
