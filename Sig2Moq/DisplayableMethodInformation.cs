using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sig2Moq
{
    public class DisplayableMethodInformation
    {
        public DisplayableMethodInformation(MethodDeclarationSyntax method)
        {
            Name = method.Identifier.ValueText;
            var predefinedReturnType = method.ReturnType as PredefinedTypeSyntax;
            ReturnTypeName = predefinedReturnType.ToString();
            IsVoid = predefinedReturnType != null && predefinedReturnType.Keyword.Kind() == SyntaxKind.VoidKeyword;
            IsProtected = method.Modifiers.Any(m => m.Kind() == SyntaxKind.ProtectedKeyword);

            Parameters = method.ParameterList.Parameters.Select(p => new DisplayableParameter(p, IsProtected)).ToList();
        }

        public string Name { get; }
        public string ReturnTypeName { get; }
        public bool IsProtected { get; }
        public bool IsVoid { get; }
        public List<DisplayableParameter> Parameters { get; }

        public string MoqDefinition
        {
            get
            {
                return MoqVariables + Environment.NewLine + MoqSetup + MoqReturn;
            }
        }

        private string MoqVariables
        {
            get
            {
                var paramsVariables = Parameters.Select(p => p.MoqVariableDefinition).Where(p => p != "").ToList();
                if (paramsVariables.Count > 0)
                {
                    var variables = string.Join(Environment.NewLine, paramsVariables);
                    if (Parameters.Any(p => p.HasRefModifier))
                    {
                        const string refWarning = "// Warning: if the parameter passed by reference is not the exact same instance, then the mocked method will never get called";
                        return refWarning + Environment.NewLine + variables;
                    }
                    else
                    {
                        return variables;
                    }
                }

                return "";
            }
        }

        private string MoqSetup
        {
            get
            {
                var setupArgs = string.Join(", ", Parameters.Select(p => p.MoqSetupDefinition));
                
                if (IsProtected)
                {
                    return $"mock.Protected().Setup<{ReturnTypeName}>(\"{Name}\", {setupArgs})";
                }
                else
                {
                    return $"mock.Setup(m => m.{Name}({setupArgs}))";
                }
            }
        }

        private string MoqReturn
        {
            get
            {
                var defaultValue = IsVoid ? "" : $"return default({ReturnTypeName});";
                var returnOrCallback = IsVoid ? "Callback" : "Returns";
                var retArgs = string.Join(", ", Parameters.Select(p => p.MoqReturnOrCallbackDefinition));
                var returns =
@"
.{0}(({1}) =>
{{
    {2}
}});";

                return string.Format(returns, returnOrCallback, retArgs, defaultValue);
            }
        }
    }
}
