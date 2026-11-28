using System;
using System.Collections.Generic;
using System.Linq;
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
            return new CommandCandidate(null, EquatableArray<DiagnosticInfo>.Empty);
        }

        var location = LocationInfo.From(context.TargetNode.GetLocation());
        var compilation = context.SemanticModel.Compilation;
        var appName = compilation.AssemblyName ?? "app";

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

        var name = string.IsNullOrWhiteSpace(explicitName) ? Names.ToCommandName(type.Name) : explicitName!.Trim();

        if (name.Length == 0)
        {
            diagnostics.Add(new DiagnosticInfo("RHL026", location, type.Name));
            return CreateResult(null, diagnostics);
        }

        if (!Names.IsValidCliName(name))
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
            if (!Names.IsValidCliName(alias))
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
            description = DocumentationReader.GetSummary(type, cancellationToken);
        }

        description ??= string.Empty;

        if (description.Length == 0)
        {
            diagnostics.Add(new DiagnosticInfo("RHL005", location, name));
        }

        var isHidden = attribute.NamedArguments
            .FirstOrDefault(static argument => argument.Key == "IsHidden")
            .Value.Value is true;

        var isDestructive = attribute.NamedArguments
            .FirstOrDefault(static argument => argument.Key == "IsDestructive")
            .Value.Value is true;

        var filters = ReadFilters(attribute, compilation, name, location, diagnostics);

        if (filters is null)
        {
            return CreateResult(null, diagnostics);
        }

        var examples = DocumentationReader.GetExamples(type, cancellationToken);
        var handlers = FindHandlers(type);

        if (handlers.Count > 1)
        {
            diagnostics.Add(new DiagnosticInfo("RHL007", location, name));
            return CreateResult(null, diagnostics);
        }

        var handler = handlers.Count == 1 ? handlers[0] : null;
        var returnKind = HandlerReturnKind.None;
        string? handlerName = null;

        if (handler is not null)
        {
            if (handler.Parameters.Length > 0)
            {
                diagnostics.Add(new DiagnosticInfo("RHL004", location, type.Name, handler.Name));
                return CreateResult(null, diagnostics);
            }

            if (!TryMapReturnKind(handler, compilation, out returnKind))
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

            handlerName = handler.Name;
        }

        var hasParameterlessConstructor = true;
        var constructorParameters = EquatableArray<string>.Empty;

        if (!type.IsStatic)
        {
            if (!TryReadConstructor(type, out hasParameterlessConstructor, out var parameters))
            {
                diagnostics.Add(new DiagnosticInfo("RHL020", location, name));
                return CreateResult(null, diagnostics);
            }

            constructorParameters = parameters;
        }

        var binding = BindingModelFactory.Create(compilation, type, location, cancellationToken);
        diagnostics.AddRange(binding.Diagnostics);

        if (binding.Diagnostics.Count > 0)
        {
            return CreateResult(null, diagnostics);
        }

        var model = new CommandModel(
            appName,
            name,
            description,
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            parentTypeFullName,
            type.IsStatic,
            isHidden,
            isDestructive,
            new EquatableArray<string>(aliases),
            new EquatableArray<string>(filters),
            new EquatableArray<OptionModel>(binding.Options.ToArray()),
            new EquatableArray<ParameterModel>(binding.Parameters.ToArray()),
            new EquatableArray<string>(examples),
            constructorParameters,
            hasParameterlessConstructor,
            handlerName,
            returnKind,
            location);

        return CreateResult(model, diagnostics);
    }

    private static CommandCandidate CreateResult(CommandModel? model, List<DiagnosticInfo> diagnostics)
    {
        return new CommandCandidate(model, new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()));
    }

    private static string[]? ReadFilters(
        AttributeData attribute,
        Compilation compilation,
        string commandName,
        LocationInfo location,
        List<DiagnosticInfo> diagnostics)
    {
        var argument = attribute.NamedArguments.FirstOrDefault(static item => item.Key == "Filters");

        if (argument.Value.Kind != TypedConstantKind.Array || argument.Value.IsNull)
        {
            return Array.Empty<string>();
        }

        var filterInterface = compilation.GetTypeByMetadataName("RunesHela.IRunesHelaFilter");
        var result = new List<string>();

        foreach (var item in argument.Value.Values)
        {
            var display = item.Value is ITypeSymbol filterSymbol ? filterSymbol.ToDisplayString() : "?";

            if (item.Value is not INamedTypeSymbol filterType ||
                filterType.TypeKind != Microsoft.CodeAnalysis.TypeKind.Class ||
                filterType.IsGenericType ||
                filterType.IsAbstract)
            {
                diagnostics.Add(new DiagnosticInfo("RHL037", location, display, commandName));
                return null;
            }

            var implementsContract = false;

            if (filterInterface is not null)
            {
                foreach (var @interface in filterType.AllInterfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(@interface, filterInterface))
                    {
                        implementsContract = true;
                        break;
                    }
                }
            }

            if (!implementsContract)
            {
                diagnostics.Add(new DiagnosticInfo("RHL038", location, display, commandName));
                return null;
            }

            var hasConstructor = false;

            foreach (var constructor in filterType.InstanceConstructors)
            {
                if (constructor.Parameters.Length == 0 &&
                    (constructor.DeclaredAccessibility == Accessibility.Public ||
                     constructor.DeclaredAccessibility == Accessibility.Internal))
                {
                    hasConstructor = true;
                    break;
                }
            }

            if (!hasConstructor)
            {
                diagnostics.Add(new DiagnosticInfo("RHL039", location, display, commandName));
                return null;
            }

            result.Add(filterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        return result.ToArray();
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

    private static bool TryReadConstructor(
        INamedTypeSymbol type,
        out bool hasParameterlessConstructor,
        out EquatableArray<string> parameters)
    {
        hasParameterlessConstructor = true;
        parameters = EquatableArray<string>.Empty;

        var constructors = new List<IMethodSymbol>();

        foreach (var constructor in type.InstanceConstructors)
        {
            if (constructor.DeclaredAccessibility == Accessibility.Public ||
                constructor.DeclaredAccessibility == Accessibility.Internal)
            {
                constructors.Add(constructor);
            }
        }

        foreach (var constructor in constructors)
        {
            if (constructor.Parameters.Length == 0)
            {
                return true;
            }
        }

        if (constructors.Count != 1)
        {
            return false;
        }

        var values = new string[constructors[0].Parameters.Length];

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = constructors[0].Parameters[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        hasParameterlessConstructor = false;
        parameters = new EquatableArray<string>(values);
        return true;
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
}
