using System;
using System.Collections.Generic;
using System.Text;

namespace AdvanceEventGenerator.Configuration
{
   public class UserConfiguration
   {
      public const string Position = "UsersConfig";

      public List<User> Users { get; set; }
   }

   public class User
   {
      public string UserId { get; set; }
      public string TokenId { get; set; }
      public string TokenData { get; set; }
   }
}
