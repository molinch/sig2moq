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

namespace MoveClassToFile
{
    [ExportCodeRefactoringProvider(RefactoringId, LanguageNames.CSharp), Shared]
    internal class MemberSignatureToMoqDefinitionCodeRefactoringProvider : CodeRefactoringProvider
    {
        public const string RefactoringId = "Sig2Moq";
        
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span);

            if (node is MethodDeclarationSyntax || node is PropertyDeclarationSyntax)
            {
                var action = CodeAction.Create("Extract Moq setup definition", c => ExtractMoqSetupDefinitionAsync(context.Document, (MemberDeclarationSyntax)node, c));
                context.RegisterRefactoring(action);
            }
        }

        private static async Task<Document> ExtractMoqSetupDefinitionAsync(Document document, MemberDeclarationSyntax member, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var method = member as MethodDeclarationSyntax;
            if (method != null)
            {
                MethodDeclaration(method);
            }
            else
            {
                var property = member as PropertyDeclarationSyntax;
                if (property != null)
                {
                    PropertyDeclaration(property);
                }
            }

            return document;
        }

        private static void MethodDeclaration(MethodDeclarationSyntax method)
        {
            var predefinedReturnType = method.ReturnType as PredefinedTypeSyntax;
            bool isVoid = predefinedReturnType != null && predefinedReturnType.Keyword.Kind() == SyntaxKind.VoidKeyword;

            bool hasRefParameter = false;
            string refOutVariableDefinition = "";
            string ret = "";
            string args = "";
            foreach (var parameter in method.ParameterList.Parameters)
            {
                if (args != "")
                {
                    args += ", ";
                    ret += ", ";
                }

                string type = parameter.Type.ToString();
                string variableName = parameter.Identifier.ValueText;

                bool specialModifier = false;
                var modifier = parameter.Modifiers.FirstOrDefault();
                if (modifier != null)
                {
                    var modifierKind = modifier.Kind();
                    if (modifierKind == SyntaxKind.RefKeyword || modifierKind == SyntaxKind.OutKeyword)
                    {
                        if (refOutVariableDefinition != "")
                        {
                            refOutVariableDefinition += Environment.NewLine;
                        }
                        
                        args += $"{modifier.ValueText} {variableName}";
                        specialModifier = true;
                    }
                    if (modifierKind == SyntaxKind.RefKeyword)
                    {
                        refOutVariableDefinition += $"{type} {variableName} = default({type});";
                        hasRefParameter = true;
                    }
                    else if (modifierKind == SyntaxKind.OutKeyword)
                    {
                        refOutVariableDefinition += $"{type} {variableName};";
                    }

                    if (specialModifier)
                    {
                        variableName = "p" + variableName;
                    }
                }
                
                if (!specialModifier)
                {
                    args += $"It.IsAny<{type}>()";
                }

                ret += $"{type} {variableName}";
            }

            string returnCallbackText = isVoid ? "Callback" : "Returns";
            var mockSetup =
                        $"mock.Setup(m => m.{method.Identifier.ValueText}({args}))" + Environment.NewLine +
                        $".{returnCallbackText}(({ret}) =>" + Environment.NewLine +
                            "{" + Environment.NewLine + (isVoid ? "" : $"return default({predefinedReturnType});") + Environment.NewLine + "});";

            if (!string.IsNullOrEmpty(refOutVariableDefinition))
            {
                mockSetup = refOutVariableDefinition + Environment.NewLine + mockSetup;
            }

            if (hasRefParameter)
            {
                const string refWarning = "// Warning: if the parameter passed by reference is not the exact same instance, then the mocked method will never get called";
                mockSetup = refWarning + Environment.NewLine + mockSetup;
            }

            CopyToClipboard(mockSetup);
        }

        private static void PropertyDeclaration(PropertyDeclarationSyntax property)
        {
            var predefinedReturnType = property.Type as PredefinedTypeSyntax;
            
            var mockSetup =
                        $"mock.Setup(m => m.{property.Identifier.ValueText})" + Environment.NewLine +
                        $".Returns(() =>" + Environment.NewLine +
                            "{" + Environment.NewLine + $"return default({predefinedReturnType});" + Environment.NewLine + "});";

            CopyToClipboard(mockSetup);
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
