using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Hl7.Fhir.V102;
using FhirProfilePublisher.Specification;

namespace FhirProfilePublisher.Engine
{
    internal class StructureDefinitionHtmlGenerator
    {
        private ResourceFileSet _resourceFileSet;
        private OutputPaths _outputPaths;

        public StructureDefinitionHtmlGenerator(ResourceFileSet resourceFileSet, OutputPaths outputPaths)
        {
            if (resourceFileSet == null)
                throw new ArgumentNullException("profileSet");

            if (outputPaths == null)
                throw new ArgumentNullException("outputPaths");

            _resourceFileSet = resourceFileSet;
            _outputPaths = outputPaths;
        }

        public void GenerateAll()
        {
            foreach (StructureDefinitionFile structureDefinitionFile in _resourceFileSet.StructureDefinitionFiles)
                Generate(structureDefinitionFile);
        }

        public void Generate(StructureDefinitionFile structureDefinitionFile)
        {
            if (!_resourceFileSet.StructureDefinitionFiles.Contains(structureDefinitionFile))
                throw new ArgumentException("StructureDefinition does not exist in FhirXmlProfileSet", "definition");

            string html = GenerateHtml(structureDefinitionFile);

            _outputPaths.WriteUtf8File(OutputFileType.Html, structureDefinitionFile.OutputHtmlFilename, html);
        }

        private string GenerateHtml(StructureDefinitionFile structureDefinitionFile)
        {
            StructureDefinition definition = structureDefinitionFile.StructureDefinition;

            string content = Html.Div(new object[]
            {
                Html.H3(GetNameHeader(definition)),
                Html.P("The official URL for this profile is: "),
                Html.Pre(definition.url.value),
                Html.H3("Description"),
                Html.P(definition.description.WhenNotNull(t => t.value) ?? (definition.GetDisplayName() + ".")),
                Html.H3("Definition"),
                Html.P(GetBaseProfileSentence(definition)),
                GetTabbedContentView(structureDefinitionFile)
            }).ToString(SaveOptions.None);

            return Pages.Instance.GetPage(definition.GetDisplayName(), content, "0.1", DateTime.Now);
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

        private object[] GetBaseProfileSentence(StructureDefinition definition)
        {
            return new object[]
            {
                "This profile is derived from ",
                Html.A(definition.@base.value, definition.constrainedType.WhenNotNull(t => t.value)),
                "."
            };
        }

        private object[] GetNameHeader(StructureDefinition definition)
        {
            return new object[] 
            { 
                BootstrapHtml.Label(definition.GetTypeDescription()), 
                " ", 
                definition.GetName()
            };
        }

        private XElement GetTabbedContentView(StructureDefinitionFile structureDefinitionFile)
        {
            return BootstrapHtml.GetTabs(new Dictionary<string, object>()
            {
                { "Snapshot", GenerateSnapshotTab(structureDefinitionFile.StructureDefinition) },
                { "Differential", GenerateDifferentialTab(structureDefinitionFile.StructureDefinition) },
                { "Examples", "" },
                { "Schemas", GenerateSchemasTab(structureDefinitionFile) }
            });
        }

        private XElement GenerateSnapshotTab(StructureDefinition structureDefinition)
        {
            TreeViewGenerator treeViewGenerator = new TreeViewGenerator(_resourceFileSet, _outputPaths);
            return treeViewGenerator.GenerateSnapshot(structureDefinition);
        }

        private XElement GenerateDifferentialTab(StructureDefinition structureDefinition)
        {
            TreeViewGenerator treeViewGenerator = new TreeViewGenerator(_resourceFileSet, _outputPaths);
            return treeViewGenerator.GenerateDifferential(structureDefinition);
        }

        private XElement GenerateSchemasTab(StructureDefinitionFile structureDefinitionFile)
        {
            return GetSchemasList(new object[]
            {
                Html.A(_outputPaths.GetRelativePath(OutputFileType.StructureDefinition, structureDefinitionFile.OutputXmlFilename), "StructureDefinition XML"),
                Html.A(_outputPaths.GetRelativePath(OutputFileType.StructureDefinition, structureDefinitionFile.OutputJsonFilename), "StructureDefinition JSON")
            });
        }

        internal static XElement GetSchemasList(object[] list)
        {
            return Html.Ul(new object[]
            {
                Html.Class(Styles.StarListClassName + " " + Styles.SchemasListClassName),
                list.Select(t => Html.Li(Html.P(t))).ToArray()
            });
        }
    }
}
