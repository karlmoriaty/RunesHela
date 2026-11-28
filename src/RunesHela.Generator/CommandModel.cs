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

internal enum ValidationKind
{
    Range,
    Regex,
    Length,
}

internal sealed class ValidationModel : IEquatable<ValidationModel>
{
    public ValidationModel(ValidationKind kind, string min, string max, string pattern)
    {
        Kind = kind;
        Min = min;
        Max = max;
        Pattern = pattern;
    }

    public ValidationKind Kind { get; }

    public string Min { get; }

    public string Max { get; }

    public string Pattern { get; }

    public bool Equals(ValidationModel? other)
    {
        return other is not null &&
               Kind == other.Kind &&
               Min == other.Min &&
               Max == other.Max &&
               Pattern == other.Pattern;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ValidationModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (int)Kind;
            hash = (hash * 397) ^ Min.GetHashCode();
            hash = (hash * 397) ^ Max.GetHashCode();
            hash = (hash * 397) ^ Pattern.GetHashCode();
            return hash;
        }
    }
}

internal sealed class OptionModel : IEquatable<OptionModel>
{
    public OptionModel(
        string propertyName,
        string longName,
        char shortName,
        EquatableArray<string> aliases,
        string valueName,
        TypeModel type,
        bool isRequired,
        bool hasDefault,
        string defaultLiteral,
        string defaultDisplay,
        string? envVar,
        bool isHidden,
        bool isSecret,
        string description,
        EquatableArray<ValidationModel> validations)
    {
        PropertyName = propertyName;
        LongName = longName;
        ShortName = shortName;
        Aliases = aliases;
        ValueName = valueName;
        Type = type;
        IsRequired = isRequired;
        HasDefault = hasDefault;
        DefaultLiteral = defaultLiteral;
        DefaultDisplay = defaultDisplay;
        EnvVar = envVar;
        IsHidden = isHidden;
        IsSecret = isSecret;
        Description = description;
        Validations = validations;
    }

    public string PropertyName { get; }

    public string LongName { get; }

    public char ShortName { get; }

    public EquatableArray<string> Aliases { get; }

    public string ValueName { get; }

    public TypeModel Type { get; }

    public bool IsRequired { get; }

    public bool HasDefault { get; }

    public string DefaultLiteral { get; }

    public string DefaultDisplay { get; }

    public string? EnvVar { get; }

    public bool IsHidden { get; }

    public bool IsSecret { get; }

    public string Description { get; }

    public EquatableArray<ValidationModel> Validations { get; }

    public bool IsRepeatable => Type.IsArray;

    public bool IsFlag => Type.Kind == TypeKind.Boolean && !Type.IsArray;

    public bool IsNegatable => IsFlag && !IsRepeatable;

    public bool Equals(OptionModel? other)
    {
        return other is not null &&
               PropertyName == other.PropertyName &&
               LongName == other.LongName &&
               ShortName == other.ShortName &&
               Aliases == other.Aliases &&
               ValueName == other.ValueName &&
               Type.Equals(other.Type) &&
               IsRequired == other.IsRequired &&
               HasDefault == other.HasDefault &&
               DefaultLiteral == other.DefaultLiteral &&
               DefaultDisplay == other.DefaultDisplay &&
               EnvVar == other.EnvVar &&
               IsHidden == other.IsHidden &&
               IsSecret == other.IsSecret &&
               Description == other.Description &&
               Validations == other.Validations;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as OptionModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = PropertyName.GetHashCode();
            hash = (hash * 397) ^ LongName.GetHashCode();
            hash = (hash * 397) ^ ShortName.GetHashCode();
            hash = (hash * 397) ^ Aliases.GetHashCode();
            hash = (hash * 397) ^ ValueName.GetHashCode();
            hash = (hash * 397) ^ Type.GetHashCode();
            hash = (hash * 397) ^ (IsRequired ? 1 : 0);
            hash = (hash * 397) ^ (HasDefault ? 1 : 0);
            hash = (hash * 397) ^ DefaultLiteral.GetHashCode();
            hash = (hash * 397) ^ DefaultDisplay.GetHashCode();
            hash = (hash * 397) ^ (EnvVar?.GetHashCode() ?? 0);
            hash = (hash * 397) ^ (IsHidden ? 1 : 0);
            hash = (hash * 397) ^ (IsSecret ? 1 : 0);
            hash = (hash * 397) ^ Description.GetHashCode();
            hash = (hash * 397) ^ Validations.GetHashCode();
            return hash;
        }
    }
}

