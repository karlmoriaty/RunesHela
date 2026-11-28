using System.Reflection;
using System.Text;
using RunesHela;
using Xunit;

namespace RunesHela.Tests;

public sealed class PublicApiTests
{
    [Fact]
    public void PublicApiMatchesTheApprovedBaseline()
    {
        var actual = BuildApiListing();
        var path = GetApprovedFilePath();

        if (Environment.GetEnvironmentVariable("UPDATE_PUBLIC_API") == "1")
        {
            File.WriteAllText(path, actual);
            return;
        }

        Assert.True(File.Exists(path), "Missing public API baseline: " + path);
        Assert.Equal(File.ReadAllText(path).Replace("\r\n", "\n"), actual);
    }

    private static string BuildApiListing()
    {
        var assembly = typeof(RunesHelaApp).Assembly;
        var lines = new List<string>();

        foreach (var type in assembly.GetExportedTypes().OrderBy(static type => type.FullName, StringComparer.Ordinal))
        {
            if (type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false))
            {
                continue;
            }

            var interfaces = type.GetInterfaces()
                .Select(static value => value.FullName)
                .OrderBy(static value => value, StringComparer.Ordinal);

            lines.Add(
                "type " + Kind(type) + " " + type.FullName
                + " : " + (type.BaseType?.FullName ?? "-")
                + " [" + string.Join(", ", interfaces) + "]");

            const BindingFlags flags =
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly;

            var members = new List<string>();

            foreach (var constructor in type.GetConstructors(flags))
            {
                members.Add("ctor " + constructor);
            }

            foreach (var method in type.GetMethods(flags))
            {
                if (method.IsSpecialName)
                {
                    continue;
                }

                members.Add("method " + method);
            }

            foreach (var property in type.GetProperties(flags))
            {
                var accessors = new StringBuilder();

                if (property.GetMethod is not null)
                {
                    accessors.Append("get;");
                }

                if (property.SetMethod is not null)
                {
                    accessors.Append("set;");
                }

                members.Add("property " + property.PropertyType + " " + property.Name + " {" + accessors + "}");
            }

            foreach (var field in type.GetFields(flags))
            {
                members.Add("field " + field.FieldType + " " + field.Name);
            }

            foreach (var eventInfo in type.GetEvents(flags))
            {
                members.Add("event " + eventInfo.EventHandlerType + " " + eventInfo.Name);
            }

            members.Sort(StringComparer.Ordinal);

            foreach (var member in members)
            {
                lines.Add("  " + member);
            }
        }

        return string.Join("\n", lines) + "\n";
    }

    private static string Kind(Type type)
    {
        if (type.IsEnum)
        {
            return "enum";
        }

        if (type.IsValueType)
        {
            return "struct";
        }

        if (type.IsInterface)
        {
            return "interface";
        }

        if (typeof(Delegate).IsAssignableFrom(type))
        {
            return "delegate";
        }

        return "class";
    }

    private static string GetApprovedFilePath()
    {
        var directory = AppContext.BaseDirectory;

        while (directory is not null &&
               !File.Exists(Path.Combine(directory, "RunesHela.Tests.csproj")))
        {
            directory = Directory.GetParent(directory)?.FullName;
        }

        if (directory is null)
        {
            throw new InvalidOperationException("Could not locate the test project directory.");
        }

        return Path.Combine(directory, "PublicApiTests.approved.txt");
    }
}
