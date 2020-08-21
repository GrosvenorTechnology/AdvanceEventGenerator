using AdvanceEventGenerator.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdvanceEventGenerator
{
   public class OemMessageHandler : HttpClientHandler
   {
      private readonly string _deviceSerial;
      private readonly BootConfiguration _config;
      private readonly byte[] _psk;
      private static readonly DateTime EpochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
      private readonly string _version;
      private readonly ILogger _logger;


      public OemMessageHandler(Device device, BootConfiguration config, ILogger logger, string appVersion)
      {
         _version = $"OEM-Access:v{appVersion}";
         _deviceSerial = device.SerialNumber;
         _config = config;
         _logger = logger;
         _psk = Convert.FromBase64String(device.SharedKey);

         // Check if certificate validation is required.
         if (config.Boot.CertificateValidation == BootEncryptionMode.EncryptionOnly)
         {
            try
            {
               ServerCertificateCustomValidationCallback = (sender, certificate, chain, errors) => true;
            }
            catch (NotImplementedException)
            {
               _logger.LogWarning("HttpClientHandler.ServerCertificateCustomValidationCallback is not implemented on this version of Mono");
            }
         }

      }

      private readonly TimeSpan _timeout = TimeSpan.FromSeconds(12);
      private readonly List<Task> _outstandingTaskList = new List<Task>();

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
      {
         foreach (var task in _outstandingTaskList.ToArray())
         {
            if (task.IsCompleted)
            {
               _outstandingTaskList.Remove(task);
            }
         }

         if (_outstandingTaskList.Count >= 20)
         {
            _logger.LogWarning($"Prevented due to time-out pile-up: [{request.Method}] {request.RequestUri.AbsoluteUri}");
            throw new TaskCanceledException("Prevented due to time-out pile-up");
         }

         request.Headers.Add("x-gtl-oem-device-serial", _deviceSerial);
         request.Headers.Add("x-gtl-oem-client-request-id", Guid.NewGuid().ToString());
         request.Headers.Add("x-gtl-oem-client-session-id", Guid.NewGuid().ToString());
         request.Headers.Add("x-gtl-oem-client-application-name", _version);

         foreach (var header in _config.Boot.CustomHeaders ?? Enumerable.Empty<BootCustomHeader>())
         {
            request.Headers.Add(header.Name, header.Value);
         }

         var timeSpan = DateTime.UtcNow - EpochStart;
         var timestamp = Convert.ToUInt64(timeSpan.TotalSeconds).ToString();
         var nonce = Guid.NewGuid().ToString("N");
         var requestUri = request.RequestUri.AbsoluteUri;
         var contentLength = request.Content?.ReadAsStreamAsync().Result.Length ?? 0;
         var contentType = request.Content?.Headers.ContentType.ToString() ?? "no-content";

         var signatureRawData = $"{request.Method}{contentLength}{contentType}{nonce}{timestamp}{requestUri}";
         var signature = Encoding.UTF8.GetBytes(signatureRawData);
         using (var hmac = new HMACSHA256(_psk))
         {
            var signatureBytes = hmac.ComputeHash(signature);
            var requestSignatureBase64String = Convert.ToBase64String(signatureBytes);
            request.Headers.Authorization = new AuthenticationHeaderValue("amx", $"{_deviceSerial}:{requestSignatureBase64String}:{nonce}:{timestamp}");
         }

         _logger.LogTrace($"Sending: [{request.Method}] {requestUri}");

         using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
         {
            cts.CancelAfter(_timeout);

            var task = base.SendAsync(request, cts.Token);

            if (Task.WaitAny(new Task[] { task }, _timeout) == -1)
            {
               _outstandingTaskList.Add(task);
               cts.Cancel();
               _logger.LogWarning($"Timed-out: [{request.Method}] {requestUri}. Outstanding tasks: {_outstandingTaskList.Count}");
               throw new TaskCanceledException("Request Timeout (override)");
            }

            task.ContinueWith(t =>
                {
                   if (!t.IsFaulted)
                   {
                      _logger.LogTrace($"Complete: [{request.Method} - {t.Result.StatusCode}({(int)t.Result.StatusCode})] {requestUri}");
                   }
                   else
                   {
                      var sb = new StringBuilder();
                      var ex = t.Exception?.InnerException;
                      while (ex != null)
                      {
                         sb.AppendLine();
                         sb.Append(ex.Message);
                         ex = ex.InnerException;
                      }

                      _logger.LogWarning($"Failed: [{request.Method}] {requestUri}{sb}");
                   }
                }
                , cts.Token);
            return task;
         }
      }
   }
}
