using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace PartialMixins
{
    internal class ParameterVisitor : CSharpSyntaxRewriter
    {
        private readonly SemanticModel semanticModel;
        private readonly INamedTypeSymbol parameterAttribute;
        private readonly INamedTypeSymbol currentTypeSymbol;

        public ParameterVisitor(SemanticModel semanticModel, INamedTypeSymbol parameterAttribute, INamedTypeSymbol currentTypeSymbol)
        {
            this.semanticModel = semanticModel;
            this.parameterAttribute = parameterAttribute;
            this.currentTypeSymbol = currentTypeSymbol;
        }


        public override SyntaxNode VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            var attribute = node.AttributeLists.Where(x => x.Target.Identifier.Kind() == SyntaxKind.ReturnKeyword).SelectMany(x => x.Attributes.Where(y => y.Name.ToString() == "global::Mixin.SubstituteAttribute")).FirstOrDefault();
            if (attribute is null)
                return base.VisitConversionOperatorDeclaration(node);

            return base.VisitConversionOperatorDeclaration(node.WithType(SyntaxFactory.ParseTypeName(this.currentTypeSymbol.ToDisplayString())));

        }

        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            var attribute = node.AttributeLists.Where(x => x.Target.Identifier.Kind() == SyntaxKind.ReturnKeyword).SelectMany(x => x.Attributes.Where(y => y.Name.ToString() == "global::Mixin.SubstituteAttribute")).FirstOrDefault();
            if (attribute is null)
                return base.VisitOperatorDeclaration(node);

            return base.VisitOperatorDeclaration(node.WithReturnType(SyntaxFactory.ParseTypeName(this.currentTypeSymbol.ToDisplayString())));

        }

        public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            var attribute = node.AttributeLists.Where(x => x.Target.Identifier.Kind() == SyntaxKind.ReturnKeyword).SelectMany(x => x.Attributes.Where(y => y.Name.ToString() == "global::Mixin.SubstituteAttribute")).FirstOrDefault();
            if (attribute is null)
                return base.VisitIndexerDeclaration(node);

            return base.VisitIndexerDeclaration(node.WithType(SyntaxFactory.ParseTypeName(this.currentTypeSymbol.ToDisplayString())));

        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var attribute = node.AttributeLists.Where(x => x.Target.Identifier.Kind() == SyntaxKind.ReturnKeyword).SelectMany(x => x.Attributes.Where(y => y.Name.ToString() == "global::Mixin.SubstituteAttribute")).FirstOrDefault();
            if (attribute is null)
                return base.VisitPropertyDeclaration(node);

            return base.VisitPropertyDeclaration(node.WithType(SyntaxFactory.ParseTypeName(this.currentTypeSymbol.ToDisplayString())));

        }

        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            return base.VisitConstructorDeclaration(node.WithIdentifier(SyntaxFactory.Identifier(this.currentTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat))));
        }

        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            var attribute = node.AttributeLists.SelectMany(x => x.Attributes.Where(y => y.Name.ToString() == "global::Mixin.SubstituteAttribute")).FirstOrDefault();

            if (attribute is null)
                return base.VisitFieldDeclaration(node);

            return base.VisitFieldDeclaration(node.WithDeclaration(node.Declaration.WithType(SyntaxFactory.ParseTypeName(this.currentTypeSymbol.ToDisplayString()))));
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var attribute = node.AttributeLists.Where(x => x.Target.Identifier.Kind() == SyntaxKind.ReturnKeyword).SelectMany(x => x.Attributes.Where(y => y.Name.ToString() == "global::Mixin.SubstituteAttribute")).FirstOrDefault();
            if (attribute is null)
                return base.VisitMethodDeclaration(node);

            return base.VisitMethodDeclaration(node.WithReturnType(SyntaxFactory.ParseTypeName(this.currentTypeSymbol.ToDisplayString())));
        }


        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            var attribute = node.AttributeLists.SelectMany(x => x.Attributes.Where(y => y.Name.ToString() == "global::Mixin.SubstituteAttribute")).FirstOrDefault();

            if (attribute is null)
                return base.VisitParameter(node);

            return base.VisitParameter(node.WithType(SyntaxFactory.ParseTypeName(this.currentTypeSymbol.ToDisplayString())));

        }

    }

}
