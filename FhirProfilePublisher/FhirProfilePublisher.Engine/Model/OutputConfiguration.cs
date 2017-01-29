using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FhirProfilePublisher.Engine
{
    public class OutputOptions
    {
        public string HeaderText { get; set; }
        public string PageTitleSuffix { get; set; }
        public string FooterText { get; set; }
        public string IndexPageHtml { get; set; }
        public string PageTemplate { get; set; }

        public bool ShowEverythingOnOnePage { get; set; } = true;
        public bool ShowResourcesInW5Group { get; set; } 
        public ResourceMaturity[] ListOnlyResourcesWithMaturity { get; set; } 
    }
}
