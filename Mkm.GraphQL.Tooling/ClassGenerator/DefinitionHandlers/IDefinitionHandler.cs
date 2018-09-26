﻿using GraphQLParser.AST;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mkm.GraphQL.Tooling.CodeGenerator.DefinitionHandlers
{
    public interface IDefinitionHandler
    {
        NamespaceDeclarationSyntax Handle(ASTNode definition, NamespaceDeclarationSyntax @namespace);
    }
}
