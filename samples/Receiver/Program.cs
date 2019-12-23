using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace Receiver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                    .ConfigureLogging((hostingContext, logging) =>
                    {
                        var configSectionForLogging = hostingContext.Configuration.GetSection("Logging");
                        if (!string.IsNullOrWhiteSpace(configSectionForLogging["ApplicationInsights:InstrumentationKey"]))
                        {
                            logging.AddApplicationInsights(configSectionForLogging["ApplicationInsights:InstrumentationKey"]?.ToString() ?? "");
                            //assign default logging level if can not find, app is information
                            logging.AddFilter<ApplicationInsightsLoggerProvider>("", Enum.Parse<LogLevel>(configSectionForLogging["LogLevel:Default"] ?? "Information"));
                            //assign default logging level if can not find, Microsoft is warning
                            logging.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", Enum.Parse<LogLevel>(configSectionForLogging["LogLevel:Microsoft"] ?? "Warning"));
                        }
                    });
                });
    }
}
