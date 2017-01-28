using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FhirProfilePublisher.Engine
{
    internal enum OutputFileType
    {
        Image,
        Script,
        Style,
        StructureDefinition,
        ValueSet,
        Html,
        // START Kevin Mayfield Leeds Teaching Trust 23/1/2017
        ConceptMap
        // END Kevin Mayfield Leeds Teaching Trust 23/1/2017
    }
}
