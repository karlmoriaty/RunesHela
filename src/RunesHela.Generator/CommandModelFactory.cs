using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace RunesHela.Generator;

internal static class CommandModelFactory
{
    private const string CommandAttributeName = "RunesHela.CommandAttribute";
    private static readonly string[] HandlerNames = { "RunAsync", "Run", "ExecuteAsync", "Execute" };
    private static readonly string[] ReservedNames = { "help", "version" };

    public static CommandCandidate Create(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var diagnostics = new List<DiagnosticInfo>();

        if (context.TargetSymbol is not INamedTypeSymbol type)
        {
            return new CommandCandidate(null, new EquatableArray<DiagnosticInfo>(Array.Empty<DiagnosticInfo>()));
        }

        var location = LocationInfo.From(context.TargetNode.GetLocation());
        var appName = context.SemanticModel.Compilation.AssemblyName ?? "app";

        if (type.IsGenericType)
        {
            diagnostics.Add(new DiagnosticInfo("RHL024", location, type.Name));
            return CreateResult(null, diagnostics);
        }

        string? parentTypeFullName = null;

        if (type.ContainingType is not null)
        {
            parentTypeFullName = type.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (!HasCommandAttribute(type.ContainingType))
            {
                diagnostics.Add(new DiagnosticInfo("RHL023", location, type.Name));
                return CreateResult(null, diagnostics);
            }
        }

        var attribute = context.Attributes[0];
        var explicitName = attribute.ConstructorArguments.Length > 0
            ? attribute.ConstructorArguments[0].Value as string
            : null;

        var name = string.IsNullOrWhiteSpace(explicitName) ? DeriveName(type.Name) : explicitName!.Trim();

        if (name.Length == 0)
        {
            diagnostics.Add(new DiagnosticInfo("RHL026", location, type.Name));
            return CreateResult(null, diagnostics);
        }

        if (!IsValidName(name))
        {
            diagnostics.Add(new DiagnosticInfo("RHL013", location, name));
            return CreateResult(null, diagnostics);
        }

        if (IsReservedName(name))
        {
            diagnostics.Add(new DiagnosticInfo("RHL025", location, name));
            return CreateResult(null, diagnostics);
        }

        var aliases = ReadAliases(attribute);
        var seenAliases = new HashSet<string>(StringComparer.Ordinal) { name };

        foreach (var alias in aliases)
        {
            if (!IsValidName(alias))
            {
                diagnostics.Add(new DiagnosticInfo("RHL027", location, alias, name));
                return CreateResult(null, diagnostics);
            }

            if (IsReservedName(alias))
            {
                diagnostics.Add(new DiagnosticInfo("RHL025", location, alias));
                return CreateResult(null, diagnostics);
            }

            if (!seenAliases.Add(alias))
            {
                diagnostics.Add(new DiagnosticInfo("RHL022", location, alias, name));
                return CreateResult(null, diagnostics);
            }
        }

        var description = attribute.NamedArguments
            .FirstOrDefault(static argument => argument.Key == "Description")
            .Value.Value as string;

        if (string.IsNullOrWhiteSpace(description))
        {
            description = GetSummary(type, cancellationToken);
        }

        description ??= string.Empty;

        if (description.Length == 0)
        {
            diagnostics.Add(new DiagnosticInfo("RHL005", location, name));
        }

        var isHidden = attribute.NamedArguments
            .FirstOrDefault(static argument => argument.Key == "IsHidden")
            .Value.Value is true;

        var handlers = FindHandlers(type);

        if (handlers.Count > 1)
        {
            diagnostics.Add(new DiagnosticInfo("RHL007", location, name));
            return CreateResult(null, diagnostics);
        }

        var handler = handlers.Count == 1 ? handlers[0] : null;
        var returnKind = HandlerReturnKind.None;
        string? handlerName = null;

        if (handler is null)
        {
            return CreateResult(
                new CommandModel(
                    appName,
                    name,
                    description,
                    type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    parentTypeFullName,
                    type.IsStatic,
                    isHidden,
                    new EquatableArray<string>(aliases),
                    null,
                    HandlerReturnKind.None,
                    location),
                diagnostics);
        }

        if (handler.Parameters.Length > 0)
        {
            diagnostics.Add(new DiagnosticInfo("RHL004", location, type.Name, handler.Name));
            return CreateResult(null, diagnostics);
        }

        if (!TryMapReturnKind(handler, context.SemanticModel.Compilation, out returnKind))
        {
            diagnostics.Add(new DiagnosticInfo(
                "RHL021",
                location,
                type.Name,
                handler.Name,
                handler.ReturnType.ToDisplayString()));
            return CreateResult(null, diagnostics);
        }

        if (IsAwaitable(returnKind) && !handler.Name.EndsWith("Async", StringComparison.Ordinal))
        {
            diagnostics.Add(new DiagnosticInfo("RHL012", location, handler.Name));
        }

        if (!type.IsStatic && !HasParameterlessConstructor(type))
        {
            diagnostics.Add(new DiagnosticInfo("RHL020", location, name));
            return CreateResult(null, diagnostics);
        }

        handlerName = handler.Name;

        var model = new CommandModel(
            appName,
            name,
            description,
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            parentTypeFullName,
            type.IsStatic,
            isHidden,
            new EquatableArray<string>(aliases),
            handlerName,
            returnKind,
            location);

        return CreateResult(model, diagnostics);
    }

