using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Improbable.Gdk.Tools;

namespace Improbable.Gdk.DeploymentManager.Commands
{
    public static class Assembly
    {
        public static WrappedTask<RedirectedProcessResult, AssemblyConfig> UploadAsync(string projectName, AssemblyConfig config)
        {
            var source = new CancellationTokenSource();
            var token = source.Token;

            var args = new List<string>
            {
                "cloud",
                "upload",
                config.AssemblyName,
                "--project_name",
                projectName,
                "--json_output"
            };

            if (config.ShouldForceUpload)
            {
                args.Add("--force");
            }

            var task = Task.Run(async () => await RedirectedProcess.Command(Tools.Common.SpatialBinary)
                .InDirectory(Tools.Common.SpatialProjectRootDir)
                .WithArgs(args.ToArray())
                .RedirectOutputOptions(OutputRedirectBehaviour.RedirectStdOut |
                    OutputRedirectBehaviour.RedirectStdErr | OutputRedirectBehaviour.ProcessSpatialOutput)
                .RunAsync(token));

            return new WrappedTask<RedirectedProcessResult, AssemblyConfig>
            {
                Task = task,
                CancelSource = source,
                Context = config.DeepCopy()
            };
        }
    }
}