using System.Xml.Linq;

namespace Sapphire.Architecture.Tests;

/// <summary>
/// Builds a directed graph of SDK-style ProjectReference edges by walking the
/// repository from Sapphire.sln. Paths are resolved relative to each .csproj,
/// so the checker does not depend on a machine-specific absolute root.
/// </summary>
internal static class ProjectReferenceGraph
{
    public const string SolutionFileName = "Sapphire.sln";

    public static DirectoryInfo FindRepositoryRoot()
    {
        var start = new DirectoryInfo(AppContext.BaseDirectory);
        for (var current = start; current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, SolutionFileName)))
            {
                return current;
            }
        }

        throw new InvalidOperationException(
            $"Could not locate {SolutionFileName} by walking up from '{AppContext.BaseDirectory}'.");
    }

    public static Dictionary<string, HashSet<string>> Build(DirectoryInfo repositoryRoot)
    {
        var graph = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var csprojFiles = Directory.EnumerateFiles(repositoryRoot.FullName, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !IsGeneratedPath(path, repositoryRoot.FullName));

        foreach (var csprojPath in csprojFiles)
        {
            var from = ProjectKey(csprojPath);
            if (!graph.ContainsKey(from))
            {
                graph[from] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var referencedPath in ReadProjectReferences(csprojPath))
            {
                var to = ProjectKey(referencedPath);
                graph[from].Add(to);
                if (!graph.ContainsKey(to))
                {
                    graph[to] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        return graph;
    }

    public static bool TryFindCycle(
        IReadOnlyDictionary<string, HashSet<string>> graph,
        out IReadOnlyList<string> cycle)
    {
        var state = graph.Keys.ToDictionary(key => key, _ => VisitState.NotVisited, StringComparer.OrdinalIgnoreCase);
        var stack = new List<string>();

        foreach (var node in graph.Keys)
        {
            if (state[node] == VisitState.NotVisited && TryDfs(node, graph, state, stack, out cycle))
            {
                return true;
            }
        }

        cycle = Array.Empty<string>();
        return false;
    }

    /// <summary>
    /// Isolated negative validation of the cycle algorithm. Does not mutate production projects.
    /// </summary>
    public static Dictionary<string, HashSet<string>> CreateSyntheticCycleGraph()
    {
        return new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = ["B"],
            ["B"] = ["C"],
            ["C"] = ["A"]
        };
    }

    private static bool TryDfs(
        string node,
        IReadOnlyDictionary<string, HashSet<string>> graph,
        IDictionary<string, VisitState> state,
        List<string> stack,
        out IReadOnlyList<string> cycle)
    {
        state[node] = VisitState.Visiting;
        stack.Add(node);

        foreach (var neighbor in graph[node])
        {
            if (!state.ContainsKey(neighbor))
            {
                continue;
            }

            if (state[neighbor] == VisitState.Visiting)
            {
                var start = stack.IndexOf(neighbor);
                cycle = stack.Skip(start).Concat([neighbor]).ToArray();
                return true;
            }

            if (state[neighbor] == VisitState.NotVisited &&
                TryDfs(neighbor, graph, state, stack, out cycle))
            {
                return true;
            }
        }

        stack.RemoveAt(stack.Count - 1);
        state[node] = VisitState.Visited;
        cycle = Array.Empty<string>();
        return false;
    }

    private static IEnumerable<string> ReadProjectReferences(string csprojPath)
    {
        var document = XDocument.Load(csprojPath);
        var csprojDirectory = Path.GetDirectoryName(csprojPath)!;

        foreach (var include in document.Descendants()
                     .Where(element => element.Name.LocalName == "ProjectReference")
                     .Select(element => element.Attribute("Include")?.Value)
                     .Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var normalized = include!.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            yield return Path.GetFullPath(Path.Combine(csprojDirectory, normalized));
        }
    }

    private static string ProjectKey(string csprojPath) =>
        Path.GetFileNameWithoutExtension(csprojPath);

    private static bool IsGeneratedPath(string path, string repositoryRoot)
    {
        var relative = Path.GetRelativePath(repositoryRoot, path);
        var parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part =>
            part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("node_modules", StringComparison.OrdinalIgnoreCase));
    }

    private enum VisitState
    {
        NotVisited,
        Visiting,
        Visited
    }
}
