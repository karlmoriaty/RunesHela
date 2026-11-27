using System;
using System.Collections.Generic;

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

internal sealed class CommandModel : IEquatable<CommandModel>
{
    public CommandModel(
        string appName,
        string name,
        string description,
        string typeFullName,
        string? parentTypeFullName,
        bool isStatic,
        bool isHidden,
        EquatableArray<string> aliases,
        string? handlerName,
        HandlerReturnKind returnKind,
        LocationInfo location)
    {
        AppName = appName;
        Name = name;
        Description = description;
        TypeFullName = typeFullName;
        ParentTypeFullName = parentTypeFullName;
        IsStatic = isStatic;
        IsHidden = isHidden;
        Aliases = aliases;
        HandlerName = handlerName;
        ReturnKind = returnKind;
        Location = location;
    }

    public string AppName { get; }

    public string Name { get; }

    public string Description { get; }

    public string TypeFullName { get; }

    public string? ParentTypeFullName { get; }

    public bool IsStatic { get; }

    public bool IsHidden { get; }

    public EquatableArray<string> Aliases { get; }

    public string? HandlerName { get; }

    public HandlerReturnKind ReturnKind { get; }

    public LocationInfo Location { get; }

    public bool Equals(CommandModel? other)
    {
        return other is not null &&
               AppName == other.AppName &&
               Name == other.Name &&
               Description == other.Description &&
               TypeFullName == other.TypeFullName &&
               ParentTypeFullName == other.ParentTypeFullName &&
               IsStatic == other.IsStatic &&
               IsHidden == other.IsHidden &&
               Aliases == other.Aliases &&
               HandlerName == other.HandlerName &&
               ReturnKind == other.ReturnKind &&
               Location.Equals(other.Location);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CommandModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = AppName.GetHashCode();
            hash = (hash * 397) ^ Name.GetHashCode();
            hash = (hash * 397) ^ Description.GetHashCode();
            hash = (hash * 397) ^ TypeFullName.GetHashCode();
            hash = (hash * 397) ^ (ParentTypeFullName?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (IsStatic ? 1 : 0);
            hash = (hash * 397) ^ (IsHidden ? 1 : 0);
            hash = (hash * 397) ^ Aliases.GetHashCode();
            hash = (hash * 397) ^ (HandlerName?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (int)ReturnKind;
            hash = (hash * 397) ^ Location.GetHashCode();
            return hash;
        }
    }
}

internal sealed class DiagnosticInfo : IEquatable<DiagnosticInfo>
{
    public DiagnosticInfo(string id, LocationInfo location, params string[] args)
    {
        Id = id;
        Location = location;
        Args = new EquatableArray<string>(args);
    }

    public string Id { get; }

    public LocationInfo Location { get; }

    public EquatableArray<string> Args { get; }

    public Microsoft.CodeAnalysis.Diagnostic ToDiagnostic()
    {
        var arguments = new object?[Args.Count];

        for (var i = 0; i < Args.Count; i++)
        {
            arguments[i] = Args[i];
        }

        return Microsoft.CodeAnalysis.Diagnostic.Create(
            Diagnostics.Get(Id),
            Location.ToLocation(),
            arguments);
    }

    public bool Equals(DiagnosticInfo? other)
    {
        return other is not null &&
               Id == other.Id &&
               Location.Equals(other.Location) &&
               Args == other.Args;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DiagnosticInfo);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Id.GetHashCode();
            hash = (hash * 397) ^ Location.GetHashCode();
            hash = (hash * 397) ^ Args.GetHashCode();
            return hash;
        }
    }
}

internal sealed class CommandCandidate : IEquatable<CommandCandidate>
{
    public CommandCandidate(CommandModel? model, EquatableArray<DiagnosticInfo> diagnostics)
    {
        Model = model;
        Diagnostics = diagnostics;
    }

    public CommandModel? Model { get; }

    public EquatableArray<DiagnosticInfo> Diagnostics { get; }

    public bool Equals(CommandCandidate? other)
    {
        return other is not null &&
               Equals(Model, other.Model) &&
               Diagnostics == other.Diagnostics;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CommandCandidate);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Model?.GetHashCode() ?? 0) * 397) ^ Diagnostics.GetHashCode();
        }
    }
}

internal sealed class CommandNode
{
    public CommandNode(CommandModel model)
    {
        Model = model;
    }

    public CommandModel Model { get; }

    public List<CommandNode> Children { get; } = new();

    public string Path { get; set; } = string.Empty;
}
