using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace RunesHela.Generator;

internal static class CommandModelFactory
{
    public static CommandCandidate Create(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        if (context.TargetSymbol is not INamedTypeSymbol type)
        {
            return new CommandCandidate(null, diagnostics.ToImmutable());
        }

        var location = context.TargetNode.GetLocation();
        var appName = context.SemanticModel.Compilation.AssemblyName ?? "app";

        if (type.ContainingType is not null)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.NestedCommandNotSupported, location, new object?[] { type.Name }));
            return new CommandCandidate(null, diagnostics.ToImmutable());
        }

        if (type.IsGenericType)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.GenericCommandNotSupported, location, new object?[] { type.Name }));
            return new CommandCandidate(null, diagnostics.ToImmutable());
        }

        var attribute = context.Attributes[0];
        var name = attribute.ConstructorArguments.Length > 0
            ? attribute.ConstructorArguments[0].Value as string
            : null;

        if (string.IsNullOrWhiteSpace(name))
        {
            name = DeriveName(type.Name);
        }

        name = name!.Trim();

        if (name is "-h" or "--help" or "--version")
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.ReservedCommandName, location, new object?[] { name }));
            return new CommandCandidate(null, diagnostics.ToImmutable());
        }

        var description = attribute.NamedArguments
            .FirstOrDefault(static argument => argument.Key == "Description")
            .Value.Value as string;

        if (string.IsNullOrWhiteSpace(description))
        {
            description = GetSummary(type, cancellationToken);
        }

        description ??= string.Empty;

        var isHidden = attribute.NamedArguments
            .FirstOrDefault(static argument => argument.Key == "IsHidden")
            .Value.Value is true;

        var handler = FindHandler(type);
        var returnKind = HandlerReturnKind.None;
        string? handlerName = null;

        if (handler is null)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.NoHandler, location, new object?[] { name }));
        }
        else if (handler.Parameters.Length > 0)
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.UnsupportedHandlerParameters,
                location,
                new object?[] { type.Name, handler.Name }));
            return new CommandCandidate(null, diagnostics.ToImmutable());
        }
        else if (!TryMapReturnKind(handler, context.SemanticModel.Compilation, out returnKind))
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.UnsupportedReturnType,
                location,
                new object?[] { type.Name, handler.Name, handler.ReturnType.ToDisplayString() }));
            return new CommandCandidate(null, diagnostics.ToImmutable());
        }
        else
        {
            handlerName = handler.Name;
        }

        if (!type.IsStatic && !HasParameterlessConstructor(type))
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.NoParameterlessConstructor,
                location,
                new object?[] { name }));
            return new CommandCandidate(null, diagnostics.ToImmutable());
        }

        var model = new CommandModel(
            appName,
            name,
            description,
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            type.IsStatic,
            isHidden,
            handlerName,
            returnKind,
            location);

        return new CommandCandidate(model, diagnostics.ToImmutable());
    }

    private static IMethodSymbol? FindHandler(INamedTypeSymbol type)
    {
        var methods = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(static method =>
                method.MethodKind == MethodKind.Ordinary &&
                !method.IsGenericMethod &&
                (method.DeclaredAccessibility == Accessibility.Public ||
                 method.DeclaredAccessibility == Accessibility.Internal))
            .ToArray();

        var names = new[] { "RunAsync", "Run", "ExecuteAsync", "Execute" };

        foreach (var name in names)
        {
            foreach (var method in methods)
            {
                if (method.Name == name)
                {
                    return method;
                }
            }
        }

        return null;
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

        if (typeName.Length > suffix.Length && typeName.EndsWith(suffix, System.StringComparison.Ordinal))
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

        var start = xml.IndexOf(openTag, System.StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += openTag.Length;

        var end = xml.IndexOf(closeTag, start, System.StringComparison.Ordinal);
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
