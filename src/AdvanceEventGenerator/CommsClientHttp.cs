using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AdvanceEventGenerator
{
   public class CommsClientHttp : CommsClient
   {
      private readonly HttpClient _httpClient;
      public CommsClientHttp(OemMessageHandler oemMessageHandler) : base(oemMessageHandler)
      {
         _httpClient = new HttpClient(oemMessageHandler, false)
         {
            Timeout = TimeSpan.FromSeconds(10)
         };
      }

      public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage)
      {
         return _httpClient.SendAsync(httpRequestMessage);
      }

      public override Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content)
      {
         return _httpClient.PostAsync(uri, content);
      }

      public override Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content, CancellationToken cancellationToken)
      {
         return _httpClient.PostAsync(uri, content, cancellationToken);
      }

      public override Task<HttpResponseMessage> DeleteAsync(string uri)
      {
         return _httpClient.DeleteAsync(uri);
      }
   }

   public abstract class CommsClient
   {
      private readonly OemMessageHandler _oemMessageHandler;

      protected CommsClient(OemMessageHandler oemMessageHandler)
      {
         _oemMessageHandler = oemMessageHandler;
      }

      public abstract Task<HttpResponseMessage> SendAsync(HttpRequestMessage httpRequestMessage);
      public abstract Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content);
      public abstract Task<HttpResponseMessage> PostAsync(Uri uri, HttpContent content, CancellationToken cancellationToken);
      public abstract Task<HttpResponseMessage> DeleteAsync(string uri);
   }
}