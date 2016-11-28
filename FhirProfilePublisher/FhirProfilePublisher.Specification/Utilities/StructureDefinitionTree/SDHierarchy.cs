using Hl7.Fhir.V102;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FhirProfilePublisher.Specification
{
    internal class SDHierarchy
    {
        private StructureDefinition _current;
        private List<StructureDefinition> _ancestors = new List<StructureDefinition>();

        public SDHierarchy(StructureDefinition structureDefinition, IStructureDefinitionResolver resolver)
        {
            _current = structureDefinition;

            AddAncestorDefinition(structureDefinition, resolver);
        }

        public StructureDefinition Current
        {
            get
            {
                return _current;
            }
        }

        public StructureDefinition ImmediateAncestor
        {
            get
            {
                return _ancestors.FirstOrDefault();
            }
        }

        private void AddAncestorDefinition(StructureDefinition structureDefinition, IStructureDefinitionResolver resolver)
        {
            if (structureDefinition == null)
                return;

            if (structureDefinition.@base == null)
                return;

            if (String.IsNullOrWhiteSpace(structureDefinition.@base.value))
                return;

            StructureDefinition baseStructureDefinition = resolver.GetStructureDefinition(structureDefinition.@base.value);

            if (baseStructureDefinition == null)
                return;

            if (baseStructureDefinition == structureDefinition)
                return;

            _ancestors.Add(baseStructureDefinition);

            AddAncestorDefinition(baseStructureDefinition, resolver);
        }

        public ElementDefinition GetCurrentElementDefinition(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            ElementDefinition currentElement = _current
                    .differential
                    .element
                    .FirstOrDefault(t => t.path.value == path);

            if (currentElement != null)
                return currentElement;

            // if could not find, it might be because the element's path has been restricted 
            if (path.EndsWith("[x]"))
            {
                // attempt to find using the basePath element
                currentElement = _current
                    .differential
                    .element
                    .Where(t => !string.IsNullOrWhiteSpace(t.GetBasePath()))
                    .FirstOrDefault(t => t.GetBasePath() == path);

                if (currentElement != null)
                    return currentElement;
            }

            // if this wasn't populated then attempt to reconstitute the base path
            currentElement = _current
                .differential
                .element
                .Where(t => String.IsNullOrWhiteSpace(t.GetBasePath()))
                .Where(t => !string.IsNullOrEmpty(t.GetReconstructedBasePath()))
                .FirstOrDefault(t => t.GetReconstructedBasePath() == path);

            return currentElement;
        }

        public ElementDefinition GetAncestorElementDefinitionFromCurrent(ElementDefinition current)
        {
            foreach (StructureDefinition sd in _ancestors)
            {
                string searchPath = ReplacePathResourcePrefix(current.path.value, sd.GetRootPath());

                if ((searchPath == "DomainResource.extension") || (searchPath == "Element.extension"))
                    return null;

                ElementDefinition ancestorElement = sd
                    .differential
                    .element
                    .FirstOrDefault(t => t.path.value == searchPath);

                if (ancestorElement != null)
                    return ancestorElement;

                if (!string.IsNullOrWhiteSpace(current.GetBasePath()))
                {
                    searchPath = ReplacePathResourcePrefix(current.GetBasePath(), sd.GetRootPath());

                    ancestorElement = sd
                        .differential
                        .element
                        .Where(t => t.path.value.EndsWith("[x]"))
                        .FirstOrDefault(t => t.path.value == searchPath);

                    if (ancestorElement != null)
                        return ancestorElement;
                }
                else if (!string.IsNullOrWhiteSpace(current.GetReconstructedBasePath()))
                {
                    searchPath = ReplacePathResourcePrefix(current.GetReconstructedBasePath(), sd.GetRootPath());

                    ancestorElement = sd
                        .differential
                        .element
                        .Where(t => t.path.value.EndsWith("[x]"))
                        .FirstOrDefault(t => t.path.value == searchPath);

                    if (ancestorElement != null)
                        return ancestorElement;
                }
            }

            return null;
        }

        private static string ReplacePathResourcePrefix(string path, string newPrefix)
        {
            if (!path.StartsWith(newPrefix))
                return newPrefix + "." + string.Join(".", path.Split('.').Skip(1));

            return path;
        }
    }
}
