using Merlion.Core.Microservices.EventBus;
using Merlion.Core.Microservices.EventBus.Kafka;
using Merlion.Core.Microservices.EventBus.Kafka.AppSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Receiver.BackgroundServices.EventHandling;

namespace Receiver
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddScoped<SampleEventHandler>();
            services.AddSingleton<IConsumer, KafkaConsumer>();

            // Background service for Integration Event Handlers
            services.AddHostedService<SampleEventHandler>();

            services.Configure<KafkaConfiguration>(Configuration.GetSection(nameof(KafkaConfiguration)));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}