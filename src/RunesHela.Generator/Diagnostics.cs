using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace RunesHela.Generator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor DuplicateCommandName = new(
        id: "RHL001",
        title: "Duplicate command name",
        messageFormat: "Command name '{0}' is used more than once under the same parent",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateOptionName = new(
        id: "RHL002",
        title: "Duplicate option name",
        messageFormat: "Option name '{0}' is used more than once on command '{1}'",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RequiredAndDefault = new(
        id: "RHL003",
        title: "Required member with default value",
        messageFormat: "Option '{0}' is required and also has a default value",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedHandlerParameters = new(
        id: "RHL004",
        title: "Unsupported handler parameters",
        messageFormat: "Handler '{0}' method '{1}' has parameters which are not supported in M1",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingDescription = new(
        id: "RHL005",
        title: "Missing command description",
        messageFormat: "Command '{0}' has no description; add a summary document comment or Description",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoHandler = new(
        id: "RHL006",
        title: "No handler found",
        messageFormat: "Command '{0}' has no public or internal Run, RunAsync, Execute, or ExecuteAsync method",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleHandlers = new(
        id: "RHL007",
        title: "Multiple handlers found",
        messageFormat: "Command '{0}' has more than one handler method; keep only one of Run, RunAsync, Execute, or ExecuteAsync",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DestructiveWithoutConfirmation = new(
        id: "RHL008",
        title: "Destructive command without confirmation guard",
        messageFormat: "Destructive command '{0}' should support a --yes guard",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor OptionalBeforeRequired = new(
        id: "RHL009",
        title: "Optional parameter before required parameter",
        messageFormat: "Parameter '{0}' is optional but is followed by required parameter '{1}'",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicatePosition = new(
        id: "RHL010",
        title: "Duplicate parameter position",
        messageFormat: "Parameter position '{0}' is used more than once on command '{1}'",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ParamsNotLast = new(
        id: "RHL011",
        title: "Params parameter is not last",
        messageFormat: "Params parameter '{0}' must be the last parameter",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AsyncNameMismatch = new(
        id: "RHL012",
        title: "Async handler name mismatch",
        messageFormat: "Handler '{0}' returns an awaitable type but is not named with an Async suffix",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidCommandName = new(
        id: "RHL013",
        title: "Invalid command name",
        messageFormat: "Command name '{0}' is invalid; use lowercase letters, digits, '-' or '_'",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ReservedOptionName = new(
        id: "RHL014",
        title: "Reserved option name",
        messageFormat: "Option name '{0}' is reserved by the runtime",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SecretNotMarked = new(
        id: "RHL015",
        title: "Sensitive option not marked as secret",
        messageFormat: "Option '{0}' looks sensitive but is not marked with Secret",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConfigKeyConflict = new(
        id: "RHL016",
        title: "Configuration key conflict",
        messageFormat: "Configuration key '{0}' is used more than once on command '{1}'",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingExample = new(
        id: "RHL017",
        title: "Missing example",
        messageFormat: "Command '{0}' has no example; add an example document comment",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NonIdempotentWithoutDryRun = new(
        id: "RHL018",
        title: "Non-idempotent command without dry-run",
        messageFormat: "Command '{0}' is not idempotent and has no dry-run option",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CircularNesting = new(
        id: "RHL019",
        title: "Circular command nesting",
        messageFormat: "Command '{0}' has a circular parent chain",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoParameterlessConstructor = new(
        id: "RHL020",
        title: "No usable constructor",
        messageFormat: "Command '{0}' must have an accessible parameterless constructor or a single accessible constructor whose parameters can be resolved from IServiceProvider",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedReturnType = new(
        id: "RHL021",
        title: "Unsupported handler return type",
        messageFormat: "Handler '{0}' method '{1}' returns '{2}' which is not supported; use void, int, Task, Task<int>, ValueTask, or ValueTask<int>",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateAlias = new(
        id: "RHL022",
        title: "Duplicate alias",
        messageFormat: "Alias '{0}' is used more than once on command '{1}'",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NestedParentNotCommand = new(
        id: "RHL023",
        title: "Nested command parent is not a command",
        messageFormat: "Nested command '{0}' must be declared inside a type marked with Command",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericCommand = new(
        id: "RHL024",
        title: "Generic commands are not supported",
        messageFormat: "Command '{0}' is generic and generic command types are not supported",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ReservedCommandName = new(
        id: "RHL025",
        title: "Reserved command name",
        messageFormat: "Command name '{0}' is reserved by the runtime",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor EmptyCommandName = new(
        id: "RHL026",
        title: "Empty command name",
        messageFormat: "Command on type '{0}' has an empty name",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidAlias = new(
        id: "RHL027",
        title: "Invalid command alias",
        messageFormat: "Alias '{0}' on command '{1}' is invalid; use lowercase letters, digits, '-' or '_'",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AliasConflictsWithSibling = new(
        id: "RHL028",
        title: "Alias conflicts with a sibling command",
        messageFormat: "Alias '{0}' on command '{1}' conflicts with another command under the same parent",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedType = new(
        id: "RHL029",
        title: "Unsupported value type",
        messageFormat: "Type '{0}' is not supported; implement IParsable<T> or use a supported type",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidOptionName = new(
        id: "RHL030",
        title: "Invalid option name",
        messageFormat: "Option name '{0}' is invalid; use lowercase letters, digits, '-' or '_'",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidBoundMember = new(
        id: "RHL031",
        title: "Invalid bound member",
        messageFormat: "Member '{0}' cannot be bound: {1}",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NonConstantDefault = new(
        id: "RHL032",
        title: "Default value is not a compile-time constant",
        messageFormat: "Member '{0}' has an initializer that is not a compile-time constant; use a constant literal or set the value from the command line",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidShortName = new(
        id: "RHL033",
        title: "Invalid short option name",
        messageFormat: "Short option name '{0}' is invalid; use a single letter or digit",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidArgumentPosition = new(
        id: "RHL034",
        title: "Invalid argument position",
        messageFormat: "Argument position '{0}' on member '{1}' is invalid; use a non-negative integer",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidValidation = new(
        id: "RHL035",
        title: "Invalid validation attribute",
        messageFormat: "Validation on member '{0}' is not supported: {1}",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConflictingBindingAttributes = new(
        id: "RHL036",
        title: "Conflicting binding attributes",
        messageFormat: "Member '{0}' cannot be both an option and an argument",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidFilterType = new(
        id: "RHL037",
        title: "Invalid filter type",
        messageFormat: "Filter '{0}' on command '{1}' is not a supported class",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FilterContract = new(
        id: "RHL038",
        title: "Filter does not implement IRunesHelaFilter",
        messageFormat: "Filter '{0}' on command '{1}' must implement RunesHela.IRunesHelaFilter",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FilterConstructor = new(
        id: "RHL039",
        title: "Filter has no usable constructor",
        messageFormat: "Filter '{0}' on command '{1}' must have a public or internal parameterless constructor",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidConfigKey = new(
        id: "RHL040",
        title: "Invalid configuration key",
        messageFormat: "Configuration key '{0}' is invalid; use dot-separated segments of letters, digits, '-' or '_'",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GeneratorError = new(
        id: "RHL999",
        title: "Generator error",
        messageFormat: "The RunesHela generator failed: {0}",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly ImmutableArray<DiagnosticDescriptor> All = ImmutableArray.Create(
        DuplicateCommandName,
        DuplicateOptionName,
        RequiredAndDefault,
        UnsupportedHandlerParameters,
        MissingDescription,
        NoHandler,
        MultipleHandlers,
        DestructiveWithoutConfirmation,
        OptionalBeforeRequired,
        DuplicatePosition,
        ParamsNotLast,
        AsyncNameMismatch,
        InvalidCommandName,
        ReservedOptionName,
        SecretNotMarked,
        ConfigKeyConflict,
        MissingExample,
        NonIdempotentWithoutDryRun,
        CircularNesting,
        NoParameterlessConstructor,
        UnsupportedReturnType,
        DuplicateAlias,
        NestedParentNotCommand,
        GenericCommand,
        ReservedCommandName,
        EmptyCommandName,
        InvalidAlias,
        AliasConflictsWithSibling,
        UnsupportedType,
        InvalidOptionName,
        InvalidBoundMember,
        NonConstantDefault,
        InvalidShortName,
        InvalidArgumentPosition,
        InvalidValidation,
        ConflictingBindingAttributes,
        InvalidFilterType,
        FilterContract,
        FilterConstructor,
        InvalidConfigKey,
        GeneratorError);

    private static readonly Dictionary<string, DiagnosticDescriptor> ById = CreateById();

    public static DiagnosticDescriptor Get(string id)
    {
        return ById[id];
    }

    private static Dictionary<string, DiagnosticDescriptor> CreateById()
    {
        var result = new Dictionary<string, DiagnosticDescriptor>(StringComparer.Ordinal);

        foreach (var descriptor in All)
        {
            result.Add(descriptor.Id, descriptor);
        }

        return result;
    }
}
