using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Strider.Agent
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(config =>
                {
                    if (args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureServices((host, services) =>
                {
                    services.AddLogging(options => options.AddConsole());
                })
                .ConfigureAppConfiguration((context, builder) =>
                {
                    // builder.SetBasePath(AppContext.BaseDirectory)
                    //     .AddJsonFile("appsettings.json", false)
                    //     .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true);
                });
        }
    }
    }
}