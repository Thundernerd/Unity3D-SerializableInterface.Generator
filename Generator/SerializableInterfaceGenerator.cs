using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TNRD.SerializableInterface.Generators.Walkers;

namespace TNRD.SerializableInterface.Generators
{
    public class SerializableInterfaceGenerator
    {
        private readonly InterfaceDeclarationSyntax node;
        private readonly string assetPath;
        private readonly IReadOnlyList<PropertyDeclarationSyntax> properties;
        private readonly IReadOnlyList<MethodDeclarationSyntax> methods;
        private readonly SyntaxList<UsingDirectiveSyntax> usings;
        private readonly string @namespace;
        private readonly string prefix;
        private readonly string suffix;

        private SerializableInterfaceGenerator(
            InterfaceDeclarationSyntax node,
            string assetPath,
            IReadOnlyList<PropertyDeclarationSyntax> properties,
            IReadOnlyList<MethodDeclarationSyntax> methods,
            SyntaxList<UsingDirectiveSyntax> usings,
            string @namespace,
            string prefix,
            string suffix
        )
        {
            this.node = node;
            this.assetPath = assetPath;
            this.properties = properties;
            this.methods = methods;
            this.usings = usings;
            this.@namespace = @namespace;
            this.prefix = prefix;
            this.suffix = suffix;
        }

        public static IEnumerable<SerializableInterfaceGenerator> Create(
            string scriptText,
            string assetPath,
            string prefix,
            string suffix
        )
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(scriptText);
            SyntaxNode rootNode = tree.GetRoot();
            InterfaceWalker interfaceWalker = new InterfaceWalker();
            interfaceWalker.Visit(rootNode);

            List<SerializableInterfaceGenerator> generators = new List<SerializableInterfaceGenerator>();

            foreach (InterfaceDeclarationSyntax node in interfaceWalker.Interfaces)
            {
                List<PropertyDeclarationSyntax> properties = node.Members
                    .Where(x => x.Kind() == SyntaxKind.PropertyDeclaration)
                    .Cast<PropertyDeclarationSyntax>()
                    .ToList();
                List<MethodDeclarationSyntax> methods = node.Members
                    .Where(x => x.Kind() == SyntaxKind.MethodDeclaration)
                    .Cast<MethodDeclarationSyntax>()
                    .ToList();

                CompilationUnitSyntax root = (CompilationUnitSyntax)GetRoot(node);
                SyntaxList<UsingDirectiveSyntax> usings = root.Usings;

                string @namespace = "";

                SyntaxNode parent = node.Parent;
                if (parent is BaseNamespaceDeclarationSyntax nds)
                {
                    @namespace = nds.Name.ToString();
                }

                generators.Add(new SerializableInterfaceGenerator(node,
                    assetPath,
                    properties,
                    methods,
                    usings,
                    @namespace,
                    prefix,
                    suffix));
            }

            return generators;
        }

        public bool Generate(out Exception exception)
        {
            exception = null;

            try
            {
                IndentedStringBuilder builder = new IndentedStringBuilder();
                builder.AppendLine("// <auto-generated />");
                BuildUsings(builder);

                if (!string.IsNullOrEmpty(@namespace))
                {
                    builder.AppendLine($"namespace {@namespace}");
                    builder.AppendLine("{");
                    builder.IncrementIndent();
                }

                builder.AppendLine("[System.Serializable]");
                builder.AppendLine("/// <inheritdoc />");
                string identifier = node.Identifier.ToString();
                builder.AppendLine($"public class {prefix}{identifier} : TNRD.SerializableInterface<{identifier}>");
                builder.AppendLine("{");

                using (builder.Indent())
                {
                    BuildProperties(builder);
                    BuildMethods(builder);
                }

                builder.AppendLine("}");

                if (!string.IsNullOrEmpty(@namespace))
                {
                    builder.DecrementIndent();
                    builder.AppendLine("}");
                }

                string directoryName = Path.GetDirectoryName(assetPath);
                string fullPath = Path.Combine(directoryName, $"{prefix}{identifier}{suffix}.cs");
                string contents = builder.ToString();

                if (File.Exists(fullPath) && File.ReadAllText(fullPath) == contents)
                    return false;

                File.WriteAllText(fullPath, contents);
                return true;
            }
            catch (Exception e)
            {
                exception = e;
                return false;
            }
        }

        private static SyntaxNode GetRoot(SyntaxNode node)
        {
            return node.Parent == null
                ? node
                : GetRoot(node.Parent);
        }

