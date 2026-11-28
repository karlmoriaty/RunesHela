using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RunesHela.Generator;

internal sealed class BindingModelResult
{
    public BindingModelResult(
        List<OptionModel> options,
        List<ParameterModel> parameters,
        List<DiagnosticInfo> diagnostics)
    {
        Options = options;
        Parameters = parameters;
        Diagnostics = diagnostics;
    }

    public List<OptionModel> Options { get; }

    public List<ParameterModel> Parameters { get; }

    public List<DiagnosticInfo> Diagnostics { get; }
}

internal static class BindingModelFactory
{
    private const string OptionAttributeName = "RunesHela.OptionAttribute";
    private const string ArgumentAttributeName = "RunesHela.ArgumentAttribute";
    private const string SecretAttributeName = "RunesHela.SecretAttribute";
    private const string RequiredAttributeName = "RunesHela.RequiredAttribute";
    private const string RangeAttributeName = "RunesHela.RangeAttribute";
    private const string RegexAttributeName = "RunesHela.RegexAttribute";
    private const string LengthAttributeName = "RunesHela.LengthAttribute";

    private static readonly string[] ReservedOptionNames =
    {
        "help",
        "version",
        "json",
        "plain",
        "no-color",
        "format",
        "config",
    };

    public static BindingModelResult Create(
        Compilation compilation,
        INamedTypeSymbol type,
        LocationInfo location,
        CancellationToken cancellationToken)
    {
        var diagnostics = new List<DiagnosticInfo>();
        var options = new List<OptionModel>();
        var parameters = new List<ParameterModel>();
        var optionNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var configKeys = new HashSet<string>(StringComparer.Ordinal);
        var shortNames = new Dictionary<char, string>();

        foreach (var member in type.GetMembers())
        {
            if (member is IFieldSymbol field)
            {
                if (HasAttribute(field, OptionAttributeName) || HasAttribute(field, ArgumentAttributeName))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        "RHL031",
                        LocationInfo.From(field.Locations.FirstOrDefault()),
                        field.Name,
                        "fields are not supported; use a settable property"));
                }

