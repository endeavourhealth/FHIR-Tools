using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FhirProfilePublisher.Specification;

namespace Hl7.Fhir.V102
{
    public partial class ElementDefinition
    {
        internal string PathBeforeSliceIndexing { get; set; }
        internal bool IsFake { get; set; } = false;
        internal bool HasChangedFromBase { get; set; } = false;
        internal ElementDefinition BaseElementDefinition { get; set; }

        public ElementDefinitionType[] GetElementDefinitionType()
        {
            if (this.type != null)
                return this.type;

            if (this.BaseElementDefinition != null)
                return this.BaseElementDefinition.type;

            return null;
        }

        public string GetValueSetUri()
        {
            ElementDefinitionBinding binding = GetElementDefinitionBinding();

            if (binding != null)
                if (binding.Item != null)
                    if (binding.Item is Reference)
                        return ((Reference)binding.Item).reference.value;

            return null;
        }

        public BindingStrengthlist? GetValueSetBindingStrength()
        {
            ElementDefinitionBinding binding = GetElementDefinitionBinding();

            if (binding != null)
                if (binding.strength != null)
                    if (binding.strength.valueSpecified)
                        return binding.strength.value;

            return null;
        }

        private ElementDefinitionBinding GetElementDefinitionBinding()
        {
            if (binding != null)
                return binding;

            if (BaseElementDefinition != null)
                return BaseElementDefinition.binding;

            return null;
        }

        public string GetW5TopLevelGroup()
        {
            if (mapping == null)
                return string.Empty;

            string w5Group = (mapping
                .FirstOrDefault(t => t.identity.value == "w5")
                .WhenNotNull(t => t.map.value) ?? string.Empty)
                .Split('.')
                .FirstOrDefault();

            if (w5Group == "administrative")
                w5Group = "identification";

            return w5Group;
        }

        public bool IsRemoved()
        {
            int maxCardinality;

            if (int.TryParse(max.WhenNotNull(t => t.value), out maxCardinality))
                return (maxCardinality == 0);

            return false;
        }
        
        public string GetCardinalityText()
        {
            int? min = GetMin();
            string max = GetMax();

            if (min == null || max == null)
                return null;

            return min.ToString() + ".." + max;
        }

        public int? GetMin()
        {
            if (this.min != null)
                return this.min.value;

            if (this.BaseElementDefinition != null)
                if (this.BaseElementDefinition.min != null)
                    return this.BaseElementDefinition.min.value;

            return null;
        }

        public string GetMax()
        {
            if (this.max != null)
                return this.max.value;

            if (this.BaseElementDefinition != null)
                if (this.BaseElementDefinition.max != null)
                    return this.BaseElementDefinition.max.value;

            return null;
        }

        public string GetLastPathValue()
        {
            return path.value.Split('.').Last();
        }

        public string[] GetInvariantText()
        {
            if (constraint != null)
                return constraint.Select(t => t.human.WhenNotNull(s => s.value)).ToArray();

            if (this.BaseElementDefinition != null)
                if (this.BaseElementDefinition.constraint != null)
                    return this.BaseElementDefinition.constraint.Select(t => t.human.WhenNotNull(s => s.value)).ToArray();

            return new string[] { };
        }

        public string GetExtensionCanonicalUrl()
        {
            if (type.WhenNotNull(t => t.Length == 1))
                if (type.Single().code.WhenNotNull(t => t.value.ToLower() == "extension"))
                    if (type.WhenNotNull(t => t.Length == 1))
                        if (type.Single().profile.WhenNotNull(s => s.Length == 1))
                            return type.Single().profile.Single().value;

            return null;
        }

        public bool HasFixedValue()
        {
            return (Item1 != null);
        }

        public string GetFixedValue()
        {
            return Item1.GetValueAsString();
        }

        public override string ToString()
        {
            if (path != null)
                return path.value;

            return base.ToString();
        }

        public string GetBasePathOrPath()
        {
            string path = GetBasePath();

            if (!string.IsNullOrWhiteSpace(path))
                return path;

            return this.path.value;
        }

        public string GetBasePath()
        {
            if (@base != null)
                if (@base.path != null)
                    if (!string.IsNullOrWhiteSpace(@base.path.value))
                        return @base.path.value;

            return null;
        }

        public string GetReconstructedBasePath()
        {
            if (type != null)
                if (type.Length == 1)
                    if (type.First().code != null)
                        if (!String.IsNullOrEmpty(type.First().code.value))
                            if (path.value.ToLower().EndsWith(type.First().code.value.ToLower()))
                                return path.value.Substring(0, (path.value.Length - type.First().code.value.Length)) + "[x]";

            return null;
        }

        public string GetShortDescription()
        {
            if (@short != null)
                return @short.value;

            if (BaseElementDefinition != null)
                if (BaseElementDefinition.@short != null)
                    return BaseElementDefinition.@short.value;

            return null;
        }
    }
}
