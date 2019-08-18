using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
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
        public MethodAttributor()
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
        }

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return node;
        }

        public override SyntaxNode VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (!node.AttributeLists.Any(x => x.Attributes.Any(y => y.Name.ToFullString() == GENERATOR_ATTRIBUTE_NAME)))
                return node.AddAttributeLists(this.generatedAttribute);
            return node;
        }
    }

}
