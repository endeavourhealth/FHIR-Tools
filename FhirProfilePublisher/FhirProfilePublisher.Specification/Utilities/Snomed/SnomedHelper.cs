using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FhirProfilePublisher.Engine
{
    public static class SnomedHelper
    {
        public static bool IsSnomedSystemUri(string uri)
        {
            return uri.ToLower().Contains("snomed.info/sct");
        }

        public static string GetBrowserUrl()
        {
            return GetBrowserUrl("138875005");
        }

        public static string GetBrowserUrl(string conceptId)
        {
            string release = "v20161001";
            string langRefSet = "900000000000508004";
        
            return string.Format("http://browser.ihtsdotools.org/?perspective=full&conceptId1={0}&edition=uk-edition&acceptLicense=true&release={1}&server=https://prod-browser-exten.ihtsdotools.org/api/snomed&langRefset={2}", conceptId, release, langRefSet);
        }
    }
}
