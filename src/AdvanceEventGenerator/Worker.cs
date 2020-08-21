using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdvanceEventGenerator.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdvanceEventGenerator
{
   public class Worker : BackgroundService
   {
      private readonly ILogger<Worker> _logger;
      private readonly IOptions<BootConfiguration> _bootOptions;
      private readonly IOptions<DeviceConfiguration> _deviceOptions;
      private readonly IOptions<UserConfiguration> _userOptions;
      private readonly IOptions<RequestConfiguration> _requesteOptions;

      private readonly Random _random;

      public Worker(ILogger<Worker> logger,
         IOptions<BootConfiguration> bootOptions,
         IOptions<DeviceConfiguration> deviceOptions,
         IOptions<UserConfiguration> userOptions,
         IOptions<RequestConfiguration> requesteOptions)
      {
         _logger = logger;
         _bootOptions = bootOptions;
         _deviceOptions = deviceOptions;
         _userOptions = userOptions;
         _requesteOptions = requesteOptions;

         _random = new Random(DateTime.UtcNow.Millisecond);
      }

      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {
         var tasks = new List<Task>();

         foreach (var device in _deviceOptions.Value.Devices)
         {
            var task = DoWork(stoppingToken, device);
            tasks.Add(task);
         }

         await Task.WhenAll(tasks.ToArray());
      }


      private async Task DoWork(CancellationToken stoppingToken, Device device)
      {
         var client = new CommsClientHttp(new OemMessageHandler(device, _bootOptions.Value, _logger, "v1"));

         while (!stoppingToken.IsCancellationRequested)
         { 
            // Select some data at random for this event
            var user = GetRandomUser();
            var portal = GetRandomPortal(device);
            var readerId = GetRandomReader(portal);

            // Details on Read event: https://github.com/GrosvenorTechnology/OemAccessApi/blob/master/documentation/Entities/HardwareReader.md#read
            var evt = new Event
            {
               MessageId = Guid.NewGuid(),
               CorrelationId = Guid.NewGuid(),
               PreviousMessageId = null,
               TimeStamp = DateTimeOffset.Now,
               Entity = $"Hardware.Reader:{readerId}",
               EventName = "Read",
               Contents =
               {
                  { "PersonId", $"AccessControl.User:{user.UserId}" },
                  { "TokenId", $"AccessControl.Token:{user.TokenId}" },
                  { "TokenData", user.TokenData },
                  { "Result", "Success" }
               }
            };
            await SendMessage(evt, device, client, stoppingToken);
            await Task.Delay(500);

            // Details on PortalUsed event: https://github.com/GrosvenorTechnology/OemAccessApi/blob/master/documentation/Entities/AccessControlPortal.md#portalentryusedportalexitused
            evt = new Event
            {
               MessageId = Guid.NewGuid(),
               CorrelationId = Guid.NewGuid(),
               PreviousMessageId = null,
               TimeStamp = DateTimeOffset.Now,
               Entity = $"AccessControl.Portal:{portal.PortalId}",
               EventName = "PortalEntryUsed",
               Contents =
               {
                  { "PersonId", $"AccessControl.User:{user.UserId}" },
                  { "TokenId", $"AccessControl.Token:{user.TokenId}" },
                  { "TokenData", user.TokenData },
                  { "ReaderId", $"Hardware.Reader:{readerId}" },
                  { "Result", "Success" }
               }
            };
            await SendMessage(evt, device, client, stoppingToken);

            //Wait before repeating, add some random jitter to the requests so they're not all happening at the same time.
            await Task.Delay(_requesteOptions.Value.RequestDelayMs + _random.Next(0, _requesteOptions.Value.JitterMs) , stoppingToken);
         }
      }

      private async Task<bool> SendMessage(Event msg, Device device, CommsClient client, CancellationToken token)
      {
         if (token.IsCancellationRequested)
         {
            return false;
         }

         try
         {
            var sw = Stopwatch.StartNew();
            var stringContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(msg), Encoding.UTF8, "application/json");
            var uri = ExpandUri("device/{deviceSerial}/events", device.SerialNumber);

            var response = await client.PostAsync(uri, stringContent, token);
            if (!response.IsSuccessStatusCode)
            {
               _logger.LogWarning($"[{device.SerialNumber}] Request Failed: {response.StatusCode} : {response.ReasonPhrase}");
               return false;
            }
            _logger.LogDebug($"[{device.SerialNumber}] message transmitted: {sw.ElapsedMilliseconds}ms");
            return true;
         }
         catch (Exception err)
         {
            _logger.LogError($"[{device.SerialNumber}] Request failed {err.Message}");
            //Keep going regardless of any error
         }
         return false;
      }


      private User GetRandomUser()
      {
         return _userOptions.Value.Users[_random.Next(0, _userOptions.Value.Users.Count)];
      }

      private Portal GetRandomPortal(Device device)
      {
         return device.Portals[_random.Next(0, device.Portals.Count)];
      }

      private string GetRandomReader(Portal portal)
      {
         return portal.ReaderIds[_random.Next(0, portal.ReaderIds.Count)];
      }

      private Uri ExpandUri(string uri, string serialNumber)
      {
         uri = ReplaceTokensInUri(uri, serialNumber);
         if (Uri.IsWellFormedUriString(uri, UriKind.Relative))
         {
            uri = $"{_bootOptions.Value.Boot.DefaultUri}/{uri}";
         }
         return new Uri(uri);
      }

      private string ReplaceTokensInUri(string str, string serialNumber)
      {
         foreach (var serviceUri in _bootOptions.Value.Boot.Services)
         {
            str = str.Replace($"{{{serviceUri.Name}}}", serviceUri.Uri);
         }
         return str.Replace("{deviceSerial}", serialNumber);
      }

   }
}
