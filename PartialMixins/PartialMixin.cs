using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace PartialMixins
{
    [Generator]
    public class PartialMixin : ISourceGenerator
    {
        private const string attributeText = @"
namespace Mixin
{
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public sealed class MixinAttribute : System.Attribute
    {
        public MixinAttribute(System.Type toImplement)
        {
        }
    }
}
";



        public void Initialize(GeneratorInitializationContext context)
        {
            // Register the attribute source
            context.RegisterForPostInitialization((i) => i.AddSource("MixinAttribute.cs", attributeText));

            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                ExecuteInternal(context);

            }
            catch (Exception e)
            {

                string lines = string.Empty;
                var reader = new StringReader(e.ToString());

                string line;
                do
                {
                    line = reader.ReadLine();
                    lines += "\n#error " + line ?? string.Empty;
                }
                while (line != null);

                var txt = SourceText.From(lines, System.Text.Encoding.UTF8);

                context.AddSource($"Error_mixins.cs", txt);
            }
        }

        private static void ExecuteInternal(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver 
            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
                return;

            // get the added attribute, and INotifyPropertyChanged
            var mixinAttribute = context.Compilation.GetTypeByMetadataName("Mixin.MixinAttribute");
            var parameterAttribute = context.Compilation.GetTypeByMetadataName("Mixin.SubstituteAttribute");


            // We do use the correct compareer...
#pragma warning disable RS1024 // Compare symbols correctly
            var typesToExtend = new HashSet<INamedTypeSymbol>(receiver.Types.Where(t => t.GetAttributes().Any(x => x.AttributeClass.Equals(mixinAttribute, SymbolEqualityComparer.Default))), SymbolEqualityComparer.Default) as IEnumerable<INamedTypeSymbol>;
#pragma warning restore RS1024 // Compare symbols correctly

            typesToExtend = typesToExtend.OrderTopological(elementThatDependsOnOther =>
            {
                var toImplenmt = elementThatDependsOnOther.GetAttributes().Where(x => x.AttributeClass.Equals(mixinAttribute, SymbolEqualityComparer.Default));
                var implementationSymbol = toImplenmt
                .Select(currentMixinAttribute => (currentMixinAttribute.ConstructorArguments.First().Value as INamedTypeSymbol).ConstructedFrom)
                .Where(x => typesToExtend.Contains(x, SymbolEqualityComparer.Default)).ToArray();
                return implementationSymbol;
            });




            var compilation = (CSharpCompilation)context.Compilation;

            foreach (var originalType in typesToExtend)
            {
                var toImplenmt = originalType.GetAttributes().Where(x => x.AttributeClass.Equals(mixinAttribute, SymbolEqualityComparer.Default));
                var typeExtensions = new List<TypeDeclarationSyntax>();
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

                        var changedImplementaionSyntaxNode = originalImplementaionSyntaxNode;

                        var typeParameterImplementer = new TypeParameterImplementer(semanticModel, typeParameterMapping, originalType, implementationSymbol);
                        changedImplementaionSyntaxNode = (TypeDeclarationSyntax)typeParameterImplementer.Visit(changedImplementaionSyntaxNode);


                        var AttributeGenerator = new MethodAttributor(changedImplementaionSyntaxNode);
                        changedImplementaionSyntaxNode = (TypeDeclarationSyntax)AttributeGenerator.Visit(changedImplementaionSyntaxNode);

                        var newClass = (originalType.IsReferenceType ?
                            SyntaxFactory.ClassDeclaration(originalType.Name) : (TypeDeclarationSyntax)SyntaxFactory.StructDeclaration(originalType.Name))
                            .WithBaseList(changedImplementaionSyntaxNode.BaseList)
                            .WithMembers(changedImplementaionSyntaxNode.Members);
                        if (originalType?.TypeParameters.Any() ?? false)
                            newClass = newClass.WithTypeParameterList(GetTypeParameters(originalType));


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

                        typeExtensions.Add(newClass);
                        //if (compilation is CSharpCompilation csCompilation)
                        //{
                        //}

                        //newClasses.Add(newNamespaceDeclaration);
                    }
                }


                var newNamespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(GetNsName(originalType.ContainingNamespace)))
                    .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(typeExtensions));
                var compilationUnit = SyntaxFactory.CompilationUnit().WithMembers(SyntaxFactory.List(new MemberDeclarationSyntax[] { newNamespaceDeclaration }));

                var syntaxTree = compilationUnit.SyntaxTree;
                syntaxTree = syntaxTree.WithRootAndOptions(syntaxTree.GetRoot(), new CSharpParseOptions(languageVersion: compilation.LanguageVersion) { });
                syntaxTree = syntaxTree.GetRoot().NormalizeWhitespace().SyntaxTree;

                compilation = compilation.AddSyntaxTrees(syntaxTree);

                var formated = syntaxTree.GetRoot();
                var txt = formated.GetText(System.Text.Encoding.UTF8);

                //string lines = string.Empty;
                //var reader = new StringReader(txt.ToString());

                //string line;
                //do
                //{
                //    line = reader.ReadLine();
                //    lines += "\n#error " + line ?? string.Empty;
                //}
                //while (line != null);

                //txt = SourceText.From(lines, System.Text.Encoding.UTF8);


                context.AddSource($"{originalType.Name}_mixins.cs", txt);
            }
        }

        private static TypeParameterListSyntax GetTypeParameters(INamedTypeSymbol originalType)
        {
            var typeParametrs = originalType.TypeParameters.Select(x => SyntaxFactory.TypeParameter(x.Name));
            return SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(typeParametrs));
        }


        internal static string GetNsName(INamespaceSymbol ns)
        {
            if (ns == null)
                return null;
            if (ns.ContainingNamespace != null && !string.IsNullOrWhiteSpace(ns.ContainingNamespace.Name))
                return $"{GetNsName(ns.ContainingNamespace)}.{ns.Name}";
            return ns.Name;
        }

        internal static string GetFullQualifiedName(ISymbol typeSymbol, bool getMetadata = false)
        {
            if (typeSymbol is IArrayTypeSymbol)
            {
                var arraySymbol = typeSymbol as IArrayTypeSymbol;
                return $"{GetFullQualifiedName(arraySymbol.ElementType)}[]";
            }
            var ns = GetNsName(typeSymbol.ContainingNamespace);
            if (!string.IsNullOrWhiteSpace(ns))
                return $"{ns}.{GetName(typeSymbol, getMetadata)}";
            return typeSymbol.MetadataName;
        }

        private static string GetName(ISymbol typeSymbol, bool getmetadata)
        {
            if (getmetadata)
                return typeSymbol.MetadataName;
            return typeSymbol.ToString();
        }



        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public List<INamedTypeSymbol> Types { get; } = new();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                // any field with at least one attribute is a candidate for property generation
                if (context.Node is TypeDeclarationSyntax typeDeclaration
                    && typeDeclaration.AttributeLists.Count > 0)
                {
                    // Get the symbol being declared by the field, and keep it if its annotated
                    var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);
                    if (typeSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "Mixin.MixinAttribute"))
                        this.Types.Add(typeSymbol);
                }
            }
        }
    }

}

