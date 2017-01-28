using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FhirProfilePublisher.Specification;

// Kevin Mayfield Leeds Teaching Trust 23/1/2017

namespace Hl7.Fhir.V102
{
    public partial class ConceptMap
    {
        public string GetExtensionValueAsString(string extensionUrl)
        {
            if (extension == null)
                return null;

            return extension
                .FirstOrDefault(t => t.url == extensionUrl)
                .WhenNotNull(t => t.Item.WhenNotNull(s => s.GetValueAsString()));
        }
    }
}
