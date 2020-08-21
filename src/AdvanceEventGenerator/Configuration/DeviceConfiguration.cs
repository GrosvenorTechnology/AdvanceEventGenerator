using System;
using System.Collections.Generic;
using System.Text;

namespace AdvanceEventGenerator.Configuration
{
   public class DeviceConfiguration
   {
      public const string Position = "DeviceConfig";
      public List<Device> Devices { get; set; }
   }

   public class Device
   {
      public string SerialNumber { get; set; }
      public string SharedKey { get; set; }
      public List<Portal> Portals { get; set; }
   }

   public class Portal
   {
      public string PortalId { get; set; }
      public List<string> ReaderIds { get; set; }

   }
}
