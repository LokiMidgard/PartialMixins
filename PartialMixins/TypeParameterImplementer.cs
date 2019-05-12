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

    class TypeParameterImplementer : CSharpSyntaxRewriter
    {
        private SemanticModel semanticModel;
        private Dictionary<ITypeParameterSymbol, ITypeSymbol> typeParameterMapping;

        public TypeParameterImplementer(SemanticModel semanticModel, Dictionary<ITypeParameterSymbol, ITypeSymbol> typeParameterMapping)
        {
            this.typeParameterMapping = typeParameterMapping;
            this.semanticModel = semanticModel;
        }

        public override SyntaxNode VisitGenericName(GenericNameSyntax node)
        {
            if (!(node.Parent is QualifiedNameSyntax))
            {
                var info = this.semanticModel.GetSymbolInfo(node).Symbol as INamedTypeSymbol;
                var newArguments = node.TypeArgumentList.Arguments.Select(x =>
                {
                    if (x is IdentifierNameSyntax)
                        return (TypeSyntax)this.VisitIdentifierName(x as IdentifierNameSyntax);
                    if (x is QualifiedNameSyntax)
                        return (TypeSyntax)this.VisitQualifiedName(x as QualifiedNameSyntax);
                    if (x is GenericNameSyntax)
                        return (TypeSyntax)this.VisitGenericName(x as GenericNameSyntax);

                    return x;
                }).ToArray();
                node = node.WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(newArguments)));

                return SyntaxFactory.QualifiedName(SyntaxFactory.ParseName($"global::{PartialMixin.GetNsName(info.ContainingNamespace)}"), node);

            }
            return base.VisitGenericName(node);
        }

        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
        {
            var rightText = node.Right.ToFullString();
            var leftText = node.Left.ToFullString();
            if (!leftText.StartsWith("global::"))
            {
                var info = this.semanticModel.GetSymbolInfo(node).Symbol;
                string fullQualifeidName;
                if (info is IMethodSymbol && info.Name == ".ctor")
                    fullQualifeidName = $"global::{PartialMixin.GetNsName(info.ContainingNamespace)}.{(info as IMethodSymbol).ReceiverType.Name}";
                else
                    fullQualifeidName = $"global::{PartialMixin.GetFullQualifiedName(info)}";

                var name = SyntaxFactory.ParseName(fullQualifeidName);
                return name;
            }
            return base.VisitQualifiedName(node);
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            var info = this.semanticModel.GetSymbolInfo(node);
            if (info.Symbol is ITypeParameterSymbol)
            {
                if (node.Parent is TypeParameterConstraintClauseSyntax)
                    return node;
                var tSymbol = info.Symbol as ITypeParameterSymbol;
                if (this.typeParameterMapping.ContainsKey(tSymbol))
                {
                    var typeParameterSyntax = SyntaxFactory.ParseName($"global::{PartialMixin.GetFullQualifiedName(this.typeParameterMapping[tSymbol])}");
                    return typeParameterSyntax;
                }
            }
            if (!(node.Parent is QualifiedNameSyntax))
            {
                if (info.Symbol != null
                    && !node.IsVar
                    && (info.Symbol.Kind == SymbolKind.ArrayType

                        || info.Symbol.Kind == SymbolKind.NamedType))
                    return SyntaxFactory.ParseName($"global::{PartialMixin.GetFullQualifiedName(info.Symbol)}");
                else if (info.Symbol != null
                    && !node.IsVar
                    && ((info.Symbol.Kind == SymbolKind.Method
                        && (info.Symbol as IMethodSymbol).MethodKind == MethodKind.Constructor)))
                    return SyntaxFactory.ParseName($"global::{PartialMixin.GetFullQualifiedName((info.Symbol as IMethodSymbol).ReceiverType)}");
            }
            return node;
        }

    }

}