                continue;
            }

            if (member is not IPropertySymbol property)
            {
                continue;
            }

            var optionAttribute = FindAttribute(property, OptionAttributeName);
            var argumentAttribute = FindAttribute(property, ArgumentAttributeName);

            if (optionAttribute is null && argumentAttribute is null)
            {
                continue;
            }

            var propertyLocation = LocationInfo.From(property.Locations.FirstOrDefault());

            if (optionAttribute is not null && argumentAttribute is not null)
            {
                diagnostics.Add(new DiagnosticInfo("RHL036", propertyLocation, property.Name));
                continue;
            }

            if (type.IsStatic)
            {
                diagnostics.Add(new DiagnosticInfo(
                    "RHL031",
                    propertyLocation,
                    property.Name,
                    "static command types cannot bind instance properties"));
                continue;
            }

            if (property.IsStatic)
            {
                diagnostics.Add(new DiagnosticInfo(
                    "RHL031",
                    propertyLocation,
                    property.Name,
                    "static properties cannot be bound"));
                continue;
            }

            if (property.IsIndexer)
            {
                diagnostics.Add(new DiagnosticInfo(
                    "RHL031",
                    propertyLocation,
                    property.Name,
                    "indexers cannot be bound"));
                continue;
            }

            if (property.DeclaredAccessibility != Accessibility.Public &&
                property.DeclaredAccessibility != Accessibility.Internal)
            {
                diagnostics.Add(new DiagnosticInfo(
                    "RHL031",
                    propertyLocation,
                    property.Name,
                    "the property must be public or internal"));
                continue;
            }

            if (property.SetMethod is null ||
                (property.SetMethod.DeclaredAccessibility != Accessibility.Public &&
                 property.SetMethod.DeclaredAccessibility != Accessibility.Internal))
            {
                diagnostics.Add(new DiagnosticInfo(
                    "RHL031",
                    propertyLocation,
                    property.Name,
                    "the property must have a public or internal set or init accessor"));
                continue;
            }

            if (!TypeAnalyzer.TryAnalyze(property.Type, compilation, out var typeModel, out var reason))
            {
                diagnostics.Add(new DiagnosticInfo("RHL029", propertyLocation, reason ?? property.Type.ToDisplayString()));
                continue;
            }

            var description = ReadDescription(optionAttribute ?? argumentAttribute!, property, cancellationToken);
            var validations = ReadValidations(property, typeModel, propertyLocation, diagnostics);
            var defaultResult = ReadDefault(compilation, property, typeModel);

            if (defaultResult.HasInitializer && !defaultResult.HasValue && !typeModel.IsArray)
            {
                diagnostics.Add(new DiagnosticInfo("RHL032", propertyLocation, property.Name));
                continue;
            }

            var hasDefault = defaultResult.HasValue;
            var defaultLiteral = defaultResult.Literal;
            var defaultDisplay = defaultResult.Display;
            var isRequired = IsRequired(property, typeModel, optionAttribute, argumentAttribute, hasDefault);

            if (isRequired && hasDefault)
            {
                diagnostics.Add(new DiagnosticInfo("RHL003", propertyLocation, property.Name));
                continue;
            }

            if (argumentAttribute is not null)
            {
                var position = ReadPosition(argumentAttribute);

                if (position < 0)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        "RHL034",
                        propertyLocation,
                        position.ToString(CultureInfo.InvariantCulture),
                        property.Name));
                    continue;
                }

                var name = GetNamedString(argumentAttribute, "Name") ?? Names.ToUpperKebab(property.Name);

                if (typeModel.IsArray && !isRequired)
                {
                    isRequired = false;
                }

                if (hasDefault && typeModel.IsArray)
                {
                    hasDefault = false;
                    defaultLiteral = string.Empty;
                    defaultDisplay = string.Empty;
                }

                if (isRequired && typeModel.IsArray)
                {
                    isRequired = false;
                }

                parameters.Add(new ParameterModel(
                    property.Name,
                    name,
                    position,
                    typeModel,
                    isRequired,
                    isParams: false,
                    hasDefault,
                    defaultLiteral,
                    defaultDisplay,
                    description,
                    new EquatableArray<ValidationModel>(validations.ToArray())));
                continue;
            }

            var optionName = GetExplicitName(optionAttribute!) ?? Names.ToKebab(property.Name);

            if (!Names.IsValidCliName(optionName))
            {
                diagnostics.Add(new DiagnosticInfo("RHL030", propertyLocation, optionName));
                continue;
            }

            if (IsReservedOptionName(optionName))
            {
                diagnostics.Add(new DiagnosticInfo("RHL014", propertyLocation, optionName));
                continue;
            }

            if (optionNames.TryGetValue(optionName, out var existingOwner))
            {
                diagnostics.Add(new DiagnosticInfo("RHL002", propertyLocation, optionName, type.Name));
                continue;
            }

            optionNames.Add(optionName, property.Name);

            var aliases = ReadAliases(optionAttribute!);
            var aliasList = new List<string>();
            var aliasValid = true;

            foreach (var alias in aliases)
            {
                if (!Names.IsValidCliName(alias))
                {
                    diagnostics.Add(new DiagnosticInfo("RHL030", propertyLocation, alias));
                    aliasValid = false;
                    continue;
                }

                if (IsReservedOptionName(alias) || optionNames.ContainsKey(alias))
                {
                    diagnostics.Add(new DiagnosticInfo("RHL002", propertyLocation, alias, type.Name));
                    aliasValid = false;
                    continue;
                }

                optionNames.Add(alias, property.Name);
                aliasList.Add(alias);
            }

            var shortName = ReadShortName(optionAttribute!);

            if (shortName != '\0')
            {
                if (!IsAsciiLetterOrDigit(shortName))
                {
                    diagnostics.Add(new DiagnosticInfo("RHL033", propertyLocation, shortName.ToString()));
                    aliasValid = false;
                }
                else if (shortNames.ContainsKey(shortName))
                {
                    diagnostics.Add(new DiagnosticInfo("RHL002", propertyLocation, "-" + shortName, type.Name));
                    aliasValid = false;
                }
                else
                {
                    shortNames.Add(shortName, property.Name);
                }
            }

            if (!aliasValid)
            {
                continue;
            }

            var valueName = GetNamedString(optionAttribute!, "ValueName") ?? Names.ValueNameOf(typeModel);
            var envVar = GetNamedString(optionAttribute!, "Env");
            var configKey = GetNamedString(optionAttribute!, "Config");

            if (string.IsNullOrWhiteSpace(envVar))
            {
                envVar = null;
            }

            if (string.IsNullOrWhiteSpace(configKey))
            {
                configKey = null;
            }
            else
            {
                configKey = configKey!.Trim();

                if (!Names.IsValidConfigKey(configKey))
                {
                    diagnostics.Add(new DiagnosticInfo("RHL040", propertyLocation, configKey));
                    continue;
                }

                if (!configKeys.Add(configKey))
                {
                    diagnostics.Add(new DiagnosticInfo("RHL016", propertyLocation, configKey, type.Name));
                    continue;
                }
            }

            options.Add(new OptionModel(
                property.Name,
                optionName,
                shortName,
                new EquatableArray<string>(aliasList.ToArray()),
                valueName,
                typeModel,
                isRequired,
                hasDefault,
                defaultLiteral,
                defaultDisplay,
                envVar,
                configKey,
                GetNamedBoolean(optionAttribute!, "Hidden"),
                HasAttribute(property, SecretAttributeName),
                description,
                new EquatableArray<ValidationModel>(validations.ToArray())));
        }

        ValidateNegatedNames(options, type.Name, location, diagnostics);
        ValidateArgumentOrder(parameters, type, location, diagnostics);

        return new BindingModelResult(options, parameters, diagnostics);
    }

    private static void ValidateNegatedNames(
        List<OptionModel> options,
        string commandName,
        LocationInfo location,
        List<DiagnosticInfo> diagnostics)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var option in options)
        {
            names.Add(option.LongName);

            foreach (var alias in option.Aliases)
            {
                names.Add(alias);
            }
        }

        foreach (var option in options)
        {
            if (option.IsNegatable && names.Contains("no-" + option.LongName))
            {
                diagnostics.Add(new DiagnosticInfo("RHL002", location, "no-" + option.LongName, commandName));
            }
        }
    }

    private static void ValidateArgumentOrder(
        List<ParameterModel> parameters,
        INamedTypeSymbol type,
        LocationInfo location,
        List<DiagnosticInfo> diagnostics)
    {
        if (parameters.Count == 0)
        {
            return;
        }

        parameters.Sort(static (left, right) => left.Position.CompareTo(right.Position));

        var seenPositions = new HashSet<int>();

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            if (!seenPositions.Add(parameter.Position))
            {
                diagnostics.Add(new DiagnosticInfo(
                    "RHL010",
                    location,
                    parameter.Position.ToString(CultureInfo.InvariantCulture),
                    type.Name));
                continue;
            }

            if (parameter.IsRepeatable && i != parameters.Count - 1)
            {
                diagnostics.Add(new DiagnosticInfo("RHL011", location, parameter.PropertyName));
                continue;
            }

            if (i == parameters.Count - 1 && parameter.IsRepeatable)
            {
                parameters[i] = new ParameterModel(
                    parameter.PropertyName,
                    parameter.Name,
                    parameter.Position,
                    parameter.Type,
                    isRequired: false,
                    isParams: true,
                    hasDefault: false,
                    parameter.DefaultLiteral,
                    parameter.DefaultDisplay,
                    parameter.Description,
                    parameter.Validations);
                continue;
            }

            if (!parameter.IsRequired && i + 1 < parameters.Count && parameters[i + 1].IsRequired)
            {
                diagnostics.Add(new DiagnosticInfo(
                    "RHL009",
                    location,
                    parameter.PropertyName,
                    parameters[i + 1].PropertyName));
            }
        }
    }

    private static List<ValidationModel> ReadValidations(
        IPropertySymbol property,
        TypeModel type,
        LocationInfo location,
        List<DiagnosticInfo> diagnostics)
    {
        var result = new List<ValidationModel>();

        foreach (var attribute in property.GetAttributes())
        {
            var name = attribute.AttributeClass?.ToDisplayString();

            switch (name)
            {
                case RangeAttributeName:
                {
                    if (type.Kind is not (TypeKind.Integer or TypeKind.Floating or TypeKind.Decimal))
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            "RHL035",
                            location,
                            property.Name,
                            "Range requires a numeric type"));
                        continue;
                    }

                    if (attribute.ConstructorArguments.Length != 2)
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            "RHL035",
                            location,
                            property.Name,
                            "Range requires a minimum and a maximum"));
                        continue;
                    }

                    var min = Convert.ToDouble(attribute.ConstructorArguments[0].Value, CultureInfo.InvariantCulture);
                    var max = Convert.ToDouble(attribute.ConstructorArguments[1].Value, CultureInfo.InvariantCulture);

                    result.Add(new ValidationModel(
                        ValidationKind.Range,
                        min.ToString("R", CultureInfo.InvariantCulture),
                        max.ToString("R", CultureInfo.InvariantCulture),
                        string.Empty));
                    break;
                }

                case RegexAttributeName:
                {
                    if (type.Kind != TypeKind.String)
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            "RHL035",
                            location,
                            property.Name,
                            "Regex requires a string type"));
                        continue;
                    }

                    var pattern = attribute.ConstructorArguments.Length == 1
                        ? attribute.ConstructorArguments[0].Value as string
                        : null;

                    if (string.IsNullOrEmpty(pattern))
                    {
                        diagnostics.Add(new DiagnosticInfo("RHL035", location, property.Name, "Regex pattern is empty"));
                        continue;
                    }

                    result.Add(new ValidationModel(ValidationKind.Regex, string.Empty, string.Empty, pattern!));
                    break;
                }

                case LengthAttributeName:
                {
                    if (type.Kind != TypeKind.String)
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            "RHL035",
                            location,
                            property.Name,
                            "Length requires a string type"));
                        continue;
                    }

                    var min = 0;
                    var max = int.MaxValue;

                    if (attribute.ConstructorArguments.Length == 1)
                    {
                        max = Convert.ToInt32(attribute.ConstructorArguments[0].Value, CultureInfo.InvariantCulture);
                    }
                    else if (attribute.ConstructorArguments.Length == 2)
                    {
                        min = Convert.ToInt32(attribute.ConstructorArguments[0].Value, CultureInfo.InvariantCulture);
                        max = Convert.ToInt32(attribute.ConstructorArguments[1].Value, CultureInfo.InvariantCulture);
                    }

                    result.Add(new ValidationModel(
                        ValidationKind.Length,
                        min.ToString(CultureInfo.InvariantCulture),
                        max.ToString(CultureInfo.InvariantCulture),
                        string.Empty));
                    break;
                }
            }
        }

        return result;
    }

    private static bool IsRequired(
        IPropertySymbol property,
        TypeModel type,
        AttributeData? optionAttribute,
        AttributeData? argumentAttribute,
        bool hasDefault)
    {
        if (HasAttribute(property, RequiredAttributeName))
        {
            return true;
        }

        if (optionAttribute is not null && GetNamedBoolean(optionAttribute, "Required"))
        {
            return true;
        }

        if (argumentAttribute is not null && GetNamedBoolean(argumentAttribute, "Required"))
        {
            return true;
        }

        if (property.IsRequired)
        {
            return true;
        }

        if (hasDefault)
        {
            return false;
        }

        return !type.IsArray && type.IsReferenceType && !type.IsNullable;
    }

    private static DefaultValue ReadDefault(Compilation compilation, IPropertySymbol property, TypeModel type)
    {
        if (type.IsArray)
        {
            return default;
        }

        ExpressionSyntax? initializer = null;
        SemanticModel? semanticModel = null;

        foreach (var reference in property.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is PropertyDeclarationSyntax declaration &&
                declaration.Initializer is not null)
            {
                initializer = declaration.Initializer.Value;
                semanticModel = compilation.GetSemanticModel(initializer.SyntaxTree);
                break;
            }
        }

        if (initializer is null || semanticModel is null)
        {
            return default;
        }

        var constant = semanticModel.GetConstantValue(initializer);

        if (!constant.HasValue)
        {
            return new DefaultValue(true, false, string.Empty, string.Empty);
        }

        if (!TryFormatConstant(initializer, semanticModel, type, constant.Value, out var literal, out var display))
        {
            return new DefaultValue(true, false, string.Empty, string.Empty);
        }

        return new DefaultValue(true, true, literal, display);
    }

    private static bool TryFormatConstant(
        ExpressionSyntax initializer,
        SemanticModel semanticModel,
        TypeModel type,
        object? value,
        out string literal,
        out string display)
    {
        literal = string.Empty;
        display = string.Empty;

        if (value is null)
        {
            literal = "null";
            return true;
        }

        switch (type.Kind)
        {
            case TypeKind.String:
                literal = CodeLiteral.String((string)value);
                display = Shorten((string)value);
                return true;

            case TypeKind.Char:
                literal = CodeLiteral.Char((char)value);
                display = value.ToString()!;
                return true;

            case TypeKind.Boolean:
                literal = (bool)value ? "true" : "false";
                display = literal;
                return true;

            case TypeKind.Enum:
            {
                if (semanticModel.GetSymbolInfo(initializer).Symbol is IFieldSymbol field &&
                    field.HasConstantValue &&
                    field.ContainingType is not null)
                {
                    literal = field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        + "." + field.Name;
                    display = field.Name;
                    return true;
                }

                literal = "(" + type.DisplayName + ")(" + FormatNumeric(value, isEnumUnderlying: true) + ")";
                display = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                return true;
            }

            default:
                if (value is string text)
                {
                    literal = CodeLiteral.String(text);
                    display = Shorten(text);
                    return true;
                }

                literal = FormatNumeric(value, isEnumUnderlying: false);
                display = literal;
                return true;
        }
    }

    private static string FormatNumeric(object value, bool isEnumUnderlying)
    {
        var text = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);

        if (isEnumUnderlying)
        {
            return text;
        }

        return value switch
        {
            float => text + "F",
            double => text,
            decimal => text + "M",
            long => text + "L",
            ulong => text + "UL",
            uint => text + "U",
            _ => text,
        };
    }

    private static string Shorten(string value)
    {
        var sanitized = value.Replace("\r", " ").Replace("\n", " ").Trim();

        return sanitized.Length <= 40 ? sanitized : sanitized.Substring(0, 37) + "...";
    }

    private static string ReadDescription(
        AttributeData attribute,
        IPropertySymbol property,
        CancellationToken cancellationToken)
    {
        var description = GetNamedString(attribute, "Description");

        if (!string.IsNullOrWhiteSpace(description))
        {
            return description!.Trim();
        }

        var summary = DocumentationReader.GetSummary(property, cancellationToken);
        return summary ?? string.Empty;
    }

    private static int ReadPosition(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length != 1)
        {
            return -1;
        }

        var value = attribute.ConstructorArguments[0].Value;

        return value is int position ? position : -1;
    }

    private static char ReadShortName(AttributeData attribute)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == "Short" && argument.Value.Value is char shortName)
            {
                return shortName;
            }
        }

        return '\0';
    }

    private static string? GetExplicitName(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
        {
            return null;
        }

        var value = attribute.ConstructorArguments[0].Value as string;
        return string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
    }

    private static List<string> ReadAliases(AttributeData attribute)
    {
        var result = new List<string>();

        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key != "Aliases" ||
                argument.Value.Kind != TypedConstantKind.Array ||
                argument.Value.IsNull)
            {
                continue;
            }

            foreach (var value in argument.Value.Values)
            {
                if (value.Value is string alias && alias.Length > 0)
                {
                    result.Add(alias);
                }
            }
        }

        return result;
    }

    private static string? GetNamedString(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name)
            {
                return argument.Value.Value as string;
            }
        }

        return null;
    }

    private static bool GetNamedBoolean(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (argument.Key == name && argument.Value.Value is bool value)
            {
                return value;
            }
        }

        return false;
    }

    private static AttributeData? FindAttribute(ISymbol symbol, string attributeName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == attributeName)
            {
                return attribute;
            }
        }

        return null;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        return FindAttribute(symbol, attributeName) is not null;
    }

    private static bool IsReservedOptionName(string name)
    {
        foreach (var reserved in ReservedOptionNames)
        {
            if (string.Equals(name, reserved, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAsciiLetterOrDigit(char value)
    {
        return (value >= 'a' && value <= 'z') ||
               (value >= 'A' && value <= 'Z') ||
               (value >= '0' && value <= '9');
    }

    private readonly struct DefaultValue
    {
        public DefaultValue(bool hasInitializer, bool hasValue, string literal, string display)
        {
            HasInitializer = hasInitializer;
            HasValue = hasValue;
            Literal = literal;
            Display = display;
        }

        public bool HasInitializer { get; }

        public bool HasValue { get; }

        public string Literal { get; }

        public string Display { get; }
    }
}
