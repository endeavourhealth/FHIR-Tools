using Hl7.Fhir.V102;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FhirProfilePublisher.Specification;

// Kevin Mayfield Leeds Teaching Trust 23/1/2017

namespace FhirProfilePublisher.Engine
{
    internal class ConceptMapFile : ResourceFile
    {
        public ConceptMapFile(string xml)
        {
            Xml = xml;
            ConceptMap = XmlHelper.Deserialize<ConceptMap>(xml);
            Json = JsonConverter.Serialize(ConceptMap);
        }

        public ConceptMap ConceptMap { get; private set; }

        public override OutputFileType FileType
        {
            get { return OutputFileType.ConceptMap; }
        }

        public override string Name
        {
            get { return ConceptMap.name.value; }
        }

        public override string CanonicalUrl 
        {
            get {
                if (ConceptMap.url != null)
                    return ConceptMap.url.value;
                else
                    return "";
            }
        }

        public override string OutputHtmlFilename
        {
            get { return OutputFilenameRoot + ".conceptmap." + HtmlExtension; }
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
                if (ConceptMap.id != null)
                    if (!string.IsNullOrWhiteSpace(ConceptMap.id.value))
                        return ConceptMap.id.value;

                return ConceptMap.name.value;
            }
        }

        public override ResourceMaturity Maturity
        {
            get 
            {
                string resourceMaturity = ConceptMap.GetExtensionValueAsString(FhirConstants.ResourceMaturityExtensionUrl);

                int result = 0;
                int.TryParse(resourceMaturity, out result);

                return (ResourceMaturity)result;
            }
        }
        public override string VersionNumber
        {
            get
            {
                string versionNumber = ConceptMap.version.WhenNotNull(t => t.value);

                if (string.IsNullOrWhiteSpace(versionNumber))
                    return "1.0";

                return versionNumber;
            }
        }
    }
}
