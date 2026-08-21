using Xunit;

namespace Sapphire.Architecture.Tests;

public class ProjectReferenceCycleTests
{
    [Fact]
    public void Repository_project_references_do_not_form_a_cycle()
    {
        var graph = BuildGraph();
        Assert.True(graph.Count >= 7, $"Expected to discover production and test projects, found {graph.Count}.");

        var foundCycle = ProjectReferenceGraph.TryFindCycle(graph, out var cycle);
        Assert.False(
            foundCycle,
            foundCycle
                ? $"Circular ProjectReference detected: {string.Join(" -> ", cycle)}"
                : string.Empty);
    }

    [Fact]
    public void Domain_projects_do_not_reach_higher_layers()
    {
        var graph = BuildGraph();

        foreach (var service in ArchitectureAssemblies.Services)
        {
            AssertCannotReachAny(
                graph,
                $"{service}.Domain",
                $"{service}.Application",
                $"{service}.Infrastructure",
                $"{service}.Api");
        }
    }

    [Fact]
    public void Application_projects_do_not_reach_infrastructure_or_api()
    {
        var graph = BuildGraph();

        foreach (var service in ArchitectureAssemblies.Services)
        {
            AssertCannotReachAny(
                graph,
                $"{service}.Application",
                $"{service}.Infrastructure",
                $"{service}.Api");
        }
    }

    [Fact]
    public void SharedKernel_project_does_not_reach_service_projects()
    {
        var graph = BuildGraph();

        AssertCannotReachAny(
            graph,
            "Sapphire.Shared.Kernel",
            "Sapphire.Auth.Domain",
            "Sapphire.Auth.Application",
            "Sapphire.Auth.Infrastructure",
            "Sapphire.Auth.Api",
            "Sapphire.Billing.Domain",
            "Sapphire.Billing.Application",
            "Sapphire.Billing.Infrastructure",
            "Sapphire.Billing.Api",
            "Sapphire.Session.Domain",
            "Sapphire.Session.Application",
            "Sapphire.Session.Infrastructure",
            "Sapphire.Session.Api");
    }

    [Fact]
    public void Service_projects_do_not_reach_other_services()
    {
        var graph = BuildGraph();

        foreach (var sourceService in ArchitectureAssemblies.Services)
        {
            var sourceProjects = graph.Keys
                .Where(project => project.StartsWith(sourceService + ".", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var forbiddenServices = ArchitectureAssemblies.Services
                .Where(service => !service.Equals(sourceService, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var sourceProject in sourceProjects)
            {
                var reachable = ReachableFrom(graph, sourceProject);
                var forbidden = reachable
                    .Where(project => forbiddenServices.Any(service =>
                        project.StartsWith(service + ".", StringComparison.OrdinalIgnoreCase)))
                    .ToArray();

                Assert.Empty(forbidden);
            }
        }
    }

    [Fact]
    public void Cycle_detector_fails_on_a_synthetic_cycle()
    {
        var synthetic = ProjectReferenceGraph.CreateSyntheticCycleGraph();
        var foundCycle = ProjectReferenceGraph.TryFindCycle(synthetic, out var cycle);

        Assert.True(foundCycle, "Negative validation helper must detect A -> B -> C -> A.");
        Assert.Contains("A", cycle);
        Assert.Contains("B", cycle);
        Assert.Contains("C", cycle);
    }

    private static Dictionary<string, HashSet<string>> BuildGraph()
    {
        var root = ProjectReferenceGraph.FindRepositoryRoot();
        Assert.True(File.Exists(Path.Combine(root.FullName, ProjectReferenceGraph.SolutionFileName)));
        return ProjectReferenceGraph.Build(root);
    }

    private static void AssertCannotReachAny(
        IReadOnlyDictionary<string, HashSet<string>> graph,
        string source,
        params string[] forbiddenTargets)
    {
        Assert.True(graph.ContainsKey(source), $"Project '{source}' was not discovered in the ProjectReference graph.");

        var reachable = ReachableFrom(graph, source);
        var forbidden = forbiddenTargets.Where(target => reachable.Contains(target)).ToArray();
        Assert.Empty(forbidden);
    }

    private static HashSet<string> ReachableFrom(IReadOnlyDictionary<string, HashSet<string>> graph, string source)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<string>();
        stack.Push(source);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current) || !graph.TryGetValue(current, out var neighbors))
            {
                continue;
            }

            foreach (var neighbor in neighbors)
            {
                stack.Push(neighbor);
            }
        }

        visited.Remove(source);
        return visited;
    }
}
