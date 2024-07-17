using Common.Constants;
using Microsoft.Extensions.Hosting;

namespace Common.Extensions
{
    public static class HostEnvironmentExtensions
    {
        public static bool IsLocal(this IHostEnvironment hostEnvironment) =>
            string.Compare(
                hostEnvironment.EnvironmentName,
                EnvironmentNames.Local,
                StringComparison.InvariantCultureIgnoreCase) == 0;

        public static bool IsSandbox(this IHostEnvironment hostEnvironment) =>
            string.Compare(
                hostEnvironment.EnvironmentName,
                EnvironmentNames.Sandbox,
                StringComparison.InvariantCultureIgnoreCase) == 0;
    }
}
