using Hl7.Fhir.V102;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FhirProfilePublisher.Specification;

// Kevin Mayfield Leeds Teaching Trust 23/1/2017 copied from ValueSetHtmlGenerator and converted for basic ConceptMap

namespace FhirProfilePublisher.Engine
{
    internal class ConceptMapHtmlGenerator
    {
        private const string _htmlExtension = "html";
        private ResourceFileSet _profileSet;
        private OutputPaths _outputPaths;
        private Dictionary<ConceptMap, string> _fileNames = new Dictionary<ConceptMap, string>();

        public ConceptMapHtmlGenerator(ResourceFileSet profileSet, OutputPaths outputPaths)
        {
            if (profileSet == null)
                throw new ArgumentNullException("profileSet");

            if (outputPaths == null)
                throw new ArgumentNullException("outputPaths");

            _profileSet = profileSet;
            _outputPaths = outputPaths;
        }

        public void GenerateAll()
        {
            foreach (ConceptMapFile conceptMapFile in _profileSet.ConceptMapFiles)
                Generate(conceptMapFile);
        }

        public void Generate(ConceptMapFile conceptMapFile)
        {
            if (!_profileSet.ConceptMapFiles.Contains(conceptMapFile))
                throw new ArgumentException("ConceptMap does not exist in FhirXmlProfileSet", "conceptMap");

            string html = GenerateHtml(conceptMapFile);

            _outputPaths.WriteUtf8File(OutputFileType.Html, conceptMapFile.OutputHtmlFilename, html);
        }

        private string GenerateHtml(ConceptMapFile conceptMapFile)
        {
            string displayName = conceptMapFile.ConceptMap.name.value;

            object[] content = GenerateContent(conceptMapFile, displayName);

            string contentHtml = Html.Div(content.ToArray()).ToString(SaveOptions.DisableFormatting);

            return Pages.Instance.GetPage(displayName, contentHtml, "0.1", DateTime.Now);
        }

        private object[] GenerateContent(ConceptMapFile conceptMapFile, string displayName)
        {
            List<object> content = new List<object>();

            ConceptMap conceptMap = conceptMapFile.ConceptMap;

            string name = conceptMap.name.value;
            
            string url = "";
            if (conceptMap.url != null)
                url = conceptMap.url.value;

            content.AddRange(new object[]
            {
                Html.H3(GetConceptMapNameLabel(name)),
                Html.P("The official URL for this value set is: "),
                Html.Pre(url),
                Html.P(GetMaturity(conceptMapFile.Maturity)),
            });

            string description = conceptMap.description.WhenNotNull(t => t.value);
            string referenceUrl = conceptMap.GetExtensionValueAsString(FhirConstants.ValueSetSourceReferenceExtensionUrl);
            string oid = conceptMap.GetExtensionValueAsString(FhirConstants.ValueSetOidExtensionUrl);

            if ((!string.IsNullOrWhiteSpace(description)) || (!string.IsNullOrWhiteSpace(referenceUrl)) || (!string.IsNullOrWhiteSpace(oid)))
            {
                content.AddRange(new object[]
                {
                    Html.H3("Description"),
                    GetFormattedDescription(description, referenceUrl, oid)
                });
            }

            content.AddRange(new object[]
            {
                Html.H3("Definition"),
                GenerateDefinition(conceptMap)
            });

        
            string copyright = conceptMap.copyright.WhenNotNull(t => t.value);

            if (!string.IsNullOrEmpty(copyright))
            {
                content.AddRange(new object[]
                {
                    Html.H3("Copyright"),
                    Html.P(copyright)
                });
            }

            content.AddRange(new object[]
            {
                Html.H3("Schemas"),
                StructureDefinitionHtmlGenerator.GetSchemasList(new object[]
                {
                    Html.A(_outputPaths.GetRelativePath(OutputFileType.ConceptMap, conceptMapFile.OutputXmlFilename), "ConceptMap XML"),
                    Html.A(_outputPaths.GetRelativePath(OutputFileType.ConceptMap, conceptMapFile.OutputJsonFilename), "ConceptMap JSON")
                })
            });

            return content.ToArray();
        }

        private object[] GetFormattedDescription(string description, string referenceUrl, string oid)
        {
            List<object> result = (description ?? string.Empty)
                .Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => (object)Html.P(t))
                .ToList();

            List<object> table = new List<object>();
            
            if (!string.IsNullOrWhiteSpace(referenceUrl))
            {
                table.Add(Html.Tr(new object[]
                {
                    Html.Td(Html.I("Reference")),
                    Html.Td(Html.A(new Link(referenceUrl, referenceUrl)))
                }));
            }

            if (!string.IsNullOrWhiteSpace(oid))
            {
                table.Add(Html.Tr(new object[]
                {
                    Html.Td(Html.I("OID")),
                    Html.Td(Html.A(new Link(GetOidLink(oid), oid)))
                }));
            }

            if (table.Count > 0)
                result.Add(Html.Table(new object[] { table, Html.Class(Styles.ValueSetReferenceTableClassName) }));

            return result.ToArray();
        }

