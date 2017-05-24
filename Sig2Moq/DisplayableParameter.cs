using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;
using System.Linq;

namespace Sig2Moq
{
    public class DisplayableParameter
    {
        public DisplayableParameter(ParameterSyntax parameter, bool isProtected)
        {
            Name = parameter.Identifier.ValueText;
            TypeName = parameter.Type.ToString();
            IsProtected = isProtected;

            var modifier = parameter.Modifiers.FirstOrDefault();
            if (modifier != null)
            {
                var modifierKind = modifier.Kind();
                if (modifierKind == SyntaxKind.RefKeyword)
                {
                    Modifier = "ref";
                    HasRefModifier = true;
                }
                else if (modifierKind == SyntaxKind.OutKeyword)
                {
                    Modifier = "out";
                    HasOutModifier = true;
                }
            }
        }

        public string Name { get; }
        public string TypeName { get; }
        public string Modifier { get; }
        public bool HasRefModifier { get; }
        public bool HasOutModifier { get; }
        public bool IsProtected { get; }

        public string MoqVariableDefinition
        {
            get
            {
                if (HasRefModifier)
                {
                    return $"var {Name} = default({TypeName});";
                }
                else if (HasOutModifier)
                {
                    return $"{TypeName} {Name};";
                }
                else
                {
                    return "";
                }
            }
        }

        public string MoqSetupDefinition
        {
            get
            {
                if (HasRefModifier || HasOutModifier)
                {
                    return $"{Modifier} {Name}";
                }
                else if (IsProtected)
                {
                    return $"ItExpr.IsAny<{TypeName}>()";
                }
                else
                {
                    return $"It.IsAny<{TypeName}>()";
                }
            }
        }

        public string MoqReturnOrCallbackDefinition
        {
            get
            {
                if (HasRefModifier || HasOutModifier)
                {
                    var titleName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Name);
                    return $"{TypeName} p{titleName}";
                }
                else
                {
                    return $"{TypeName} {Name}";
                }
            }
        }
    }
}
