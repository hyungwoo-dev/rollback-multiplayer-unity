using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Text;

namespace SourceGenerator
{
    [Generator]
    public sealed class ManagedStateGenerator : ISourceGenerator
    {
        static readonly DiagnosticDescriptor MissingNew =
            new DiagnosticDescriptor(
                "MST001",
                "Get missing",
                "ManagedState '{0}' must implement method New()",
                "ManagedState",
                DiagnosticSeverity.Error,
                true);

        static readonly DiagnosticDescriptor MissingOnRelease =
            new DiagnosticDescriptor(
                "MST002",
                "Return missing",
                "ManagedState '{0}' must implement method OnRelease()",
                "ManagedState",
                DiagnosticSeverity.Error,
                true);

        static readonly DiagnosticDescriptor MissingManagedState =
            new DiagnosticDescriptor(
                "MST003",
                "ManagedStateAttribute missing",
                "'{0}' must be have ManagedStateAttribute",
                "ManagedState",
                DiagnosticSeverity.Error,
                true);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = context.SyntaxReceiver as SyntaxReceiver;
            if (receiver == null)
                return;

            foreach (var classDecl in receiver.Candidates)
            {
                var model = context.Compilation.GetSemanticModel(classDecl.SyntaxTree);
                var type = model.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (type == null)
                    continue;

                if (!HasManagedStateAttribute(type))
                    continue;

                ValidatePoolMethods(context, type);
                Generate(context, type);
            }
        }
        #region Validation

        static void ValidatePoolMethods(GeneratorExecutionContext ctx, INamedTypeSymbol type)
        {
            bool hasNew = false;
            bool hasOnRelease = false;
            bool hasUnsupportedType = false;
            foreach (var member in type.GetMembers())
            {
                if (member is IMethodSymbol method)
                {
                    if (method.Name == "New"
                    && method.Parameters.Length == 0
                    && SymbolEqualityComparer.Default.Equals(method.ReturnType, type))
                    {
                        hasNew = true;
                    }

                    if (method.Name == "OnRelease"
                        && method.Parameters.Length == 0
                        && method.ReturnsVoid)
                    {
                        hasOnRelease = true;
                    }
                }


                if (member is IFieldSymbol field)
                {
                    if (IsList(field.Type))
                    {
                        if (field.Type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
                        {
                            var genericType = namedTypeSymbol.TypeArguments[0];
                            if (IsUserDefinedClass(genericType) && !IsManagedState(genericType))
                            {
                                ctx.ReportDiagnostic(Diagnostic.Create(MissingManagedState, type.Locations[0], field.Type.Name));
                            }
                        }
                    }
                    else if (IsUserDefinedClass(field.Type) && !IsManagedState(field.Type))
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(MissingManagedState, type.Locations[0], field.Type.Name));
                    }
                }
            }

            if (!hasNew)
                ctx.ReportDiagnostic(Diagnostic.Create(MissingNew, type.Locations[0], type.Name));

