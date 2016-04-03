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
        public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            //return null;
            return base.VisitAliasQualifiedName(node);
        }

        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
        {
            var rightText = node.Right.ToFullString();
            var leftText = node.Left.ToFullString();
            //node.ToFullString()
            if (!leftText.StartsWith("global"))
            {
                var info = semanticModel.GetSymbolInfo(node).Symbol;
                string fullQualifeidName;
                if (info is IMethodSymbol)
                    fullQualifeidName = $"global::{GetNsName(info.ContainingNamespace)}.{(info as IMethodSymbol).ReceiverType.Name}";
                else
                    fullQualifeidName = $"global::{GetFullQualifiedName(info)}";

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
                    var typeParameterSyntax = SyntaxFactory.ParseName($"global::{GetFullQualifiedName(typeParameterMapping[tSymbol])}");
                    return typeParameterSyntax;
                }
            }
            if (!(node.Parent is QualifiedNameSyntax))
            {

                if (info.Symbol != null
                    && !node.IsVar
                    && (info.Symbol.Kind == SymbolKind.ArrayType
                        || info.Symbol.Kind == SymbolKind.NamedType))
                    return SyntaxFactory.ParseName($"global::{GetFullQualifiedName(info.Symbol)}");
                else if (info.Symbol != null
                    && !node.IsVar
                    && ((info.Symbol.Kind == SymbolKind.Method
                        && (info.Symbol as IMethodSymbol).MethodKind == MethodKind.Constructor)))
                    return SyntaxFactory.ParseName($"global::{GetFullQualifiedName((info.Symbol as IMethodSymbol).ReceiverType)}");
                //else if (info.Symbol != null
                //    && !node.IsVar
                //    && (info.Symbol.Kind == SymbolKind.Namespace))
                //    node = node;
            }
            return node;
        }
        private static string GetFullQualifiedName(ISymbol typeSymbol)
        {
            var ns = GetNsName(typeSymbol.ContainingNamespace);
            if (!String.IsNullOrWhiteSpace(ns))
                return $"{ns}.{typeSymbol.MetadataName}";
            return typeSymbol.MetadataName;
        }

        private static string GetNsName(INamespaceSymbol ns)
        {
            if (ns == null)
                return null;
            if (ns.ContainingNamespace != null && !string.IsNullOrWhiteSpace(ns.ContainingNamespace.Name))
                return $"{GetNsName(ns.ContainingNamespace)}.{ns.Name}";
            return ns.Name;
        }

    }

}

