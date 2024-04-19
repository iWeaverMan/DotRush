using System.Reflection;

namespace DotRush.Roslyn.Tests.Extensions;

public static class TestProjectExtensions {
    public static string TestProjectsDirectory { get; }
    public static string TestSharedProjectsDirectory { get; }
    public static string SingleTargetFramework => "net8.0";
    public static string MultiTargetFramework => "net6.0;net8.0";

    static TestProjectExtensions() {
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        TestProjectsDirectory = Path.GetFullPath(Path.Combine(assemblyLocation, "TestData", "Projects"));
        TestSharedProjectsDirectory = Path.GetFullPath(Path.Combine(assemblyLocation, "TestData", "SharedProjects"));
    }

    public static string CreateConsoleApp(string projectName, string? targetFramework = null, string? projectDirectory = null) {
        targetFramework ??= SingleTargetFramework;
        projectDirectory ??= TestProjectsDirectory;
        var projectPath = CreateProject(@$"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFrameworks>{targetFramework}</TargetFrameworks>
    </PropertyGroup>
</Project>
        ", projectName, projectDirectory);

        CreateDocument(projectPath, "Program.cs", @$"
using System;

namespace {projectName} {{
    class Program {{
        static void Main(string[] args) {{
            Console.WriteLine(""Hello World!"");
        }}
    }}
}}
        ");
        return projectPath;
    }
    public static string CreateClassLib(string projectName, string? targetFramework = null, string? projectDirectory = null) {
        targetFramework ??= SingleTargetFramework;
        projectDirectory ??= TestProjectsDirectory;
        var projectPath = CreateProject(@$"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <OutputType>Library</OutputType>
        <TargetFrameworks>{targetFramework}</TargetFrameworks>
    </PropertyGroup>
</Project>
        ", projectName, projectDirectory);

        CreateDocument(projectPath, "Class1.cs", @$"
using System;

namespace {projectName} {{
    class Class1 {{
        static void Print() {{
            Console.WriteLine(""Hello World!"");
        }}
    }}
}}
        ");
        return projectPath;
    }
    public static string CreateDocument(string projectPath, string documentPath, string documentContent) {
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var documentFullPath = Path.Combine(projectDirectory, documentPath);
        var documentDirectory = Path.GetDirectoryName(documentFullPath)!;
        if (!Directory.Exists(documentDirectory))
            Directory.CreateDirectory(documentDirectory);
        if (File.Exists(documentFullPath))
            File.Delete(documentFullPath);

        File.WriteAllText(documentFullPath, documentContent);
        return documentFullPath;
    }
    public static string CreateProject(string csprojContent, string projectName, string? projectDirectory = null) {
        projectDirectory ??= TestProjectsDirectory;
        string csprojPath = Path.Combine(projectDirectory, projectName, $"{projectName}.csproj");
        Directory.CreateDirectory(Path.Combine(projectDirectory, projectName));

        using var writer = File.CreateText(csprojPath);
        writer.WriteLine(csprojContent);

        return csprojPath;
    }
}