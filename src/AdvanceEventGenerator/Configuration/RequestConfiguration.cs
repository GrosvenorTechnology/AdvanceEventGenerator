using System;
using System.Collections.Generic;
using System.Text;

namespace AdvanceEventGenerator.Configuration
{
   public class RequestConfiguration
   {
      public const string Position = "Requests";

      public int RequestDelayMs { get; set; }
      public int JitterMs { get; set; }
   }
}
