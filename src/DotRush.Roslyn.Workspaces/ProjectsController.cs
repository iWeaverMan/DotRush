using DotRush.Roslyn.Common.Extensions;
using DotRush.Roslyn.Common.External;
using DotRush.Roslyn.Common.Logging;
using DotRush.Roslyn.Workspaces.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using FileSystemExtensions = DotRush.Roslyn.Common.Extensions.FileSystemExtensions;

namespace DotRush.Roslyn.Workspaces;

public abstract class ProjectsController {
    private readonly HashSet<string> projectFilePaths = new HashSet<string>();

    protected abstract bool RestoreProjectsBeforeLoading { get; }
    protected abstract bool CompileProjectsAfterLoading { get; }

    public virtual Task OnLoadingStartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual Task OnLoadingCompletedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public virtual void OnProjectRestoreStarted(string documentPath) {}
    public virtual void OnProjectRestoreCompleted(string documentPath) {}
    public virtual void OnProjectRestoreFailed(string documentPath, ProcessResult result) {}
    public virtual void OnProjectLoadStarted(string documentPath) {}
    public virtual void OnProjectLoadCompleted(string documentPath) {}
    public virtual void OnProjectCompilationStarted(string documentPath) {}
    public virtual void OnProjectCompilationCompleted(string documentPath) {}
    protected abstract void OnWorkspaceStateChanged(Solution newSolution);

    protected void AddProjectFiles(IEnumerable<string> projectPaths, IEnumerable<string>? excludePatterns = null) {
        foreach (var projectPath in projectPaths) {
            if (excludePatterns != null && FileSystemExtensions.CheckGlobPatterns(projectPath, excludePatterns))
                continue;

            projectFilePaths.Add(projectPath);
        }
    }
    protected void RemoveProjectFiles(IEnumerable<string> projectPaths) {
        foreach (var projectPath in projectPaths)
            projectFilePaths.Remove(projectPath);
    }
    protected async Task LoadAsync(MSBuildWorkspace workspace, CancellationToken cancellationToken) {
        CurrentSessionLogger.Debug($"Loading projects: {string.Join(';', projectFilePaths)}");
        await OnLoadingStartedAsync(cancellationToken);

        foreach (var projectFile in projectFilePaths) {
            await SafeExtensions.InvokeAsync(async () => {
                if (RestoreProjectsBeforeLoading) {
                    OnProjectRestoreStarted(projectFile);
                    var result = await workspace.RestoreProjectAsync(projectFile, cancellationToken);
                    if (result.ExitCode != 0)
                        OnProjectRestoreFailed(projectFile, result);
                    OnProjectRestoreCompleted(projectFile);
                }

                OnProjectLoadStarted(projectFile);
                var project = await workspace.OpenProjectAsync(projectFile, null, cancellationToken);
                OnProjectLoadCompleted(projectFile);

                OnWorkspaceStateChanged(workspace.CurrentSolution);

                if (CompileProjectsAfterLoading) {
                    OnProjectCompilationStarted(projectFile);
                    _ = await project.GetCompilationAsync(cancellationToken);
                    OnProjectCompilationCompleted(projectFile);
                }
            });
        }

        await OnLoadingCompletedAsync(cancellationToken);
        CurrentSessionLogger.Debug($"Projects loading completed, loaded {workspace.CurrentSolution.ProjectIds.Count} projects");
    }
}
