using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SecurityClearanceWebApp.Util
{
    public class Audit
    {
        public string UserName { get; set; }
        public string IPAddress { get; set; }
        public string AreaAccessed { get; set; }
        public DateTime TimesAccessed { get; set; }
    }
}