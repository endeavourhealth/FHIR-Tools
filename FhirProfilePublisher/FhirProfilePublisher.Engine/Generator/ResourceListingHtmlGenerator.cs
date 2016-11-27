using Hl7.Fhir.V102;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FhirProfilePublisher.Engine
{
    internal class ResourceListingHtmlGenerator
    {
        private OutputPaths _outputPaths;
        private OutputOptions _outputOptions;

        public ResourceListingHtmlGenerator(OutputPaths outputPaths, OutputOptions outputOptions)
        {
            _outputPaths = outputPaths;
            _outputOptions = outputOptions;
        }

        internal void GenerateSingleResourceListingPageWithIntroText(string fileName, ResourceFileSet resourceFileSet, string introText)
        {
            List<object> structureDefinitionListing = GetStructureDefinitionListing(resourceFileSet);
            XElement valuesetListing = GetValueSetListing(resourceFileSet);

            structureDefinitionListing.Insert(0, XElement.Parse("<div>" + introText + "</div>"));
            structureDefinitionListing.Insert(1, Html.H3("Resources"));

            structureDefinitionListing.AddRange(new object[]
            {
                Html.H3("Value sets"),
                Html.P("The structures above refer to the following value sets:"),
                valuesetListing
            });

            WritePage(fileName, "Overview", Html.Div(structureDefinitionListing.ToArray()));
        }

        internal void GenerateStructureDefinitionListing(string fileName, ResourceFileSet resourceFileSet)
        {
            List<object> structureDefinitionListing = GetStructureDefinitionListing(resourceFileSet);

            WritePage(fileName, "Resources", Html.Div(structureDefinitionListing.ToArray()));
        }

        private List<object> GetStructureDefinitionListing(ResourceFileSet resourceFileSet)
        { 
            List<object> result = new List<object>();

            if (_outputOptions.ShowResourcesInW5Group)
            {
                foreach (string w5Group in resourceFileSet.StructureDefinitionsByW5Group.Keys.OrderBy(t => t))
                {
                    StructureDefinitionFile[] structureDefinitionFiles = resourceFileSet
                        .StructureDefinitionsByW5Group[w5Group]
                        .Where(t => isInResourceMaturityList(t.Maturity))
                        .OrderBy(t => t.Name)
                        .ToArray();

                    XElement groupItemList = GenerateItemList(structureDefinitionFiles);

                    result.Add(Html.H4(w5Group));
                    result.Add(groupItemList);
                }
            }
            else
            {
                StructureDefinitionFile[] structureDefinitionFiles = resourceFileSet
                    .StructureDefinitionFilesWithoutExtensions
                    .Where(t => isInResourceMaturityList(t.Maturity))
                    .OrderBy(t => t.Name)
                    .ToArray();

                XElement itemList = GenerateItemList(structureDefinitionFiles);
                result.Add(itemList);
            }
            
            XElement extensionContent = GenerateItemList(resourceFileSet
                .StructureDefinitionExtensionFiles
                .Where(t => isInResourceMaturityList(t.Maturity))
                .OrderBy(t => t.Name)
                .ToArray());

            result.AddRange(new object[]
            {
                Html.H3("Extensions"),
                Html.P("The structures above refer to the following extensions:"),
                extensionContent
            });

            return result;
        }

        private bool isInResourceMaturityList(ResourceMaturity resourceMaturity)
        {
            if (_outputOptions.ListOnlyResourcesWithMaturity == null)
                return true;

            if (_outputOptions.ListOnlyResourcesWithMaturity.Length == 0)
                return true;

            return _outputOptions.ListOnlyResourcesWithMaturity.Contains(resourceMaturity);
        }

        private XElement GetValueSetListing(ResourceFileSet resourceFileSet)
        {
            ValueSetFile[] items = resourceFileSet.ValueSetFiles
                .Where(t => isInResourceMaturityList(t.Maturity))
                .OrderBy(t => t.Name)
                .ToArray();

            XElement content = GenerateItemList(items);

            return content;
        }

        internal void GenerateValueSetListing(string fileName, ResourceFileSet resourceFileSet)
        {
            XElement content = GetValueSetListing(resourceFileSet);

            WritePage(fileName, "Value sets", content);
        }

        private void WritePage(string fileName, string itemTypeName, XElement content)
        {
            string contentHtml = Html.Div(new object[]
            {
                Html.H3(itemTypeName),
                content
            })
            .ToString(SaveOptions.DisableFormatting);

            string html = Pages.Instance.GetPage(itemTypeName, contentHtml, "0.1", DateTime.Now);

            _outputPaths.WriteUtf8File(OutputFileType.Html, fileName, html);
        }

        private XElement GenerateItemList(ResourceFile[] items)
        {
            return Html.Table(new object[]
            {
                Html.Id(Styles.ResourcesListingTableIdName),
                Html.Class("table table-hover table-condensed"),
                Html.THead(new object[]
                {
                    Html.Th(Styles.ResourceListingTableNameColumnClassName, "Name"),
                    Html.Th(Styles.ResourceListingTableIdentifierColumnClassName, "Identifier")
                }),
                Html.TBody
                (
                    items.Select(t => Html.Tr(new object[] 
                    {
                        Html.Td(Styles.ResourceListingTableNameColumnClassName, Html.A(t.OutputHtmlFilename, t.Name)), 
                        Html.Td(Styles.ResourceListingTableIdentifierColumnClassName, t.CanonicalUrl)
                    })).ToArray()
                )
            });
        }

        private object[] GetMaturityColumnWithImage(string description, string iconName)
        {
            return new object[]
            {
                Html.Img(_outputPaths.GetRelativePath(OutputFileType.Image, iconName)),
                " ",
                description
            };
        }
    }
}