            if (!hasOnRelease)
                ctx.ReportDiagnostic(Diagnostic.Create(MissingOnRelease, type.Locations[0], type.Name));
        }

        #endregion 

        #region Generation

        static void Generate(GeneratorExecutionContext ctx, INamedTypeSymbol type)
        {
            var sb = new StringBuilder();
            var ns = type.ContainingNamespace.IsGlobalNamespace
                ? null
                : type.ContainingNamespace.ToDisplayString();

            if (ns != null)
            {
                sb.AppendLine("namespace " + ns);
                sb.AppendLine("{");
            }

            sb.AppendLine("partial class " + type.Name);
            sb.AppendLine("{");

            sb.AppendLine("    partial void OnClone();");
            sb.AppendLine();

            GenerateCopyFrom(sb, type);
            sb.AppendLine();

            GenerateClone(sb, type);
            sb.AppendLine();

            GenerateRelease(sb, type);

            sb.AppendLine("}");

            if (ns != null)
                sb.AppendLine("}");

            ctx.AddSource(type.Name + ".ManagedState.g.cs", sb.ToString());
        }

        static void GenerateClone(StringBuilder sb, INamedTypeSymbol type)
        {
            sb.AppendLine("    public " + type.Name + " Clone()");
            sb.AppendLine("    {");
            sb.AppendLine("        var obj = New();");
            sb.AppendLine("        obj.CopyFrom(this);");
            sb.AppendLine("        obj.OnClone();");
            sb.AppendLine("        return obj;");
            sb.AppendLine("    }");
        }

        static void GenerateRelease(StringBuilder sb, INamedTypeSymbol type)
        {
            sb.AppendLine("    public void Release()");
            sb.AppendLine("    {");

            var isFirstLine = true;
            foreach (var member in type.GetMembers())
            {
                var field = member as IFieldSymbol;
                if (field == null) continue;
                if (field.IsConst) continue;
                if (field.IsStatic) continue;

                if (isFirstLine)
                {
                    isFirstLine = false;
                }
                else
                {
                    sb.AppendLine();
                }

                if (IsManagedState(field.Type))
                {
                    sb.AppendLine("        if (" + field.Name + " != null)");
                    sb.AppendLine("            " + field.Name + ".Release();");
                }
                else if (IsList(field.Type))
                {
                    sb.AppendLine("        for (int i = 0; i < " + field.Name + ".Count; i++)");
                    sb.AppendLine("            " + field.Name + "[i].Release();");
                    sb.AppendLine("        " + field.Name + ".Clear();");
                }
                else if (IsIndexedCollection(field))
                {
                    sb.AppendLine("        " + field.Name + ".Clear();");
                }
                else if (IsString(field.Type))
                {
                    sb.AppendLine("        " + field.Name + " = System.String.Empty;");
                }
                else if (IsValueType(field.Type))
                {
                    sb.AppendLine("        " + field.Name + " = default(" + field.Type.ToDisplayString() + ");");
                }
            }

            sb.AppendLine();
            sb.AppendLine("        OnRelease();");
            sb.AppendLine("    }");
        }

        static void GenerateCopyFrom(StringBuilder sb, INamedTypeSymbol type)
        {
            sb.AppendLine("    public void CopyFrom(" + type.Name + " other)");
            sb.AppendLine("    {");

            if (type.BaseType != null && HasManagedStateAttribute(type.BaseType))
            {
                sb.AppendLine("        base.CopyFrom(other);");
            }

            var isFirstLine = true;
            foreach (var member in type.GetMembers())
            {
                var field = member as IFieldSymbol;
                if (field == null) continue;
                if (field.IsConst) continue;
                if (field.IsStatic) continue;

                if (isFirstLine)
                {
                    isFirstLine = false;
                }
                else
                {
                    sb.AppendLine();
                }

                if (IsList(field.Type))
                {
                    sb.AppendLine("        " + field.Name + ".Clear();");
                    sb.AppendLine("        for (int i = 0; i < other." + field.Name + ".Count; i++)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            var clone = other." + field.Name + "[i].Clone();");
                    sb.AppendLine("            " + field.Name + ".Add(clone);");
                    sb.AppendLine("        }");
                }
                else if (IsDictionary(field.Type) && TryGetManagedDictionary(field, out var dictioanryBacking, out var key))
                {
                    sb.AppendLine("        " + field.Name + ".Clear();");
                    sb.AppendLine("        for (int i = 0; i < " + dictioanryBacking + ".Count; i++)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            var e = " + dictioanryBacking + "[i];");
                    sb.AppendLine("            " + field.Name + ".Add(e." + key + ", e);");
                    sb.AppendLine("        }");
                }
                else if (IsHashSet(field.Type) && TryGetManagedHashSet(field, out var hashSetBacking))
                {
                    sb.AppendLine("        " + field.Name + ".Clear();");
                    sb.AppendLine("        for (int i = 0; i < " + hashSetBacking + ".Count; i++)");
                    sb.AppendLine("            " + field.Name + ".Add(" + hashSetBacking + "[i]);");
                }
                else if (IsString(field.Type) || IsValueType(field.Type))
                {
                    sb.AppendLine("        " + field.Name + " = other." + field.Name + ";");
                }
                else if (IsUserDefinedClass(field.Type))
                {
                    sb.AppendLine("        " + "if (other." + field.Name + " != null)");
                    sb.AppendLine("            " + field.Name + " = other." + field.Name + ".Clone();");
                    sb.AppendLine("        " + "else");
                    sb.AppendLine("            " + field.Name + " = null;");
                }
            }

            sb.AppendLine("    }");
        }

        private static bool IsPrimaryValueType(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Char:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Enum:
                case SpecialType.System_String: // String is a reference type but often considered a primary/primitive type in C#
                case SpecialType.System_Decimal: // Decimal is a value type, not strictly primitive, but often grouped with them
                {
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }

        #endregion

        #region Helpers

        static bool HasManagedStateAttribute(INamedTypeSymbol type)
        {
            foreach (var a in type.GetAttributes())
                if (a.AttributeClass.Name == "ManagedStateAttribute")
                    return true;
            return false;
        }

        static bool IsManagedState(ITypeSymbol type)
        {
            var nts = type as INamedTypeSymbol;
            return nts != null && HasManagedStateAttribute(nts);
        }

        static bool IsList(ITypeSymbol type)
        {
            var nts = type as INamedTypeSymbol;
            return nts != null && nts.Name == "List";
        }

        static bool IsDictionary(ITypeSymbol type)
        {
            var nts = type as INamedTypeSymbol;
            return nts != null && nts.Name == "Dictionary";
        }

        static bool IsHashSet(ITypeSymbol type)
        {
            var nts = type as INamedTypeSymbol;
            return nts != null && nts.Name == "HashSet";
        }

        static bool IsString(ITypeSymbol type)
        {
            return type.SpecialType == SpecialType.System_String;
        }

        static bool IsValueType(ITypeSymbol type)
        {
            return type.IsValueType;
        }

        static bool IsUserDefinedClass(ITypeSymbol type)
        {
            return !IsPrimaryValueType(type) &&
                !IsList(type) &&
                !IsDictionary(type) &&
                !IsHashSet(type) &&
                type.TypeKind == TypeKind.Class;
        }

        static bool IsIndexedCollection(IFieldSymbol field)
        {
            return IsDictionary(field.Type) || IsHashSet(field.Type);
        }

        static bool TryGetManagedHashSet(IFieldSymbol field, out string backing)
        {
            backing = null;
            foreach (var a in field.GetAttributes())
            {
                if (a.AttributeClass.Name == "ManagedHashSetAttribute")
                {
                    backing = (string)a.ConstructorArguments[0].Value;
                    return true;
                }
            }
            return false;
        }

        static bool TryGetManagedDictionary(IFieldSymbol field, out string backing, out string key)
        {
            backing = null;
            key = null;
            foreach (var a in field.GetAttributes())
            {
                if (a.AttributeClass.Name == "ManagedDictionaryAttribute")
                {
                    backing = (string)a.ConstructorArguments[0].Value;
                    key = (string)a.ConstructorArguments[1].Value;
                    return true;
                }
            }
            return false;
        }

        #endregion

        sealed class SyntaxReceiver : ISyntaxReceiver
        {
            public readonly List<ClassDeclarationSyntax> Candidates = new List<ClassDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                var cds = syntaxNode as ClassDeclarationSyntax;
                if (cds != null && cds.AttributeLists.Count > 0)
                    Candidates.Add(cds);
            }
        }
    }
}
