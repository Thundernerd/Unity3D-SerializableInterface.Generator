using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TNRD.SerializableInterface.SourceGenerator
{
    internal class InterfaceSourceGeneratorSyntaxReceiver : ISyntaxReceiver
    {
        private const string ATTRIBUTE_NAME = "SerializableInterface";
        
        public bool Generate { get; private set; }
        public string Namespace { get; private set; }
        public InterfaceDeclarationSyntax Interface { get; private set; }
        public IReadOnlyList<PropertyDeclarationSyntax> Properties { get; private set; }
        public IReadOnlyList<MethodDeclarationSyntax> Methods { get; private set; }
        public IReadOnlyList<UsingDirectiveSyntax> Usings { get; private set; }

        /// <inheritdoc />
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (!(syntaxNode is InterfaceDeclarationSyntax ids))
                return;

            if (!HasAttribute(ids))
                return;

            Generate = true;
            Interface = ids;
            Properties = ids.Members
                .Where(x => x.Kind() == SyntaxKind.PropertyDeclaration)
                .Cast<PropertyDeclarationSyntax>()
                .ToList();
            Methods = ids.Members
                .Where(x => x.Kind() == SyntaxKind.MethodDeclaration)
                .Cast<MethodDeclarationSyntax>()
                .ToList();

            CompilationUnitSyntax root = (CompilationUnitSyntax)GetRoot(ids);
            Usings = root.Usings;

            SyntaxNode parent = ids.Parent;
            if (parent is BaseNamespaceDeclarationSyntax nds)
            {
                Namespace = nds.Name.ToString();
            }
        }

        private SyntaxNode GetRoot(SyntaxNode node)
        {
            return node.Parent == null
                ? node
                : GetRoot(node.Parent);
        }

        private bool HasAttribute(InterfaceDeclarationSyntax ids)
        {
            foreach (AttributeListSyntax list in ids.AttributeLists)
            {
                foreach (AttributeSyntax attribute in list.Attributes)
                {
                    if (attribute.Name.ToString() == ATTRIBUTE_NAME)
                        return true;
                }
            }

            return false;
        }
    }
}
