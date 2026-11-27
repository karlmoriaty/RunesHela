using System;

namespace RunesHela.Generator;

internal enum TypeKind
{
    Unsupported,
    String,
    Char,
    Boolean,
    Integer,
    Floating,
    Decimal,
    Enum,
    Guid,
    DateTime,
    DateTimeOffset,
    TimeSpan,
    Uri,
    FileInfo,
    DirectoryInfo,
    Array,
    Parsable,
}

internal sealed class TypeModel : IEquatable<TypeModel>
{
    public TypeModel(
        TypeKind kind,
        string displayName,
        bool isNullable,
        bool isReferenceType,
        TypeModel? elementType)
    {
        Kind = kind;
        DisplayName = displayName;
        IsNullable = isNullable;
        IsReferenceType = isReferenceType;
        ElementType = elementType;
    }

    public TypeKind Kind { get; }

    public string DisplayName { get; }

    public bool IsNullable { get; }

    public bool IsReferenceType { get; }

    public TypeModel? ElementType { get; }

    public bool Equals(TypeModel? other)
    {
        return other is not null &&
               Kind == other.Kind &&
               DisplayName == other.DisplayName &&
               IsNullable == other.IsNullable &&
               IsReferenceType == other.IsReferenceType &&
               Equals(ElementType, other.ElementType);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as TypeModel);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (int)Kind;
            hash = (hash * 397) ^ DisplayName.GetHashCode();
            hash = (hash * 397) ^ (IsNullable ? 1 : 0);
            hash = (hash * 397) ^ (IsReferenceType ? 1 : 0);
            hash = (hash * 397) ^ (ElementType?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
