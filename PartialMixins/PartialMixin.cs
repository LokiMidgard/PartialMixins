using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            System.IO.File.WriteAllText(newFilePath, "");
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

            System.IO.File.WriteAllText(GeneratedFilePath, "");
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
            IEnumerable<ITypeSymbol> typesToExtend = GetTypes(namespaces)
                .Where(x => x.IsReferenceType)
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

                    implementationSymbol = updatetedImplementationSymbol; // Wait until we saved the TypeParameters.

                    foreach (var originalImplementaionSyntaxNode in implementationSymbol.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).Cast<ClassDeclarationSyntax>())
                    {
                        var semanticModel = compilation.GetSemanticModel(originalImplementaionSyntaxNode.SyntaxTree);
                        var typeParameterImplementer = new TypeParameterImplementer(semanticModel, typeParameterMapping);
                        var changedImplementaionSyntaxNode = (ClassDeclarationSyntax)typeParameterImplementer.Visit(originalImplementaionSyntaxNode);

                        var AttributeGenerator = new MethodAttributor();
                        changedImplementaionSyntaxNode = (ClassDeclarationSyntax)AttributeGenerator.Visit(changedImplementaionSyntaxNode);


                        var newClass = SyntaxFactory.ClassDeclaration(originalType.Name)
                            .WithMembers(changedImplementaionSyntaxNode.Members)
                            .WithModifiers(changedImplementaionSyntaxNode.Modifiers);
                        if ((originalType as INamedTypeSymbol).TypeParameters.Any())
                            newClass = newClass.WithTypeParameterList(GetTypeParameters(originalType as INamedTypeSymbol));

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
            //(originalType as INamedTypeSymbol).TypeParameters;

            var typeParametrs = originalType.TypeParameters.Select(x =>
              {
                  return SyntaxFactory.TypeParameter(x.Name);
              });

            return SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(typeParametrs));

        }






        private static string GetFullQualifiedName(ISymbol typeSymbol)
        {
            var ns = GetNsName(typeSymbol.ContainingNamespace);
            if (!String.IsNullOrWhiteSpace(ns))
                return $"{ns}.{typeSymbol.MetadataName}";
            return typeSymbol.MetadataName;
        }


        class MethodAttributor : CSharpSyntaxRewriter
        {
            private const string GENERATOR_ATTRIBUTE_NAME = "global::System.CodeDom.Compiler.GeneratedCodeAttribute";
            private readonly AttributeListSyntax[] generatedAttribute;
            public MethodAttributor()
            {
                generatedAttribute = new AttributeListSyntax[] { SyntaxFactory.AttributeList(
                            SyntaxFactory.SeparatedList(new AttributeSyntax[] {
                            SyntaxFactory.Attribute(SyntaxFactory.ParseName( GENERATOR_ATTRIBUTE_NAME),
                                SyntaxFactory.AttributeArgumentList(
                                    SyntaxFactory.SeparatedList(new AttributeArgumentSyntax[] {
                                        SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression( SyntaxKind.StringLiteralExpression, SyntaxFactory.ParseToken("\"Mixin Task\""))),
                                        SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression( SyntaxKind.StringLiteralExpression, SyntaxFactory.ParseToken($"\"{this.GetType().Assembly.GetName().Version}\"")))
                                    }))
                            )
                        }))};
            }

            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                    return node.AddAttributeLists(generatedAttribute);
                return node;
            }

            public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                    return node.AddAttributeLists(generatedAttribute);
                return node;
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