        private string GetOidLink(string oid)
        {
            return "http://www.hl7.org.uk/version3group/downloads/OidRootHl7UKonly.html#oid_" + oid.Replace("urn:oid:", "");
        }

        private object[] GetMaturity(ResourceMaturity maturity)
        {
            return new object[]
            {
                ResourceMaturityHelper.GetMaturityLabel() + ": ",
                Html.Img(_outputPaths.GetRelativePath(OutputFileType.Image, maturity.GetAssociatedIcon())),
                " ",
                maturity.GetDescription(),
                "."
            };
        }

        private object[] GetConceptMapNameLabel(string name)
        {
            return new object[] { BootstrapHtml.Label("Concept map"), " ", name };
        }

        private object[] GetLabelAndValue(string labelText, object value)
        {
            return new object[]
            {
                BootstrapHtml.Label(labelText),
                " ",
                value
            };
        }
        
        private object[] GenerateDefinition(ConceptMap conceptMap)
        {
            List<object> result = new List<object>();
            List<object> table = new List<object>();

            
            table.Add(Html.Tr(new object[]
                {
                    Html.Td(Html.B("Source")),
                    Html.Td(""),
                    Html.Td(Html.B("Target")),
                    Html.Td("")
                }
            ));
            ValueSet sourceValueSet = null;
            ValueSet targetValueSet = null;


            if (conceptMap.Item != null && conceptMap.Item.GetType() == typeof(Reference))
            {
                // Cheating a little here at the mo. Not safe
                Reference source = (Reference) conceptMap.Item;
                Reference target = (Reference) conceptMap.Item1;

                Link sourceValuesetLink = _profileSet.GetValueSetLink(source.reference.GetValueAsString());
                Link targetValuesetLink = _profileSet.GetValueSetLink(target.reference.GetValueAsString());
                sourceValueSet = _profileSet.GetValueSet(source.reference.GetValueAsString());
                targetValueSet = _profileSet.GetValueSet(target.reference.GetValueAsString());

                table.Add(Html.Tr(new object[]
                {
                    Html.Td(Html.P(GetLabelAndValue("Valueset", new object[]
                    {
                        Html.A(sourceValuesetLink.Url, sourceValuesetLink.Display)
  
                    }))),
                    Html.Td(""),
                    Html.Td(Html.P(GetLabelAndValue("Valueset", new object[]
                    {
                        Html.A(targetValuesetLink.Url, targetValuesetLink.Display)
                    }))),
                    Html.Td("")
                }
            ));
            }
            
            if (conceptMap.element != null && sourceValueSet != null && targetValueSet !=null)
            {
                    
                    foreach (ConceptMapElement element in conceptMap.element)
                    {
                        string sourceCode = element.code.GetValueAsString(); // GetExtensionValueAsString(FhirConstants.ValueSetSystemNameExtensionUrl);
                        string sourceName = "";
                        foreach (ValueSetInclude include in sourceValueSet.compose.include)
                        {
                            foreach (ValueSetConcept1 concept in include.concept)
                            {
                                if (concept.code.value.Equals(sourceCode))
                                {
                                    sourceName = concept.display.GetValueAsString();
                                }
                            }
                        }
                        if (element.code.WhenNotNull(t => t.ToString().Length) > 0)
                        {
                            if (element.target != null)
                            {
                                bool isFirstTarget = true;
                                
                                foreach (ConceptMapTarget target in element.target)
                                {
                                    if (!isFirstTarget) 
                                        sourceCode="";
                                    isFirstTarget= true;
                                    string targetCode = target.code.GetValueAsString();
                                    string targetName = "";
                                    foreach (ValueSetInclude include in targetValueSet.compose.include)
                                    {
                                        foreach (ValueSetConcept1 concept in include.concept)
                                        {
                                            if (concept.code.value.Equals(targetCode))
                                            {
                                                targetName = concept.display.GetValueAsString();
                                            }
                                        }
                                    }
                                    // Need to check codeSystem is SNOMED
                                    if (float.Parse(targetCode) > 0 && targetCode.Length > 7)
                                    {
                                        table.Add(Html.Tr(new object[]
                                            {
                                                Html.Td(sourceCode),
                                                Html.Td(sourceName),
                                                Html.Td(Html.A(SnomedHelper.GetBrowserUrl(targetCode), targetCode)),
                                                Html.Td(targetName)
                                            }
                                        ));
                                    }
                                    else
                                    {
                                        table.Add(Html.Tr(new object[]
                                            {
                                                Html.Td(sourceCode),
                                                Html.Td(sourceName),
                                                Html.Td(targetCode),
                                                Html.Td(targetName)
                                            }
                                        ));
                                    }


                                }
                            }
                            else
                            {
                                table.Add(Html.Tr(new object[]
                                     {
                                        Html.Td(sourceCode),
                                        Html.Td(""),
                                        Html.Td(""),
                                        Html.Td("")
                                    }
                                )
                                );
                            }
                        }

                    }
              
            }
            result.Add(Html.Table(new object[] { table, Html.Class(Styles.ValueSetReferenceTableClassName) }));

            return result.ToArray();
        }

       
       
    }
}
