using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeBleuPreprocessor.Service
{
    public static class CodeBleuPreprocessorService
    {
        public static string Process(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax)tree.GetRoot();

            //  Remove comentários
            root = root.ReplaceTrivia(
            root.DescendantTrivia().Where(t =>
                t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                t.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)),
            (_, _) => default);

            root = root.WithUsings(new SyntaxList<UsingDirectiveSyntax>());

            if (root.Members.Count == 1)
            {
                if (root.Members[0] is NamespaceDeclarationSyntax namespaceDecl)
                {
                    root = root.WithMembers(namespaceDecl.Members);
                }
                else if (root.Members[0] is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
                {
                    root = root.WithMembers(fileScopedNamespace.Members);
                }
            }

            var normalized = root
                .NormalizeWhitespace(" ", "")
                .ToFullString();

            return normalized;
        }
    }
}
