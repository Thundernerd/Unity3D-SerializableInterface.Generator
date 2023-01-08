using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TNRD.SerializableInterface.Generators.Walkers
{
    internal class AttributeWalker : CSharpSyntaxWalker
    {
        public bool HasSerializableInterfaceAttribute { get; private set; }
            
        /// <inheritdoc />
        public override void VisitAttribute(AttributeSyntax node)
        {
            if (node.Name.ToString() == "SerializableInterface") // TODO: Make this change-proof
            {
                HasSerializableInterfaceAttribute = true;
            }
        }
    }
}
