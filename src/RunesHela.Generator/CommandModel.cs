using Microsoft.CodeAnalysis;

namespace RunesHela.Generator;

internal enum HandlerReturnKind
{
    None,
    Void,
    Int,
    Task,
    TaskOfInt,
    ValueTask,
    ValueTaskOfInt,
}

internal sealed class CommandModel
{
    public CommandModel(
        string appName,
        string name,
        string description,
        string typeFullName,
        bool isStatic,
        bool isHidden,
        string? handlerName,
        HandlerReturnKind returnKind,
        Location? location)
    {
        AppName = appName;
        Name = name;
        Description = description;
        TypeFullName = typeFullName;
        IsStatic = isStatic;
        IsHidden = isHidden;
        HandlerName = handlerName;
        ReturnKind = returnKind;
        Location = location;
    }

    public string AppName { get; }

    public string Name { get; }

    public string Description { get; }

    public string TypeFullName { get; }

    public bool IsStatic { get; }

    public bool IsHidden { get; }

    public string? HandlerName { get; }

    public HandlerReturnKind ReturnKind { get; }

    public Location? Location { get; }
}

internal sealed class DiagnosticInfo
{
    public DiagnosticInfo(DiagnosticDescriptor descriptor, Location? location, object?[] args)
    {
        Descriptor = descriptor;
        Location = location;
        Args = args;
    }

    public DiagnosticDescriptor Descriptor { get; }

    public Location? Location { get; }

    public object?[] Args { get; }

    public Diagnostic ToDiagnostic()
    {
        return Diagnostic.Create(Descriptor, Location, Args);
    }
}

internal sealed class CommandCandidate
{
    public CommandCandidate(CommandModel? model, System.Collections.Immutable.ImmutableArray<DiagnosticInfo> diagnostics)
    {
        Model = model;
        Diagnostics = diagnostics;
    }

    public CommandModel? Model { get; }

    public System.Collections.Immutable.ImmutableArray<DiagnosticInfo> Diagnostics { get; }
}
