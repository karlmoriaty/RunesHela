using Microsoft.CodeAnalysis;

namespace RunesHela.Generator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor DuplicateCommandName = new(
        id: "RHL001",
        title: "Duplicate command name",
        messageFormat: "Command name '{0}' is used by more than one type; only the first registration will be generated",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedHandlerParameters = new(
        id: "RHL004",
        title: "Unsupported handler parameters",
        messageFormat: "Handler '{0}' method '{1}' has parameters which are not supported in M0",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoHandler = new(
        id: "RHL006",
        title: "No handler found",
        messageFormat: "Command '{0}' has no public or internal Run, RunAsync, Execute, or ExecuteAsync method",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoParameterlessConstructor = new(
        id: "RHL020",
        title: "No parameterless constructor",
        messageFormat: "Command '{0}' must have a public or internal parameterless constructor or a static handler",
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

    public static readonly DiagnosticDescriptor NestedCommandNotSupported = new(
        id: "RHL023",
        title: "Nested commands are not supported in M0",
        messageFormat: "Command '{0}' is nested and nested routing is not supported in M0",
        category: "RunesHela",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericCommandNotSupported = new(
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
}
