using Hl7.Fhir.V102;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FhirProfilePublisher.Specification
{
    public class SDTreeBuilder
    {
        private StructureDefinition _structureDefinition;
        private SDHierarchy _sdHierarchy;
        private IStructureDefinitionResolver _resolver;

        public SDTreeBuilder(StructureDefinition structureDefinition, IStructureDefinitionResolver resolver)
        {
            _structureDefinition = XmlHelper.DeepClone(structureDefinition);
            _sdHierarchy = new SDHierarchy(_structureDefinition, resolver);
            _resolver = resolver;
        }

        public SDTreeNode GenerateDifferentialTree()
        {
            SDTreeNode snapshotTree = GenerateSnapshotTree(true);

            snapshotTree.DepthFirstTreeWalk(t => RemoveUnchangedNodes(t));

            return snapshotTree;
        }

        private void RemoveUnchangedNodes(SDTreeNode node)
        {
            if (!node.ThisOrChildrenHaveChangedFromBase)
                if (node.Parent != null)
                    node.Parent.RemoveChild(node);
        }

        public SDTreeNode GenerateSnapshotTree(bool includeNodesWithZeroMaxCardinality = true)
        {
            // process ElementDefinition list
            //
            // to create list where path values are unique (by indexing slices)
            // and where there are no orphan children by creating fake parents
            //

            ElementDefinition[] elements = _structureDefinition.differential.element;

            // mark elements as changed
            foreach (ElementDefinition element in elements)
                element.HasChangedFromBase = true;

            // sanity checks
            PerformDifferentialElementsSanityCheck(elements, false);

            // populate ElementDefinitions with their ancestor ElementDefinition
            PopulateBaseElementDefinitions(elements);

            // "index" slices to create unique ElementDefinition.path values
            IndexSlices(elements);

            // Merge differential and the direct base StructureDefinition's differential. -- this needs expanding to include all ancestor base StructureDefinitions
            elements = CreateSnapshot(_structureDefinition);

            // Add fake missing parents
            elements = AddFakeMissingParents(elements);

            // build tree
            //
            //

            SDTreeNode rootNode = GenerateTree(elements);

            // process tree
            //
            //

            // Expand out data types
            rootNode.DepthFirstTreeWalk(t => AddMissingComplexDataTypeElements(t));

            // group slices under the slice "setup" node (except extension slices)
            rootNode.DepthFirstTreeWalk(t => GroupSlices(t));

            // remove 0..0 nodes and their children
            if (!includeNodesWithZeroMaxCardinality)
                rootNode.DepthFirstTreeWalk(t => RemoveZeroMaxCardinalityNodes(t));

            // remove setup extension "setup" slice nodes
            rootNode.DepthFirstTreeWalk(t => RemoveExtensionSetupSlices(t));

            // add extension definitions
            rootNode.DepthFirstTreeWalk(t => AddExtensionDefinitions(t, _resolver));

            // group children of slice parent not part of numbered slice
            rootNode.DepthFirstTreeWalk(t => GroupOpenSliceElements(t));

            return rootNode;
        }

        private void PerformDifferentialElementsSanityCheck(ElementDefinition[] elements, bool checkPathForUniqueness)
        {
            if ((elements == null))
                throw new ArgumentException("StructureDefinition does not have differential element list populated");

            // sanity check #2 - all have path values
            if (!(elements.All(t => (t.path != null) && (!string.IsNullOrWhiteSpace(t.path.value)))))
                throw new ArgumentException("StructureDefinition has element with null or empty path value");

            // sanity check #3 - elements don't contain # character
            if (elements.Any(t => (t.path.value.Contains("#"))))
                throw new ArgumentException("StructureDefinition has element with path value containing a # character");

            // sanity check #4 - elements have unique path values
            if (checkPathForUniqueness)
                VerifyPathIsUnique(elements);

        }

        private void PopulateBaseElementDefinitions(ElementDefinition[] elements)
        {
            foreach (ElementDefinition element in elements)
                element.BaseElementDefinition = _sdHierarchy.GetAncestorElementDefinitionFromCurrent(element);
        }

        private void VerifyPathIsUnique(ElementDefinition[] elements)
        {
            if ((elements.DistinctBy(t => t.path.value).Count() != elements.Count()))
                throw new ArgumentException("StructureDefinition has elements with non-unique path values");
        }


        private ElementDefinition[] AddFakeMissingParents(ElementDefinition[] elementDefinitions)
        {
            List<ElementDefinition> result = new List<ElementDefinition>();

            foreach (ElementDefinition elementDefinition in elementDefinitions)
            {
                string path = elementDefinition.path.WhenNotNull(t => t.value) ?? string.Empty;

                // walk up each path item testing for existence of parent element

                string pathTemporary = string.Empty;

                foreach (string pathItem in path.Split('.'))
                {
                    pathTemporary += pathItem;

                    // if the parent doesn't exist
                    // or we haven't already created a fake parent

                    if (!((elementDefinitions.Any(t => t.path.value == pathTemporary))
                        || (result.Any(t => t.path.value == pathTemporary))))
                    {
                        // create a fake parent

                        ElementDefinition fakeElement = new ElementDefinition();
                        fakeElement.IsFake = true;
                        fakeElement.path = new @string();
                        fakeElement.path.value = pathTemporary;
                        result.Add(fakeElement);
                    }

                    pathTemporary += ".";
                }

                result.Add(elementDefinition);
            }

            return result.ToArray();
        }

        private static void RemoveExtensionSetupSlices(SDTreeNode node)
        {
            if (node.IsSetupSliceForExtension)
                node.Parent.RemoveChild(node);
        }

        private static void RemoveZeroMaxCardinalityNodes(SDTreeNode node)
        {
            if (node.HasZeroMaxCardinality())
                if (node.Parent != null)
                    node.Parent.RemoveChild(node);
        }

        private Dictionary<SDTreeNode, MultiLevelComplexTypePointer> multiLevelComplexTypeRevisit = new Dictionary<SDTreeNode, MultiLevelComplexTypePointer>();

        internal class MultiLevelComplexTypePointer
        {
            public StructureDefinition ComplexDataTypeDefinition { get; set; }
            public ElementDefinition MultiLevelElementDefinition { get; set; }
        }

        private void AddMissingComplexDataTypeElements(SDTreeNode node)
        {
            StructureDefinition dataTypeDefinition;
            ElementDefinition dataTypeRootElement;
            ElementDefinition[] dataTypeChildElements;

            // if is element in multi level complex type, recall context
            if (multiLevelComplexTypeRevisit.ContainsKey(node))
            {
                dataTypeDefinition = multiLevelComplexTypeRevisit[node].ComplexDataTypeDefinition;
                dataTypeRootElement = multiLevelComplexTypeRevisit[node].MultiLevelElementDefinition;
                dataTypeChildElements = dataTypeDefinition.differential.element.GetChildren(dataTypeRootElement).ToArray();

            }
            else  // else check whether is root of complex type
            {
                // don't expand profiles with multi choice types
                if (node.GetElementDefinitionType() == null)
                    return;

                if (node.GetElementDefinitionType().Count() != 1)
                    return;

                ElementDefinitionType elementType = node.GetElementDefinitionType().Single();

                if (elementType == null)
                    return;

                if (!(elementType.IsComplexType() || elementType.IsReference()))
                    return;

                // don't expand root Extension elements
                if ((node.Path == "Extension") && (elementType.TypeName == "Element"))
                    return;

                dataTypeDefinition = FhirData.Instance.FindDataTypeStructureDefinition(elementType.TypeName);

                if (dataTypeDefinition == null)
                    throw new Exception("Could not find FHIR data type " + elementType.TypeName);

                dataTypeRootElement = dataTypeDefinition.differential.element.GetRootElement();
                dataTypeChildElements = dataTypeDefinition.differential.element.GetChildren(dataTypeRootElement).ToArray();
            }

            List<SDTreeNode> newChildren = new List<SDTreeNode>();

            foreach (ElementDefinition dataTypeChildElement in dataTypeChildElements)
            {
                string lastPathElement = dataTypeChildElement.GetLastPathValue();

                SDTreeNode[] existingChildren = node.Children.Where(t => t.LastPathElementWithoutSliceIndex == lastPathElement).ToArray();

                List<SDTreeNode> currentNewChildren = new List<SDTreeNode>();

                // if child or children don't exist, add the child
                if (existingChildren.Length == 0)
                {
                    // TEMPORARY IF STATEMENT
                    if (!node.GetElementDefinitionType().Single().IsReference())
                    {
                        SDTreeNode newChild = new SDTreeNode(dataTypeChildElement);
                        currentNewChildren.Add(newChild);
                    }
                }
                else // child or children already exists
                {
                    foreach (SDTreeNode existingChild in existingChildren)
                    {
                        // if is a "fake" element, it needs replacing
                        if (existingChild.Element.IsFake)
                        {
                            SDTreeNode newChild = new SDTreeNode(dataTypeChildElement);
                            SDTreeNode[] childsChildren = existingChild.Children;
                            existingChild.RemoveAllChildren();
                            newChild.AddChildren(childsChildren);
                            currentNewChildren.Add(newChild);
                        }
                        else // keep the existing child
                        {
                            existingChild.Element.BaseElementDefinition = dataTypeChildElement;
                            currentNewChildren.Add(existingChild);
                        }
                    }
                }

                newChildren.AddRange(currentNewChildren);

                // if complex data type's children have children....argh!  (should only be for the Timing data type)
                if (dataTypeDefinition.differential.element.GetChildren(dataTypeChildElement).Count() > 0)
                {
                    MultiLevelComplexTypePointer multiLevelComplexTypePointer = new MultiLevelComplexTypePointer()
                    {
                        ComplexDataTypeDefinition = dataTypeDefinition,
                        MultiLevelElementDefinition = dataTypeChildElement
                    };

                    foreach (SDTreeNode currentChild in currentNewChildren)
                        multiLevelComplexTypeRevisit.Add(currentChild, multiLevelComplexTypePointer);
                }
            }

            foreach (SDTreeNode childNode in node.Children)
                if (newChildren.All(t => t.LastPathElement != childNode.LastPathElement))
                    newChildren.Add(childNode);

            node.RemoveAllChildren();
            node.AddChildren(newChildren.ToArray());
        }

        private static void GroupSlices(SDTreeNode node)
        {
            SDTreeNode[] childSetupSlices = node.Children.Where(t => t.IsSetupSlice && (!t.IsSetupSliceForExtension)).ToArray();

            if (childSetupSlices.Any())
            {
                foreach (SDTreeNode childSetupSlice in childSetupSlices)
                {
                    SDTreeNode[] childSlices = node.Children.Where(t => t.Path.StartsWith(childSetupSlice.Path + "#")).ToArray();

                    foreach (SDTreeNode childSlice in childSlices)
                    {
                        childSlice.IsSlice = true;
                        node.RemoveChild(childSlice);
                        childSetupSlice.AddChild(childSlice);
                    }
                }
            }
        }

        private static void IndexSlices(ElementDefinition[] elements)
        {
            ElementDefinition[] slices = elements.Where(t => t.slicing != null).ToArray();

            foreach (ElementDefinition slice in slices)
            {
                string slicePath = slice.path.value;
                int sliceCount = 0;
                bool isInitialSlice = false;   // need boolean and integer in case root slice is not first in list

                foreach (ElementDefinition element in elements)
                {
                    string path = element.path.value;

                    if (path == slicePath)
                    {
                        if (element == slice)
                        {
                            isInitialSlice = true;
                        }
                        else
                        {
                            isInitialSlice = false;
                            sliceCount++;
                        }
                    }

                    if (path.StartsWith(slicePath))
                    {
                        if (!isInitialSlice)
                        {
                            if (sliceCount == 0)
                                throw new Exception("Found slice child element before slice root element");

                            element.PathBeforeSliceIndexing = element.path.value;
                            element.path.value = slicePath + "#" + sliceCount.ToString() + path.Substring(slicePath.Length);
                        }
                    }
                }
            }
        }

        private ElementDefinition[] CreateSnapshot(StructureDefinition structure)
        {
            ElementDefinition[] elements = structure.differential.WhenNotNull(t => t.element);

            if (elements == null)
                throw new Exception("No differential in definition");

            StructureDefinition baseStructure = _sdHierarchy.ImmediateAncestor;
            ElementDefinition[] baseSnapshotElements = baseStructure.differential.WhenNotNull(t => t.element);

            if (baseSnapshotElements == null)
                throw new Exception("No snapshot in base definition");

            List<ElementDefinition> result = new List<ElementDefinition>();

            foreach (ElementDefinition baseElement in baseSnapshotElements)
            {
                ElementDefinition element = _sdHierarchy.GetCurrentElementDefinition(baseElement.path.WhenNotNull(t => t.value));

                if (element != null)
                    result.Add(element);
                else
                    result.Add(baseElement);
            }

            List<ElementDefinition> elementsToAddToBeginning = new List<ElementDefinition>();

            string[] baseBaseElementNames = new string[]
            {
                "id",
                "meta",
                "implicitRules",
                "language",
                "text",
                "contained"
            };

            foreach (ElementDefinition element in elements)
            {
                if (!result.Any(t => t == element))
                {
                    if (baseBaseElementNames.Contains(element.GetLastPathValue()))
                        elementsToAddToBeginning.Add(element);
                    else
                        result.Add(element);
                }
            }

            result.InsertRange(0, elementsToAddToBeginning);

            return result.ToArray();
        }

        private SDTreeNode GenerateTree(ElementDefinition[] elements)
        {
            ElementDefinition rootElement = elements.GetRootElement();

            if (rootElement == null)
                throw new Exception("Could not find root element");

            SDTreeNode rootNode = new SDTreeNode(rootElement);

            Stack<SDTreeNode> stack = new Stack<SDTreeNode>();
            stack.Push(rootNode);

            while (stack.Any())
            {
                SDTreeNode node = stack.Pop();

                foreach (ElementDefinition element in elements.GetChildren(node.Element))
                {
                    SDTreeNode childNode = new SDTreeNode(element);
                    node.AddChild(childNode);
                    stack.Push(childNode);
                }
            }

            return rootNode;
        }

        public void AddExtensionDefinitions(SDTreeNode treeNode, IStructureDefinitionResolver resolver)
        {
            if (!treeNode.GetNodeType().IsExtension())
                return;

            ElementDefinitionType[] type = treeNode.GetElementDefinitionType();

            if (type == null)
                return;

            if (type.Length != 1)
                return;

            if (type.First().profile == null)
                return;

            if (type.First().profile.Length != 1)
                return;

            uri profileUri = type.First().profile.First();

            if (profileUri == null)
                return;

            StructureDefinition structureDefinition = resolver.GetStructureDefinition(profileUri.value);

            if (structureDefinition == null)
                throw new Exception("Could not find extension " + profileUri.value);

            treeNode.ExtensionDefinition = structureDefinition;
        }

        private void GroupOpenSliceElements(SDTreeNode node)
        {
            if (node.IsSetupSlice && (!node.IsSetupSliceForExtension))
            {
                List<SDTreeNode> nodesToGroup = new List<SDTreeNode>();

                foreach (SDTreeNode childNode in node.Children)
                    if (!childNode.IsSlice)
                        nodesToGroup.Add(childNode);

                if (node.Element.slicing.rules.value == SlicingRuleslist.open)
                {
                    ElementDefinition openSliceElement = new ElementDefinition();
                    openSliceElement.path = new @string();
                    openSliceElement.path.value = node.Element.path.value + "#n";
                    openSliceElement.name = new @string();
                    // openSliceElement.name.value = "open";
                    // also fix cardinalities
                    openSliceElement.type = node.GetElementDefinitionType();
                    SDTreeNode openSlice = new SDTreeNode(openSliceElement);
                    openSlice.AddChildren(nodesToGroup.ToArray());
                    node.AddChild(openSlice);
                }
                else if (node.Element.slicing.rules.value == SlicingRuleslist.openAtEnd)
                {
                    ElementDefinition openAtEndSliceElement = new ElementDefinition();
                    openAtEndSliceElement.path = new @string();
                    openAtEndSliceElement.path.value = node.Element.path.value + "#n";
                    openAtEndSliceElement.name = new @string();
                    // openAtEndSliceElement.name.value = "openAtEnd";
                    // also fix cardinalities
                    openAtEndSliceElement.type = node.GetElementDefinitionType();
                    SDTreeNode openAtEndSlice = new SDTreeNode(openAtEndSliceElement);
                    openAtEndSlice.AddChildren(nodesToGroup.ToArray());
                    node.AddChild(openAtEndSlice);
                }
                else
                {
                    foreach (SDTreeNode childNodeToRemove in nodesToGroup)
                        node.RemoveChild(childNodeToRemove);
                }
            }
        }
    }
}
