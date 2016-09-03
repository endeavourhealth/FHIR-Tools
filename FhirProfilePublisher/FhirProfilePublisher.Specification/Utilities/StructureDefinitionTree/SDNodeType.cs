using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FhirProfilePublisher.Specification
{
    public enum SDNodeType
    {
        Unknown,
        PrimitiveType,
        ComplexType,
        Element,
        Reference,
        SimpleExtension,
        ComplexExtension,
        SetupSlice,
        Resource,
        Choice
    }

    public static class SDNodeTypeHelper
    {
        public static bool IsExtension(this SDNodeType value)
        {
            return ((value == SDNodeType.SimpleExtension) || (value == SDNodeType.ComplexExtension));
        }
    }
}
