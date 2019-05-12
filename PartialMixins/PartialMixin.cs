using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

#if !NETFRAMEWORK
using Buildalyzer.Workspaces;
using Buildalyzer;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PartialMixins
{
    class PartialMixin
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

            System.IO.File.WriteAllText(newFilePath, "");
            var codeToGenerate = GenerateSource(projectPath).Result;
            System.IO.File.WriteAllText(newFilePath, codeToGenerate);
        }


        private static async Task<string> GenerateSource(string projectPath)
        {

            //MSBuildWorkspace workspace = MSBuildWorkspace.Create();

#if NETFRAMEWORK
            var workspace = Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace.Create();
            Project project = await workspace.OpenProjectAsync(projectPath);
#else
            var manager = new AnalyzerManager();
            manager.SetGlobalProperty("MixinsGenerator", "true");
            var workspace = new AdhocWorkspace();
            var analyzer = manager.GetProject(projectPath);
            var project = analyzer.AddToWorkspace(workspace);
#endif

            //Project project = await workspace.OpenProjectAsync(projectPath);

            var compilation = await project.GetCompilationAsync();

            var mixinAttribute = compilation.GetTypeByMetadataName("Mixin.MixinAttribute");
            if (mixinAttribute.ContainingAssembly.Identity.Name != "Mixin")
                throw new Exception("Attribut loded from wrong Assembly");
            var namespaces = compilation.GlobalNamespace.GetNamespaceMembers();
            var typesToExtend = GetTypes(namespaces)
                .Where(x => x.IsReferenceType | x.IsValueType)
                .Where(x => x.GetAttributes().Any(checkedAttribute => checkedAttribute.AttributeClass == mixinAttribute));
            typesToExtend = new HashSet<ITypeSymbol>(typesToExtend);
            typesToExtend = typesToExtend.OrderTopological(elementThatDependsOnOther =>
            {
                var toImplenmt = elementThatDependsOnOther.GetAttributes().Where(y => y.AttributeClass == mixinAttribute);
                var implementationSymbol = toImplenmt
                .Select(currentMixinAttribute => (currentMixinAttribute.ConstructorArguments.First().Value as INamedTypeSymbol).ConstructedFrom)
                .Where(x => typesToExtend.Contains(x)).ToArray();
                return implementationSymbol;
            });

            var newClasses = new List<MemberDeclarationSyntax>();

            foreach (var originalType in typesToExtend)
            {
                var toImplenmt = originalType.GetAttributes().Where(y => y.AttributeClass == mixinAttribute);
                foreach (var currentMixinAttribute in toImplenmt)
                {
                    var implementationSymbol = (currentMixinAttribute.ConstructorArguments.First().Value as INamedTypeSymbol);
                    var updatetedImplementationSymbol = compilation.GetTypeByMetadataName(GetFullQualifiedName(implementationSymbol));
                    // Get Generic Typeparameter

                    var typeParameterMapping = implementationSymbol.TypeParameters
                                            .Zip(implementationSymbol.TypeArguments, (parameter, argumet) => new { parameter, argumet })
                                        .ToDictionary(x => x.parameter, x => x.argumet, new TypeParameterComparer());

                    implementationSymbol = updatetedImplementationSymbol; // Waited until we saved the TypeParameters.

                    foreach (var originalImplementaionSyntaxNode in implementationSymbol.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).Cast<TypeDeclarationSyntax>())
                    {
                        var semanticModel = compilation.GetSemanticModel(originalImplementaionSyntaxNode.SyntaxTree);
                        var typeParameterImplementer = new TypeParameterImplementer(semanticModel, typeParameterMapping);
                        var changedImplementaionSyntaxNode = (TypeDeclarationSyntax)typeParameterImplementer.Visit(originalImplementaionSyntaxNode);

                        var AttributeGenerator = new MethodAttributor();
                        changedImplementaionSyntaxNode = (TypeDeclarationSyntax)AttributeGenerator.Visit(changedImplementaionSyntaxNode);

                        var newClass = (originalType.IsReferenceType ?
                            SyntaxFactory.ClassDeclaration(originalType.Name) : (TypeDeclarationSyntax)SyntaxFactory.StructDeclaration(originalType.Name))
                            .WithBaseList(originalImplementaionSyntaxNode.BaseList)
                            .WithMembers(changedImplementaionSyntaxNode.Members);
                        if ((originalType as INamedTypeSymbol)?.TypeParameters.Any() ?? false)
                            newClass = newClass.WithTypeParameterList(GetTypeParameters(originalType as INamedTypeSymbol));


                        switch (originalType.DeclaredAccessibility)
                        {
                            case Accessibility.NotApplicable:
                                break;
                            case Accessibility.Private:
                                newClass = newClass.AddModifiers(SyntaxFactory.ParseToken("private"));
                                break;
                            case Accessibility.ProtectedAndInternal:
                                newClass = newClass.AddModifiers(SyntaxFactory.ParseToken("protected"));
                                newClass = newClass.AddModifiers(SyntaxFactory.ParseToken("internal"));
                                break;
                            case Accessibility.Protected:
                                newClass = newClass.AddModifiers(SyntaxFactory.ParseToken("protected"));
                                break;
                            case Accessibility.Internal:
                                newClass = newClass.AddModifiers(SyntaxFactory.ParseToken("internal"));
                                break;
                            case Accessibility.ProtectedOrInternal:
                                newClass = newClass.AddModifiers(SyntaxFactory.ParseToken("protected"));
                                newClass = newClass.AddModifiers(SyntaxFactory.ParseToken("internal"));
                                break;
                            case Accessibility.Public:
                                newClass = newClass.AddModifiers(SyntaxFactory.ParseToken("public"));
                                break;
                            default:
                                break;
                        }

                        if (originalType.IsStatic)
                            newClass = newClass.AddModifiers(SyntaxFactory.ParseToken("static"));

                        if (!newClass.Modifiers.Any(x => x.Text == "partial"))
                            newClass = newClass.AddModifiers(SyntaxFactory.ParseToken("partial"));


                        var newNamespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(GetNsName(originalType.ContainingNamespace)))
                            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new MemberDeclarationSyntax[] { newClass }));
                        compilation = compilation.AddSyntaxTrees(SyntaxFactory.CompilationUnit().WithMembers(SyntaxFactory.List(new MemberDeclarationSyntax[] { newNamespaceDeclaration })).SyntaxTree);
                        newClasses.Add(newNamespaceDeclaration);
                    }
                }
            }

            var newClassesCode = Formatter.Format(SyntaxFactory.CompilationUnit()
                .WithMembers(SyntaxFactory.List(newClasses)), workspace).ToFullString();

            return newClassesCode;
        }

        private static TypeParameterListSyntax GetTypeParameters(INamedTypeSymbol originalType)
        {
            var typeParametrs = originalType.TypeParameters.Select(x => SyntaxFactory.TypeParameter(x.Name));
            return SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(typeParametrs));
        }

        internal static string GetFullQualifiedName(ISymbol typeSymbol)
        {
            if (typeSymbol is IArrayTypeSymbol)
            {
                var arraySymbol = typeSymbol as IArrayTypeSymbol;
                return $"{GetFullQualifiedName(arraySymbol.ElementType)}[]";
            }
            var ns = GetNsName(typeSymbol.ContainingNamespace);
            if (!string.IsNullOrWhiteSpace(ns))
                return $"{ns}.{typeSymbol.MetadataName}";
            return typeSymbol.MetadataName;
        }

        internal static string GetNsName(INamespaceSymbol ns)
        {
            if (ns == null)
                return null;
            if (ns.ContainingNamespace != null && !string.IsNullOrWhiteSpace(ns.ContainingNamespace.Name))
                return $"{GetNsName(ns.ContainingNamespace)}.{ns.Name}";
            return ns.Name;
        }

        internal static IEnumerable<ITypeSymbol> GetTypes(IEnumerable<INamespaceSymbol> namespaces)
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