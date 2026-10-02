using Cake.Common.Tools.VSTest;
using Cake.Core.IO;
using Cake.Frosting;
using System;
using System.Diagnostics;
using System.IO;

namespace Build.Tasks.Standard;

[TaskName("Test")]
[IsDependentOn(typeof(CompileProjectsTask))]
[TaskDescription("Runs the net481 MSTest suite after building, and fails if tests fail or none are discovered.")]
public sealed class TestTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        string project = Path.Combine(context.SourceDirectory, "DotNetFrameworkToolkit.Tests");
        string assembly = Path.Combine(project, "bin", context.Config.ToString(), "DotNetFrameworkToolkit.Tests.dll");
        string results = Path.Combine(context.AbsolutePathToRepo, "artifacts", "test-results", context.Config.ToString());
        if (!File.Exists(assembly))
        {
            throw new FileNotFoundException("The test assembly was not built.", assembly);
        }

        Directory.CreateDirectory(results);
        context.VSTest(new[] { new FilePath(assembly) }, new VSTestSettings
        {
            ToolPath = LocateTestRunner(),
            TestAdapterPath = new DirectoryPath(Path.GetDirectoryName(assembly)!),
            SettingsFile = new FilePath(Path.Combine(project, "tests.runsettings")),
            ResultsDirectory = new DirectoryPath(results)
        }.WithLogger("trx;LogFileName=toolkit.trx"));
    }

    private static FilePath LocateTestRunner()
    {
        string vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (!File.Exists(vswhere))
        {
            throw new FileNotFoundException("Install Visual Studio with the test tools to run the net481 suite.", vswhere);
        }

        using Process process = Process.Start(new ProcessStartInfo(vswhere, "-latest -products * -find Common7/IDE/Extensions/TestPlatform/vstest.console.exe")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        })!;
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        string runner = output.Trim();
        if (process.ExitCode != 0 || !File.Exists(runner))
        {
            throw new InvalidOperationException("Visual Studio's vstest.console.exe was not found. Install the test tools workload.");
        }

        return new FilePath(runner);
    }
}
