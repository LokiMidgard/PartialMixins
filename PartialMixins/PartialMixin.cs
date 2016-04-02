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
            var typesToExtend = compilation.GlobalNamespace.GetNamespaceMembers()
                .SelectMany(x => x.GetMembers())
                .OfType<ITypeSymbol>()
                .Where(x => x.IsReferenceType)
                .Where(x => x.GetAttributes().Any(checkedAttribute => checkedAttribute.AttributeClass == mixinAttribute))
                .ToArray();

            var newClasses = new List<MemberDeclarationSyntax>();
            var newUsings = new List<UsingDirectiveSyntax>();

            var generator = SyntaxGenerator.GetGenerator(project);


            foreach (var originalType in typesToExtend)
            {
                var toImplenmt = originalType.GetAttributes().Where(y => y.AttributeClass == mixinAttribute);
                foreach (var currentMixinAttribute in toImplenmt)
                {

                    var nameOfImplementaion = currentMixinAttribute.ConstructorArguments.First().Value.ToString();

                    var implementationSymbol = compilation.GetTypeByMetadataName(nameOfImplementaion);
                    foreach (var implementaionSyntaxNode in implementationSymbol.DeclaringSyntaxReferences.Select(x => x.GetSyntax()).Cast<ClassDeclarationSyntax>())
                    {
                        var newClass = SyntaxFactory.ClassDeclaration(originalType.Name)
                            .WithMembers(implementaionSyntaxNode.Members)
                            .WithModifiers(implementaionSyntaxNode.Modifiers)
                            .AddModifiers(SyntaxFactory.ParseToken("partial"));
                        var newNamespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(originalType.ContainingNamespace.MetadataName))
                            .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new MemberDeclarationSyntax[] { newClass }));
                        newClasses.Add(newNamespaceDeclaration);


                        var usings = (implementaionSyntaxNode.SyntaxTree.GetRoot() as CompilationUnitSyntax).Usings;
                        newUsings.AddRange(usings);

                        var syntaxString = implementaionSyntaxNode.ToFullString();

                    }
                }

            }

            var newClassesCode = Formatter.Format(SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List(newUsings))
                .WithMembers(SyntaxFactory.List(newClasses)), workspace).ToFullString();


            return newClassesCode;


        }


    }
}
