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

        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
        {
            var rightText = node.Right.ToFullString();
            var leftText = node.Left.ToFullString();
            if (!leftText.StartsWith("global"))
            {
                var info = semanticModel.GetSymbolInfo(node).Symbol;
                string fullQualifeidName;
                if (info is IMethodSymbol)
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
            var info = semanticModel.GetSymbolInfo(node);
            if (info.Symbol is ITypeParameterSymbol)
            {
                var tSymbol = info.Symbol as ITypeParameterSymbol;
                if (typeParameterMapping.ContainsKey(tSymbol))
                {
                    var typeParameterSyntax = SyntaxFactory.ParseName($"global::{PartialMixin.GetFullQualifiedName(typeParameterMapping[tSymbol])}");
                    return typeParameterSyntax;
                }
            }
            if (!(node.Parent is QualifiedNameSyntax))
            {
                if (info.Symbol != null
                    && !node.IsVar
                    && (info.Symbol.Kind == SymbolKind.ArrayType
                        || info.Symbol.Kind == SymbolKind.NamedType))
                    return SyntaxFactory.ParseName($"global::{PartialMixin. GetFullQualifiedName(info.Symbol)}");
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

