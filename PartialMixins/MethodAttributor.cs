using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartialMixins
{
    class MethodAttributor : CSharpSyntaxRewriter
    {
        private const string GENERATOR_ATTRIBUTE_NAME = "global::System.CodeDom.Compiler.GeneratedCodeAttribute";
        private readonly AttributeListSyntax[] generatedAttribute;
        private readonly TypeDeclarationSyntax currentDeclaration;

        public MethodAttributor(TypeDeclarationSyntax currentDeclaration)
        {
            this.generatedAttribute = new AttributeListSyntax[] { SyntaxFactory.AttributeList(
                            SyntaxFactory.SeparatedList(new AttributeSyntax[] {
                            SyntaxFactory.Attribute(SyntaxFactory.ParseName( GENERATOR_ATTRIBUTE_NAME),
                                SyntaxFactory.AttributeArgumentList(
                                    SyntaxFactory.SeparatedList(new AttributeArgumentSyntax[] {
                                        SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression( SyntaxKind.StringLiteralExpression, SyntaxFactory.ParseToken("\"Mixin Task\""))),
                                        SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression( SyntaxKind.StringLiteralExpression, SyntaxFactory.ParseToken($"\"{this.GetType().Assembly.GetName().Version}\"")))
                                    }))
                            )
                        }))};
            this.currentDeclaration = currentDeclaration;
        }


        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {

            if (node.Modifiers.Any(x => x.Kind() == SyntaxKind.AbstractKeyword))
                node = node.WithModifiers(node.Modifiers.Remove(node.Modifiers.First(x => x.Kind() == SyntaxKind.AbstractKeyword)).Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword)));

            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitMethodDeclaration(node);
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {

            if (node != this.currentDeclaration && !node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitClassDeclaration(node);
        }

        public override SyntaxNode VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitDelegateDeclaration(node);
        }

        public override SyntaxNode VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitDestructorDeclaration(node);
        }

        public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitEnumDeclaration(node);
        }
        public override SyntaxNode VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitFieldDeclaration(node);
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitInterfaceDeclaration(node);
        }

        public override SyntaxNode VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            if (node != this.currentDeclaration && !node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitRecordDeclaration(node);
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (node != this.currentDeclaration && !node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitStructDeclaration(node);
        }


        public override SyntaxNode VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitConstructorDeclaration(node);
        }

        public override SyntaxNode VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitConversionOperatorDeclaration(node);
        }

        public override SyntaxNode VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitOperatorDeclaration(node);
        }

        public override SyntaxNode VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitEventFieldDeclaration(node);
        }

        public override SyntaxNode VisitEventDeclaration(EventDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitEventDeclaration(node);
        }

        public override SyntaxNode VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitIndexerDeclaration(node);
        }




        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return base.VisitPropertyDeclaration(node);
        }
    }

}
