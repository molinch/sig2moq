using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sig2Moq
{
    public class DisplayablePropertyInformation
    {
        public DisplayablePropertyInformation(PropertyDeclarationSyntax property, bool isSetter)
        {
            Name = property.Identifier.ValueText;
            ReturnTypeName = property.Type.ToString();
            IsProtected = property.Modifiers.Any(m => m.Kind() == SyntaxKind.ProtectedKeyword);
            IsSetter = isSetter;
        }

        public string Name { get; }
        public string ReturnTypeName { get; }
        public bool IsProtected { get; }
        public bool IsSetter { get; }

        public string MoqDefinition
        {
            get
            {
                return Environment.NewLine + MoqSetup + MoqReturn;
            }
        }
        
        private string MoqSetup
        {
            get
            {
                var setup = IsSetter ? "SetupSet" : "Setup";
                string isAny = (IsProtected ? "ItExpr" : "It") +  $".IsAny<{ReturnTypeName}>()";
                
                if (IsProtected)
                {
                    string result = $"mock.Protected().{setup}<{ReturnTypeName}>(\"{Name}\"";
                    result += IsSetter ? $", {isAny})" : ")";
                    return result;
                }
                else
                {
                    string result = $"mock.{setup}(m => m.{Name}";
                    result += IsSetter ? $" = {isAny})" : ")";
                    return result;
                }
            }
        }

        private string MoqReturn
        {
            get
            {
                var defaultValue = IsSetter ? "" : $"return default({ReturnTypeName});";
                var returnOrCallback = IsSetter ? $"Callback(({ReturnTypeName} value)" : "Returns(()";
                var returns =
@"
.{0} =>
{{
    {1}
}});";

                return string.Format(returns, returnOrCallback, defaultValue);
            }
        }
    }
}
