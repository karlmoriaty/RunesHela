using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RunesHela.Generator;
using Xunit;

namespace RunesHela.Generator.Tests;

public sealed class TypeAnalyzerTests
{
    [Theory]
    [InlineData("string", "String")]
    [InlineData("char", "Char")]
    [InlineData("bool", "Boolean")]
    [InlineData("int", "Integer")]
    [InlineData("double", "Floating")]
    [InlineData("decimal", "Decimal")]
    [InlineData("System.Guid", "Guid")]
    [InlineData("System.DateTime", "DateTime")]
    [InlineData("System.DateTimeOffset", "DateTimeOffset")]
    [InlineData("System.TimeSpan", "TimeSpan")]
    [InlineData("System.Uri", "Uri")]
    [InlineData("System.IO.FileInfo", "FileInfo")]
    [InlineData("System.IO.DirectoryInfo", "DirectoryInfo")]
    public void MapsSupportedTypes(string typeName, string expectedKind)
    {
        var compilation = CreateCompilation($$"""
            public sealed class Holder
            {
                public {{typeName}} Value { get; set; } = default!;
            }
            """);

        var type = GetPropertyType(compilation, "Holder", "Value");

        Assert.True(TypeAnalyzer.TryAnalyze(type, compilation, out var model, out var reason), reason);
        Assert.Equal(Enum.Parse<TypeKind>(expectedKind), model.Kind);
    }

    [Fact]
    public void MapsNullableValueType()
    {
        var compilation = CreateCompilation("""
            public sealed class Holder
            {
                public int? Value { get; set; }
            }
            """);

        var type = GetPropertyType(compilation, "Holder", "Value");

        Assert.True(TypeAnalyzer.TryAnalyze(type, compilation, out var model, out var reason), reason);
        Assert.Equal(TypeKind.Integer, model.Kind);
        Assert.True(model.IsNullable);
    }

    [Fact]
    public void MapsArrayElementType()
    {
        var compilation = CreateCompilation("""
            public sealed class Holder
            {
                public int[] Value { get; set; } = default!;
            }
            """);

        var type = GetPropertyType(compilation, "Holder", "Value");

        Assert.True(TypeAnalyzer.TryAnalyze(type, compilation, out var model, out var reason), reason);
        Assert.Equal(TypeKind.Array, model.Kind);
        Assert.NotNull(model.ElementType);
        Assert.Equal(TypeKind.Integer, model.ElementType!.Kind);
    }

    [Fact]
    public void MapsEnums()
    {
        var compilation = CreateCompilation("""
            public enum Color
            {
                Red,
                Green,
            }

            public sealed class Holder
            {
                public Color Value { get; set; }
            }
            """);

        var type = GetPropertyType(compilation, "Holder", "Value");

        Assert.True(TypeAnalyzer.TryAnalyze(type, compilation, out var model, out var reason), reason);
        Assert.Equal(TypeKind.Enum, model.Kind);
    }

    [Fact]
    public void MapsParsableTypes()
    {
        var compilation = CreateCompilation("""
            public readonly struct Code : System.IParsable<Code>
            {
                public static Code Parse(string value, System.IFormatProvider? provider) => default;

                public static bool TryParse(string? value, System.IFormatProvider? provider, out Code result)
                {
                    result = default;
                    return true;
                }
            }

            public sealed class Holder
            {
                public Code Value { get; set; }
            }
            """);

        var type = GetPropertyType(compilation, "Holder", "Value");

        Assert.True(TypeAnalyzer.TryAnalyze(type, compilation, out var model, out var reason), reason);
        Assert.Equal(TypeKind.Parsable, model.Kind);
    }

    [Fact]
    public void RejectsUnsupportedTypes()
    {
        var compilation = CreateCompilation("""
            public sealed class Holder
            {
                public object Value { get; set; } = default!;
            }
            """);

        var type = GetPropertyType(compilation, "Holder", "Value");

        Assert.False(TypeAnalyzer.TryAnalyze(type, compilation, out _, out var reason));
        Assert.NotNull(reason);
    }

    private static ITypeSymbol GetPropertyType(Compilation compilation, string typeName, string propertyName)
    {
        var type = compilation.GetTypeByMetadataName(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' was not found.");

        var property = type.GetMembers(propertyName)
            .OfType<IPropertySymbol>()
            .Single();

        return property.Type;
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create(
            "TypeAnalyzerTests",
            new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static ImmutableArray<MetadataReference> GetReferences()
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is not available.");

        var references = trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .Cast<MetadataReference>()
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(RunesHela.CommandAttribute).Assembly.Location));

        return references.ToImmutableArray();
    }
}
