using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotRush.Server.Services;

public class SolutionService {
    public static SolutionService Instance { get; private set; } = null!;
    public HashSet<string> ProjectFiles { get; private set; }
    public MSBuildWorkspace? Workspace { get; private set; }
    public Solution? Solution => Workspace?.CurrentSolution;
    private string? targetFramework;


    private SolutionService() {
        ProjectFiles = new HashSet<string>();
    }

    public static void Initialize(string[] targets) {
        var queryOptions = VisualStudioInstanceQueryOptions.Default;
        var instances = MSBuildLocator.QueryVisualStudioInstances(queryOptions);
        MSBuildLocator.RegisterInstance(instances.FirstOrDefault());

        Instance = new SolutionService();
        foreach (var target in targets)
            foreach (var path in Directory.GetFiles(target, "*.csproj", SearchOption.AllDirectories)) 
                Instance.ProjectFiles.Add(path);
        
        Instance.ForceReload();
    }

    public void UpdateSolution(Solution? solution) {
        if (solution == null) 
            return;
        Workspace?.TryApplyChanges(solution);
    }
    public void UpdateFramework(string? framework) {
        if (targetFramework == framework) 
            return;
        targetFramework = framework;
        ForceReload();
    }

    public void AddTargets(string[] targets) {
        var added = new List<string>();
        foreach (var target in targets) 
            foreach (var path in Directory.GetFiles(target, "*.csproj", SearchOption.AllDirectories))
                if (ProjectFiles.Add(path))
                    added.Add(path);

        LoadProjects(added);
    }
    public void RemoveTargets(string[] targets) {
        var changed = false;
        foreach (var target in targets) 
            foreach (var path in Directory.GetFiles(target, "*.csproj", SearchOption.AllDirectories))
                changed = ProjectFiles.Remove(path);
        // MSBuildWorkspace does not support unloading projects
        if (changed) ForceReload();
    }
    public void ForceReload() {
        var configuration = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(targetFramework))
            configuration.Add("TargetFramework", targetFramework);

        Workspace = MSBuildWorkspace.Create(configuration);
        Workspace.LoadMetadataForReferencedProjects = true;
        Workspace.SkipUnrecognizedProjects = true;

        LoadProjects(ProjectFiles);
    }


    private void LoadProjects(IEnumerable<string> projectPaths) {
        foreach (var path in projectPaths) {
            try {
                Workspace?.OpenProjectAsync(path).Wait();
                LoggingService.Instance.LogMessage("Add project {0}", path);
            } catch(Exception ex) {
                LoggingService.Instance.LogError(ex.Message, ex);
            }
        }

        UpdateSolution(Workspace?.CurrentSolution);
    }
}