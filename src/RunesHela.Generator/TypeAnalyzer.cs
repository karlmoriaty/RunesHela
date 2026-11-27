using Microsoft.CodeAnalysis;

namespace RunesHela.Generator;

internal static class TypeAnalyzer
{
    public static bool TryAnalyze(
        ITypeSymbol type,
        Compilation compilation,
        out TypeModel model,
        out string? reason)
    {
        reason = null;

        var isNullable = type.NullableAnnotation == NullableAnnotation.Annotated;
        var current = type;

        if (current is INamedTypeSymbol named &&
            named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            named.TypeArguments.Length == 1)
        {
            isNullable = true;
            current = named.TypeArguments[0];
        }

        if (current is IArrayTypeSymbol array)
        {
            if (!TryAnalyze(array.ElementType, compilation, out var element, out reason))
            {
                model = null!;
                return false;
            }

            model = new TypeModel(
                TypeKind.Array,
                current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                isNullable,
                isReferenceType: true,
                element);
            return true;
        }

        var kind = GetKind(current, compilation);

        if (kind == TypeKind.Unsupported)
        {
            model = null!;
            reason = current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return false;
        }

        model = new TypeModel(
            kind,
            current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            isNullable,
            current.IsReferenceType,
            elementType: null);
        return true;
    }

    private static TypeKind GetKind(ITypeSymbol type, Compilation compilation)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_String:
                return TypeKind.String;
            case SpecialType.System_Char:
                return TypeKind.Char;
            case SpecialType.System_Boolean:
                return TypeKind.Boolean;
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
                return TypeKind.Integer;
            case SpecialType.System_Single:
            case SpecialType.System_Double:
                return TypeKind.Floating;
            case SpecialType.System_Decimal:
                return TypeKind.Decimal;
            case SpecialType.System_DateTime:
                return TypeKind.DateTime;
        }

        if (type.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum)
        {
            return TypeKind.Enum;
        }

        var metadataName = type.ToDisplayString();

        switch (metadataName)
        {
            case "System.Guid":
                return TypeKind.Guid;
            case "System.DateTimeOffset":
                return TypeKind.DateTimeOffset;
            case "System.TimeSpan":
                return TypeKind.TimeSpan;
            case "System.Uri":
                return TypeKind.Uri;
            case "System.IO.FileInfo":
                return TypeKind.FileInfo;
            case "System.IO.DirectoryInfo":
                return TypeKind.DirectoryInfo;
        }

        var parsable = compilation.GetTypeByMetadataName("System.IParsable`1");
        var spanParsable = compilation.GetTypeByMetadataName("System.ISpanParsable`1");

        foreach (var @interface in type.AllInterfaces)
        {
            if (parsable is not null &&
                SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition, parsable))
            {
                return TypeKind.Parsable;
            }

            if (spanParsable is not null &&
                SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition, spanParsable))
            {
                return TypeKind.Parsable;
            }
        }

        return TypeKind.Unsupported;
    }
}
