using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Constants
{
    public static class AppConstants
    {
        public const string IdentityServiceSection = "IdentityService";
        public const string RedisSection = "Redis";
        public const string OpenTelemetrySection = "OpenTelemetry";
        public const string WorkerOptionsSection = "WorkerOptions";

        public const string PolicyWrite = "ai.modelrunner.write";
        public const string PolicyRead = "ai.modelrunner.read";
        public const string PolicyAdmin = "ai.modelrunner.admin";
        
        #region IDS Scopes
        public const string ScopeAdmin = "ai.modelrunner.admin";
        public const string ScopeRead = "ai.modelrunner.read";
        public const string ScopeWrite = "ai.modelrunner.write";

        public const string UserClientId = "ai.modelrunner";
        #endregion
    }
}
