using Hl7.Fhir.V102;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FhirProfilePublisher.Specification
{
    public class SDTreeNode
    {
        private List<SDTreeNode> _children = new List<SDTreeNode>();
        private string _lastPathElement;
        private string _name;

        public SDTreeNode Parent { get; set; }
        public string Path { get; private set; }
        public ElementDefinition Element { get; set; }
        public bool IsSlice { get; set; }
        public StructureDefinition ExtensionDefinition { get; set; }

        public SDTreeNode(ElementDefinition element)
        {
            Element = element;
            Path = element.path.WhenNotNull(t => t.value);
            _lastPathElement = element.GetLastPathValue();
            _name = element.name.WhenNotNull(t => t.value);
            IsSlice = false;
        }

        public ElementDefinitionType[] GetElementDefinitionType()
        {
            return this.Element.GetElementDefinitionType();
        }

        public bool HasChangedFromBase
        {
            get
            {
                return Element.HasChangedFromBase;
            }
        }

        public bool ThisOrChildrenHaveChangedFromBase
        {
            get
            {
                if (HasChangedFromBase)
                    return true;

                foreach (SDTreeNode child in _children)
                    if (child.ThisOrChildrenHaveChangedFromBase)
                        return true;

                return false; 
            }
        }

        public bool IsSetupSlice
        {
            get
            {
                return (Element.slicing != null);
            }
        }

        public bool IsSetupSliceForExtension
        {
            get
            {
                return (IsSetupSlice && (Path.EndsWith(".extension") || (Path.EndsWith(".modifierExtension"))));
            }
        }

        public SDTreeNode[] Children
        {
            get
            {
                return _children.ToArray();
            }
        }

        public bool HasChildren
        {
            get
            {
                return Children.Any();
            }
        }

        public string LastPathElement
        {
            get
            {
                return _lastPathElement;
            }
        }

        public string LastPathElementWithoutSliceIndex
        {
            get
            {
                return (Element.PathBeforeSliceIndexing ?? Element.path.value).Split('.').Last();
            }
        }

        public void AddChild(SDTreeNode child)
        {
            if (child.Parent != null)
                child.Parent.RemoveChild(child);

            _children.Add(child);
            child.Parent = this;
        }

        public void RemoveChild(SDTreeNode child)
        {
            _children.Remove(child);
            child.Parent = null;
        }

        public void RemoveAllChildren()
        {
            foreach (SDTreeNode child in _children.ToArray())
                RemoveChild(child);
        }

        public void AddChildren(SDTreeNode[] children)
        {
            foreach (SDTreeNode child in children)
                AddChild(child);
        }

        public bool IsLastChild()
        {
            if (Parent == null)
                return true;

            SDTreeNode lastChild = Parent.Children.LastOrDefault();

            if (lastChild == null)
                throw new Exception("Anomolous tree structure detected");

            return (lastChild == this);
        }

        public override string ToString()
        {
            string result = _lastPathElement;

            if (!string.IsNullOrWhiteSpace(_name))
                result += " (" + _name + ")";

            return result;
        }

        public bool HasZeroMaxCardinality()
        {
            return HasZeroMaxCardinality(this);
        }

        private static bool HasZeroMaxCardinality(SDTreeNode treeNode)
        {
            if (treeNode == null)
                return false;

            return (treeNode.Element.IsRemoved() || HasZeroMaxCardinality(treeNode.Parent));
        }

        public string GetCardinalityText()
        {
            return Element.GetCardinalityText();
        }

        public string GetDisplayName()
        {
            if (GetNodeType().IsExtension())
            {
                string name = Element.name.WhenNotNull(t => t.value);

                if (!String.IsNullOrWhiteSpace(name))
                    return name;

                return Element.GetLastPathValue();
            }
            else
            {
                string result = Element.GetLastPathValue();

                string name = Element.name.WhenNotNull(t => t.value);

                if (name == null)
                    name = Element.BaseElementDefinition.WhenNotNull(t => t.name.WhenNotNull(s => s.value));

                if (!string.IsNullOrWhiteSpace(name))
                    result += " [" + name + "]";

                return result;
            }
        }

        public SDNodeType GetNodeType()
        {
            if (IsSetupSlice)
                return SDNodeType.SetupSlice;

            ElementDefinitionType[] types = GetElementDefinitionType();

            if (types != null)
            {
                if (types.Length == 0)
                {
                    return SDNodeType.Unknown;
                }
                else if (types.Length == 1)
                {
                    ElementDefinitionType elementType = types.Single();

                    if (elementType.IsBackboneElement())
                        return SDNodeType.Element;
                    else if (elementType.IsPrimitiveType())
                        return SDNodeType.PrimitiveType;
                    else if (elementType.IsReference())
                        return SDNodeType.Reference;
                    else if (elementType.IsComplexType())
                        return SDNodeType.ComplexType;
                    else if (elementType.IsExtension())
                    {
                        if (ExtensionDefinition != null)
                            if (ExtensionDefinition.IsComplexExtension())
                                return SDNodeType.ComplexExtension;

                        return SDNodeType.SimpleExtension;
                    }
                    else if (elementType.IsResource())
                        return SDNodeType.Resource;

                    return SDNodeType.Unknown;
                }
                else
                {
                    if (types.Any(t => t.IsReference()))
                        return SDNodeType.Reference;

                    return SDNodeType.Choice;
                }
            }
            else if ((Element.PathBeforeSliceIndexing ?? string.Empty).EndsWith(".extension"))
            {
                // hacky but apparently only way to determine extensions within extensions

                return SDNodeType.SimpleExtension;
            }
            else if (Element.nameReference != null)
            {
                return SDNodeType.ReferenceToAnotherElement;
            }

            return SDNodeType.Unknown;
        }

        public bool IsRemoved()
        {
            return Element.IsRemoved();
        }

        public string GetShortDescription()
        {
            return Element.GetShortDescription();
        }

        public string GetValueSetUri()
        {
            return Element.GetValueSetUri();
        }

        public BindingStrengthlist? GetValueSetBindingStrength()
        {
            return Element.GetValueSetBindingStrength();
        }

        public void DepthFirstTreeWalk(Action<SDTreeNode> function)
        {
            Stack<SDTreeNode> stack = new Stack<SDTreeNode>();
            stack.Push(this);

            while (stack.Any())
            {
                SDTreeNode node = stack.Pop();

                function(node);

                foreach (SDTreeNode childNode in node.Children.Reverse())
                    stack.Push(childNode);
            }
        }
    }
}
