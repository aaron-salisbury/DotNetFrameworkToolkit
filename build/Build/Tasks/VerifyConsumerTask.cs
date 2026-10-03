using Cake.Frosting;
using System;
using System.Diagnostics;
using System.IO;

namespace Build.Tasks;

[TaskName("Verify Consumer")]
[IsDependentOn(typeof(VerifyPackageTask))]
[TaskDescription("Installs the validated NuGet package and runs x86/x64 net20 consumers on CLR 2.0.")]
public sealed class VerifyConsumerTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
        ProcessStartInfo start = new ProcessStartInfo(powershell)
        {
            UseShellExecute = false,
            WorkingDirectory = context.AbsolutePathToRepo
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-ExecutionPolicy");
        start.ArgumentList.Add("Bypass");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(Path.Combine(context.AbsolutePathToRepo, "build", "verify-consumer.ps1"));
        using (Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start consumer verification."))
        {
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Consumer installation, compilation or CLR 2.0 execution failed.");
            }
        }
    }
}
