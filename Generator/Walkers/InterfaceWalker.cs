using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TNRD.SerializableInterface.Generators.Walkers
{
    internal class InterfaceWalker : CSharpSyntaxWalker
    {
        private readonly List<InterfaceDeclarationSyntax> interfaces = new List<InterfaceDeclarationSyntax>();

        public IReadOnlyList<InterfaceDeclarationSyntax> Interfaces => interfaces;

        /// <inheritdoc />
        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            AttributeWalker walker = new AttributeWalker();

            foreach (AttributeListSyntax attributeListSyntax in node.AttributeLists)
            {
                walker.Visit(attributeListSyntax);
            }

            if (walker.HasSerializableInterfaceAttribute)
            {
                interfaces.Add(node);
            }
        }
    }
}
