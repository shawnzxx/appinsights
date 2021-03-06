﻿using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Sender.Extensions
{
    public static class TelemetryExtensions
    {
        public static void SetActivity(this RequestTelemetry telemetry, Activity actvitiy)
        {
            telemetry.Id = actvitiy.Id;
            telemetry.Context.Operation.Id = actvitiy.RootId;
            telemetry.Context.Operation.ParentId = actvitiy.ParentId;
        }
    }

    public static class DependencyTelemetryExtensions
    {
        public static void SetActivity(this DependencyTelemetry telemetry, Activity actvitiy)
        {
            telemetry.Id = actvitiy.Id;
            telemetry.Context.Operation.Id = actvitiy.RootId;
            telemetry.Context.Operation.ParentId = actvitiy.ParentId;
        }
    }
}
