using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Windows.Forms;

namespace Sig2Moq
{
    [ExportCodeRefactoringProvider(RefactoringId, LanguageNames.CSharp), Shared]
    internal class MemberSignatureToMoqDefinitionCodeRefactoringProvider : CodeRefactoringProvider
    {
        public const string RefactoringId = "Sig2Moq";
        
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            if (node is MethodDeclarationSyntax methodNode)
            {
                var action = CodeAction.Create("Extract Moq setup definition", c => MethodDeclaration(context.Document, c, methodNode));
                context.RegisterRefactoring(action);
            }
            else if (node is PropertyDeclarationSyntax propNode)
            {
                var actionGet = CodeAction.Create("Extract GET Moq setup definition", c => PropertyDeclaration(context.Document, c, propNode, false));
                context.RegisterRefactoring(actionGet);

                var actionSet = CodeAction.Create("Extract SET Moq setup definition", c => PropertyDeclaration(context.Document, c, propNode, true));
                context.RegisterRefactoring(actionSet);
            }
            else if (node is AccessorDeclarationSyntax accessorNode)
            {
                var kind = accessorNode.Kind();
                if (kind == SyntaxKind.GetAccessorDeclaration || kind == SyntaxKind.SetAccessorDeclaration)
                {
                    var action = CodeAction.Create("Extract Moq setup definition", c => PropertyAccessorDeclaration(context.Document, c, accessorNode, kind));
                    context.RegisterRefactoring(action);
                }
            }
        }
        
        private static Task<Document> MethodDeclaration(Document document, CancellationToken cancellationToken, MethodDeclarationSyntax method)
        {
            var methodInfo = new DisplayableMethodInformation(method);
            CopyToClipboard(methodInfo.MoqDefinition);
            return Task.FromResult(document);
        }

        private static Task<Document> PropertyDeclaration(Document document, CancellationToken cancellationToken, PropertyDeclarationSyntax property, bool isSetter)
        {
            var propInfo = new DisplayablePropertyInformation(property, isSetter);
            CopyToClipboard(propInfo.MoqDefinition);
            return Task.FromResult(document);
        }

        private static Task<Document> PropertyAccessorDeclaration(Document document, CancellationToken cancellationToken, AccessorDeclarationSyntax propertyAccessor, SyntaxKind kind)
        {
            var ancestor = propertyAccessor.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (ancestor != null)
            {
                PropertyDeclaration(document, cancellationToken, ancestor, kind == SyntaxKind.SetAccessorDeclaration);
            }
            return Task.FromResult(document);
        }

        private static void CopyToClipboard(string text)
        {
            var thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
