using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartialMixins
{
    public class PartialMixin : Microsoft.Build.Utilities.Task
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: PartialMixins.exe [ProjectFile] [OutputFile]");
                return;
            }

            var projectPath = args[0];
            var newFilePath = args[1];

            var codeToGenerate = GenerateSource(projectPath).Result;
            System.IO.File.WriteAllText(newFilePath, codeToGenerate);
        }


        public String GeneratedFilePath { get; set; }
        public String ProjectPath { get; set; }

        public override bool Execute()
        {
            var projectPath = ProjectPath;
            if (String.IsNullOrWhiteSpace(projectPath))
                projectPath = this.BuildEngine4.ProjectFileOfTaskNode;

            var codeToGenerate = GenerateSource(projectPath).Result;
            System.IO.File.WriteAllText(GeneratedFilePath, codeToGenerate);
            return true;
        }


        private static async Task<String> GenerateSource(string projectPath)
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Project project = await workspace.OpenProjectAsync(projectPath);

            var compilation = await project.GetCompilationAsync();

            var mixinAttribute = compilation.GetTypeByMetadataName($"{nameof(Mixin)}.{nameof(Mixin.MixinAttribute)}");
            if (mixinAttribute.ContainingAssembly.Identity.Name != "Mixin")
                throw new Exception("Attribut loded from wrong Assembly");
            var namespaces = compilation.GlobalNamespace.GetNamespaceMembers();
            var typesToExtend = GetTypes(namespaces)
                .Where(x => x.IsReferenceType)
                .Where(x => x.GetAttributes().Any(checkedAttribute => checkedAttribute.AttributeClass == mixinAttribute))
                .ToArray();

            var newClasses = new List<MemberDeclarationSyntax>();

            var generator = SyntaxGenerator.GetGenerator(project);


            foreach (var originalType in typesToExtend)
            {
                var toImplenmt = originalType.GetAttributes().Where(y => y.AttributeClass == mixinAttribute);
                foreach (var currentMixinAttribute in toImplenmt)
                {

                    var implementationSymbol = (currentMixinAttribute.ConstructorArguments.First().Value as INamedTypeSymbol);

                    // Get Generic Typeparameter

                    var typeParameterMapping = implementationSymbol.TypeParameters
                                            .Zip(implementationSymbol.TypeArguments, (parameter, argumet) => new { parameter, argumet })
                                        .ToDictionary(x => x.parameter, x => x.argumet);

                    foreach (var originalImplementaionSyntaxNode in implementationSymbol.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).Cast<ClassDeclarationSyntax>())
                    {
                        var typeParameterImplementer = new TypeParameterImplementer(compilation.GetSemanticModel(originalImplementaionSyntaxNode.SyntaxTree), typeParameterMapping);
                        var changedImplementaionSyntaxNode = (ClassDeclarationSyntax)typeParameterImplementer.Visit(originalImplementaionSyntaxNode);

                        var newClass = SyntaxFactory.ClassDeclaration(originalType.Name)
                            .WithMembers(changedImplementaionSyntaxNode.Members)
                            .WithModifiers(changedImplementaionSyntaxNode.Modifiers)
                            .AddModifiers(SyntaxFactory.ParseToken("partial"));
                        var newNamespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(GetNsName(originalType.ContainingNamespace)))
                            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new MemberDeclarationSyntax[] { newClass }));
                        newClasses.Add(newNamespaceDeclaration);




                        var syntaxString = changedImplementaionSyntaxNode.ToFullString();

                    }
                }

            }

            var newClassesCode = Formatter.Format(SyntaxFactory.CompilationUnit()
                .WithMembers(SyntaxFactory.List(newClasses)), workspace).ToFullString();


            return newClassesCode;


        }

        class TypeParameterImplementer : CSharpSyntaxRewriter
        {
            private SemanticModel semanticModel;
            private Dictionary<ITypeParameterSymbol, ITypeSymbol> typeParameterMapping;



            public TypeParameterImplementer(SemanticModel semanticModel, Dictionary<ITypeParameterSymbol, ITypeSymbol> typeParameterMapping)
            {
                this.typeParameterMapping = typeParameterMapping;
                this.semanticModel = semanticModel;

            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                var value = node.Identifier.Value;
                var valueText = node.Identifier.ValueText;
                var text = node.Identifier.Text;
                var info = semanticModel.GetSymbolInfo(node);
                if (info.Symbol is ITypeParameterSymbol)
                {
                    var tSymbol = info.Symbol as ITypeParameterSymbol;
                    if (typeParameterMapping.ContainsKey(tSymbol))
                    {
                        var typeParameterSyntax = SyntaxFactory.IdentifierName($"global::{GetFullQualifiedName(typeParameterMapping[tSymbol])}");
                        return typeParameterSyntax;
                    }
                }
                else if (info.Symbol != null
                    && !node.IsVar
                    && (info.Symbol.Kind == SymbolKind.ArrayType
                        || info.Symbol.Kind == SymbolKind.NamedType))
                    node = SyntaxFactory.IdentifierName($"global::{GetFullQualifiedName(info.Symbol)}");
                return node;
            }

            private static string GetFullQualifiedName(ISymbol typeSymbol)
            {
                var ns = GetNsName(typeSymbol.ContainingNamespace);
                if (!String.IsNullOrWhiteSpace(ns))
                    return $"{ns}.{typeSymbol.MetadataName}";
                return typeSymbol.MetadataName;
            }

        }



        private static string GetNsName(INamespaceSymbol ns)
        {
            if (ns == null)
                return null;
            if (ns.ContainingNamespace != null && !string.IsNullOrWhiteSpace(ns.ContainingNamespace.Name))
                return $"{GetNsName(ns.ContainingNamespace)}.{ns.Name}";
            return ns.Name;
        }

        private static IEnumerable<ITypeSymbol> GetTypes(IEnumerable<INamespaceSymbol> namespaces)
        {
            var childNamespaces = namespaces
                            .SelectMany(x => x.GetMembers())
                            .OfType<INamespaceSymbol>();
            var types = namespaces.SelectMany(x => x.GetMembers())
                            .OfType<ITypeSymbol>();
            if (childNamespaces.Any())
                types = types.Concat(GetTypes(childNamespaces));
            return types;
        }

    }
}