internal sealed class ParameterModel : IEquatable<ParameterModel>
{
    public ParameterModel(
        string propertyName,
        string name,
        int position,
        TypeModel type,
        bool isRequired,
        bool isParams,
        bool hasDefault,
        string defaultLiteral,
        string defaultDisplay,
        string description,
        EquatableArray<ValidationModel> validations)
    {
        PropertyName = propertyName;
        Name = name;
        Position = position;
        Type = type;
        IsRequired = isRequired;
        IsParams = isParams;
        HasDefault = hasDefault;
        DefaultLiteral = defaultLiteral;
        DefaultDisplay = defaultDisplay;
        Description = description;
        Validations = validations;
    }

    public string PropertyName { get; }

    public string Name { get; }

    public int Position { get; }

    public TypeModel Type { get; }

    public bool IsRequired { get; }

    public bool IsParams { get; }

    public bool HasDefault { get; }

    public string DefaultLiteral { get; }

    public string DefaultDisplay { get; }

    public string Description { get; }

    public EquatableArray<ValidationModel> Validations { get; }

    public bool IsRepeatable => Type.IsArray;

    public bool Equals(ParameterModel? other)
    {
        return other is not null &&
               PropertyName == other.PropertyName &&
               Name == other.Name &&
               Position == other.Position &&
               Type.Equals(other.Type) &&
               IsRequired == other.IsRequired &&
               IsParams == other.IsParams &&
               HasDefault == other.HasDefault &&
               DefaultLiteral == other.DefaultLiteral &&
               DefaultDisplay == other.DefaultDisplay &&
               Description == other.Description &&
               Validations == other.Validations;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as ParameterModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = PropertyName.GetHashCode();
            hash = (hash * 397) ^ Name.GetHashCode();
            hash = (hash * 397) ^ Position;
            hash = (hash * 397) ^ Type.GetHashCode();
            hash = (hash * 397) ^ (IsRequired ? 1 : 0);
            hash = (hash * 397) ^ (IsParams ? 1 : 0);
            hash = (hash * 397) ^ (HasDefault ? 1 : 0);
            hash = (hash * 397) ^ DefaultLiteral.GetHashCode();
            hash = (hash * 397) ^ DefaultDisplay.GetHashCode();
            hash = (hash * 397) ^ Description.GetHashCode();
            hash = (hash * 397) ^ Validations.GetHashCode();
            return hash;
        }
    }
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
        bool isDestructive,
        EquatableArray<string> aliases,
        EquatableArray<string> filters,
        EquatableArray<OptionModel> options,
        EquatableArray<ParameterModel> parameters,
        EquatableArray<string> examples,
        EquatableArray<string> constructorParameters,
        bool hasParameterlessConstructor,
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
        IsDestructive = isDestructive;
        Aliases = aliases;
        Filters = filters;
        Options = options;
        Parameters = parameters;
        Examples = examples;
        ConstructorParameters = constructorParameters;
        HasParameterlessConstructor = hasParameterlessConstructor;
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

    public bool IsDestructive { get; }

    public EquatableArray<string> Aliases { get; }

    public EquatableArray<string> Filters { get; }

    public EquatableArray<OptionModel> Options { get; }

    public EquatableArray<ParameterModel> Parameters { get; }

    public EquatableArray<string> Examples { get; }

    public EquatableArray<string> ConstructorParameters { get; }

    public bool HasParameterlessConstructor { get; }

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
               IsDestructive == other.IsDestructive &&
               Aliases == other.Aliases &&
               Filters == other.Filters &&
               Options == other.Options &&
               Parameters == other.Parameters &&
               Examples == other.Examples &&
               ConstructorParameters == other.ConstructorParameters &&
               HasParameterlessConstructor == other.HasParameterlessConstructor &&
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
            hash = (hash * 397) ^ (IsDestructive ? 1 : 0);
            hash = (hash * 397) ^ Aliases.GetHashCode();
            hash = (hash * 397) ^ Filters.GetHashCode();
            hash = (hash * 397) ^ Options.GetHashCode();
            hash = (hash * 397) ^ Parameters.GetHashCode();
            hash = (hash * 397) ^ Examples.GetHashCode();
            hash = (hash * 397) ^ ConstructorParameters.GetHashCode();
            hash = (hash * 397) ^ (HasParameterlessConstructor ? 1 : 0);
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
