using System;
using System.Collections.Generic;
using System.Text;

namespace AdvanceEventGenerator.Configuration
{
   public enum BootEncryptionMode
   {
      EncryptionOnly,
      ValidateCertificate
   }

   public class BootConfiguration
   {
      public const string Position = "BootConfig";

      public Boot Boot { get; set; }
   }

   public class Boot
   {
      public string Application { get; set; } = "OemAccess";
      public string DefaultUri { get; set; }
      //public string SharedKey { get; set; }
      public string PlatformConfig { get; set; }
      public ServiceBaseUri[] Services { get; set; } = new ServiceBaseUri[0];
      public BootCustomHeader[] CustomHeaders { get; set; } = new BootCustomHeader[0];
      public BootLogging Logging { get; set; } = null;
      public string[] Features { get; set; } = new string[0];
      public BootEncryptionMode CertificateValidation { get; set; } = BootEncryptionMode.EncryptionOnly;
      public bool AutoRestart { get; set; } = true;

      public static string GenerateSharedKey()
      {
         var rnd = new Random(DateTime.Now.Millisecond);
         var bytes = new byte[32];
         rnd.NextBytes(bytes);
         return Convert.ToBase64String(bytes);
      }
   }

   public sealed class BootLogging
   {
      public bool Enabled = false;
      public bool DebugMessages = false;
      public string Url = "";
   }

   public sealed class BootCustomHeader
   {
      public string Name { get; set; }
      public string Value { get; set; }
   }

   public sealed class ServiceBaseUri
   {
      public string Name { get; set; }
      public string Uri { get; set; }
   }
}