    private static CommandCandidate CreateResult(CommandModel? model, List<DiagnosticInfo> diagnostics)
    {
        return new CommandCandidate(model, new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    private static bool HasCommandAttribute(INamedTypeSymbol type)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == CommandAttributeName)
            {
                return true;
            }
        }

        return false;
    }

    private static string[] ReadAliases(AttributeData attribute)
    {
        var argument = attribute.NamedArguments.FirstOrDefault(static item => item.Key == "Aliases");

        if (argument.Value.Kind != TypedConstantKind.Array || argument.Value.IsNull)
        {
            return Array.Empty<string>();
        }

        var values = argument.Value.Values;
        var result = new string[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            result[i] = values[i].Value as string ?? string.Empty;
        }

        return result;
    }

    private static bool IsReservedName(string name)
    {
        foreach (var reserved in ReservedNames)
        {
            if (string.Equals(name, reserved, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidName(string name)
    {
        if (name.Length == 0 || name[0] == '-')
        {
            return false;
        }

        foreach (var c in name)
        {
            if (!(char.IsLower(c) || char.IsDigit(c) || c == '-' || c == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static List<IMethodSymbol> FindHandlers(INamedTypeSymbol type)
    {
        var result = new List<IMethodSymbol>();

        foreach (var member in type.GetMembers())
        {
            if (member is not IMethodSymbol method ||
                method.MethodKind != MethodKind.Ordinary ||
                method.IsGenericMethod ||
                (method.DeclaredAccessibility != Accessibility.Public &&
                 method.DeclaredAccessibility != Accessibility.Internal))
            {
                continue;
            }

            foreach (var name in HandlerNames)
            {
                if (method.Name == name)
                {
                    result.Add(method);
                    break;
                }
            }
        }

        return result;
    }

    private static bool TryMapReturnKind(
        IMethodSymbol method,
        Compilation compilation,
        out HandlerReturnKind kind)
    {
        kind = HandlerReturnKind.None;

        if (method.ReturnsVoid)
        {
            kind = HandlerReturnKind.Void;
            return true;
        }

        var returnType = method.ReturnType;

        if (returnType.SpecialType == SpecialType.System_Int32)
        {
            kind = HandlerReturnKind.Int;
            return true;
        }

        var task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var taskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var valueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        if (SymbolEqualityComparer.Default.Equals(returnType, task))
        {
            kind = HandlerReturnKind.Task;
            return true;
        }

        if (SymbolEqualityComparer.Default.Equals(returnType, valueTask))
        {
            kind = HandlerReturnKind.ValueTask;
            return true;
        }

        if (returnType is not INamedTypeSymbol named || named.TypeArguments.Length != 1)
        {
            return false;
        }

        if (named.TypeArguments[0].SpecialType != SpecialType.System_Int32)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, taskOfT))
        {
            kind = HandlerReturnKind.TaskOfInt;
            return true;
        }

        if (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, valueTaskOfT))
        {
            kind = HandlerReturnKind.ValueTaskOfInt;
            return true;
        }

        return false;
    }

    private static bool IsAwaitable(HandlerReturnKind kind)
    {
        return kind is HandlerReturnKind.Task or HandlerReturnKind.TaskOfInt
            or HandlerReturnKind.ValueTask or HandlerReturnKind.ValueTaskOfInt;
    }

    private static bool HasParameterlessConstructor(INamedTypeSymbol type)
    {
        foreach (var constructor in type.InstanceConstructors)
        {
            if (constructor.Parameters.Length == 0 &&
                (constructor.DeclaredAccessibility == Accessibility.Public ||
                 constructor.DeclaredAccessibility == Accessibility.Internal))
            {
                return true;
            }
        }

        return false;
    }

    private static string DeriveName(string typeName)
    {
        const string suffix = "Command";

        if (typeName.Length > suffix.Length && typeName.EndsWith(suffix, StringComparison.Ordinal))
        {
            typeName = typeName.Substring(0, typeName.Length - suffix.Length);
        }

        var builder = new StringBuilder(typeName.Length + 4);

        for (var i = 0; i < typeName.Length; i++)
        {
            var c = typeName[i];

            if (char.IsUpper(c) && i > 0 &&
                (char.IsLower(typeName[i - 1]) || char.IsDigit(typeName[i - 1])))
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.Length == 0 ? "command" : builder.ToString();
    }

    private static string? GetSummary(INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        var xml = type.GetDocumentationCommentXml(cancellationToken: cancellationToken);

        if (xml is null || xml.Length == 0)
        {
            return null;
        }

        xml = xml.Trim();

        if (xml.Length == 0)
        {
            return null;
        }

        const string openTag = "<summary>";
        const string closeTag = "</summary>";

        var start = xml.IndexOf(openTag, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += openTag.Length;

        var end = xml.IndexOf(closeTag, start, StringComparison.Ordinal);
        if (end < 0)
        {
            return null;
        }

        var summary = xml.Substring(start, end - start);
        summary = summary.Replace("\r", " ").Replace("\n", " ");
        summary = StripTags(summary);

        while (summary.Contains("  "))
        {
            summary = summary.Replace("  ", " ");
        }

        summary = summary.Trim();
        return summary.Length == 0 ? null : summary;
    }

    private static string StripTags(string text)
    {
        var builder = new StringBuilder(text.Length);
        var insideTag = false;

        foreach (var c in text)
        {
            if (c == '<')
            {
                insideTag = true;
            }
            else if (c == '>')
            {
                insideTag = false;
            }
            else if (!insideTag)
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
