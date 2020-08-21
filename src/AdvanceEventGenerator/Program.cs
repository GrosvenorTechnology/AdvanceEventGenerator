using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using AdvanceEventGenerator.Configuration;

namespace AdvanceEventGenerator
{
   class Program
   {
      static void Main(string[] args)
      {
         CreateHostBuilder(args).Build().Run();
      }

      public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)   
            .ConfigureAppConfiguration((hostContext, config) =>
            {
               config.AddJsonFile("/config/users.json", optional: true, reloadOnChange: false);
               config.AddJsonFile("/config/devices.json", optional: true, reloadOnChange: false);
            })
            .ConfigureServices((hostContext, services) =>
              {
                 services.Configure<BootConfiguration>(hostContext.Configuration.GetSection(BootConfiguration.Position));
                 services.Configure<DeviceConfiguration>(hostContext.Configuration.GetSection(DeviceConfiguration.Position));
                 services.Configure<RequestConfiguration>(hostContext.Configuration.GetSection(RequestConfiguration.Position));
                 services.Configure<UserConfiguration>(hostContext.Configuration.GetSection(UserConfiguration.Position));

                  services.AddHostedService<Worker>();
              });
   }
}
