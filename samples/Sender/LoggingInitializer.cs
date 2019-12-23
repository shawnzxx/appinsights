using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Sender
{
    public class LoggingInitializer : ITelemetryInitializer
    {
        readonly string _roleName;
        public LoggingInitializer(string roleName = null)
        {
            this._roleName = roleName ?? "api";
        }
        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = _roleName;
        }
    }
}