        private void BuildUsings(IndentedStringBuilder builder)
        {
            foreach (UsingDirectiveSyntax syntax in usings)
            {
                builder.AppendLine($"using {syntax.Name.ToString()};");
            }

            builder.AppendLine();
        }

        private void BuildProperties(IndentedStringBuilder builder)
        {
            foreach (PropertyDeclarationSyntax property in properties)
            {
                BuildProperty(builder, property);
            }
        }

        private static void BuildProperty(IndentedStringBuilder builder, PropertyDeclarationSyntax property)
        {
            builder.AppendLine("/// <inheritdoc />");
            builder.AppendLine($"public {property.Type.ToString()} {property.Identifier.ToString()}");
            builder.AppendLine("{");

            AccessorDeclarationSyntax getAccessor =
                property.AccessorList.Accessors.FirstOrDefault(x => x.Kind() == SyntaxKind.GetAccessorDeclaration);
            if (getAccessor != null)
            {
                using (builder.Indent())
                {
                    builder.AppendLine($"get {{ return Value.{property.Identifier.ToString()}; }}");
                }
            }

            AccessorDeclarationSyntax setAccessor =
                property.AccessorList.Accessors.FirstOrDefault(x => x.Kind() == SyntaxKind.SetAccessorDeclaration);
            if (setAccessor != null)
            {
                using (builder.Indent())
                {
                    builder.AppendLine($"set {{ Value.{property.Identifier.ToString()} = value; }}");
                }
            }

            builder.AppendLine("}");
        }

        private void BuildMethods(IndentedStringBuilder builder)
        {
            foreach (MethodDeclarationSyntax method in methods)
            {
                BuildMethod(builder, method);
            }
        }

        private static void BuildMethod(IndentedStringBuilder builder, MethodDeclarationSyntax method)
        {
            builder.AppendLine("/// <inheritdoc />");
            if (method.Modifiers.Count == 0)
            {
                builder.Append("public ");
            }
            else
            {
                builder.Append(string.Join(" ", method.Modifiers.Select(x => x.ValueText)) + " ");
            }

            builder.Append(method.ReturnType + " ");
            builder.Append($"{method.Identifier.ValueText}");
            BuildMethodTypeParameters(builder, method);
            builder.Append("(");
            BuildMethodParameters(builder, method);
            builder.AppendLine(")");
            BuildMethodGenericConstraints(builder, method);
            builder.AppendLine("{");
            BuildMethodBody(builder, method);
            builder.AppendLine("}");
        }

        private static void BuildMethodTypeParameters(IndentedStringBuilder builder, MethodDeclarationSyntax method)
        {
            if (method.TypeParameterList == null || method.TypeParameterList.Parameters.Count == 0)
                return;

            builder.Append("<");
            builder.Append(string.Join(", ",
                method.TypeParameterList.Parameters.Select(x => x.Identifier.ValueText)));
            builder.Append(">");
        }

        private static void BuildMethodParameters(IndentedStringBuilder builder, MethodDeclarationSyntax method)
        {
            if (method.ParameterList.Parameters.Count == 0)
                return;

            IEnumerable<string> parameters =
                method.ParameterList.Parameters.Select(x => $"{x.Type.ToString()} {x.Identifier.ToString()}");
            builder.Append(string.Join(", ", parameters));
        }

        private static void BuildMethodGenericConstraints(IndentedStringBuilder builder, MethodDeclarationSyntax method)
        {
            using (builder.Indent())
            {
                foreach (TypeParameterConstraintClauseSyntax clause in method.ConstraintClauses)
                {
                    string constraints = string.Join(", ", clause.Constraints.Select(x => x.ToString()));
                    builder.AppendLine($"where {clause.Name.Identifier.ValueText} : {constraints}");
                }
            }
        }

        private static void BuildMethodBody(IndentedStringBuilder builder, MethodDeclarationSyntax method)
        {
            using (builder.Indent())
            {
                if (method.ReturnType.ToString() != "void")
                {
                    builder.Append("return ");
                }
                
                builder.Append($"Value.{method.Identifier.ValueText}");

                if (method.TypeParameterList != null && method.TypeParameterList.Parameters.Count > 0)
                {
                    builder.Append("<");
                    builder.Append(string.Join(", ",
                        method.TypeParameterList.Parameters.Select(x => x.Identifier.ValueText)));
                    builder.Append(">");
                }

                builder.Append("(");

                if (method.ParameterList.Parameters.Count > 0)
                {
                    IEnumerable<string> parameters =
                        method.ParameterList.Parameters.Select(x => x.Identifier.ToString());
                    builder.Append(string.Join(", ", parameters));
                }

                builder.AppendLine(");");
            }
        }
    }
}
