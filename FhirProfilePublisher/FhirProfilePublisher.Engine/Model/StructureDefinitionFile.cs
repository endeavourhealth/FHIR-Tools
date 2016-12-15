using Hl7.Fhir.V102;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FhirProfilePublisher.Specification;

namespace FhirProfilePublisher.Engine
{
    internal class StructureDefinitionFile : ResourceFile
    {
        public StructureDefinitionFile(string xml)
        {
            Xml = xml;
            StructureDefinition = XmlHelper.Deserialize<StructureDefinition>(xml);
            Json = JsonConverter.Serialize(StructureDefinition);
        }

        public StructureDefinition StructureDefinition { get; private set; }

        public override OutputFileType FileType
        { 
            get { return OutputFileType.StructureDefinition; } 
        }

        public override string Name 
        { 
            get { return StructureDefinition.GetName(); }
        }

        public override string CanonicalUrl
        {
            get { return StructureDefinition.url.value; }
        }

        public override string OutputHtmlFilename
        {
            get { return OutputFilenameRoot + (StructureDefinition.IsExtension() ? ".extension" : string.Empty) + "." + HtmlExtension; }
        }

        public override string OutputXmlFilename
        {
            get { return OutputFilenameRoot + "." + XmlExtension; }
        }

        public override string OutputJsonFilename
        {
            get { return OutputFilenameRoot + "." + JsonExtension; }
        }

        private string OutputFilenameRoot
        {
            get
            {
                if (StructureDefinition.id != null)
                    if (!string.IsNullOrWhiteSpace(StructureDefinition.id.value))
                        return StructureDefinition.id.value;

                return StructureDefinition.name.value;
            }
        }

        public override ResourceMaturity Maturity
        {
            get 
            { 
                string resourceMaturity = StructureDefinition.GetExtensionValueAsString(FhirConstants.ResourceMaturityExtensionUrl);

                int result = 0;
                int.TryParse(resourceMaturity, out result);
                
                return (ResourceMaturity)result; 
            }
        }

        public override string VersionNumber
        {
            get
            {
                string versionNumber = StructureDefinition.version.WhenNotNull(t => t.value);

                if (string.IsNullOrWhiteSpace(versionNumber))
                    return "1.0";

                return versionNumber;
            }
        }
    }
}
