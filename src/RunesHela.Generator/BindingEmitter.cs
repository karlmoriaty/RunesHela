using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RunesHela.Generator;

internal sealed class BindingEmitter
{
    private readonly StringBuilder _builder;
    private readonly CommandNode _node;
    private readonly string _appName;
    private readonly string? _configFileName;
    private readonly int _nodeIndex;
    private readonly List<ParserMethod> _parsers = new List<ParserMethod>();
    private int _variableIndex;

    private BindingEmitter(
        StringBuilder builder,
        CommandNode node,
        string appName,
        string? configFileName,
        int nodeIndex)
    {
        _builder = builder;
        _node = node;
        _appName = appName;
        _configFileName = configFileName;
        _nodeIndex = nodeIndex;
    }

    public static void Emit(
        StringBuilder builder,
        CommandNode node,
        string appName,
        string? configFileName,
        int nodeIndex)
    {
        new BindingEmitter(builder, node, appName, configFileName, nodeIndex).EmitNode();
    }

    private void EmitNode()
    {
        var model = _node.Model;

        if (model.HandlerName is null)
        {
            return;
        }

        if (model.IsStatic)
        {
            _builder.Append("    private static int BindNode").Append(_nodeIndex)
                .AppendLine("(ReadOnlySpan<string> args, global::System.IServiceProvider? services, global::System.Threading.CancellationToken cancellationToken, bool json, string? configPath)");
        }
        else
        {
            _builder.Append("    private static int BindNode").Append(_nodeIndex)
                .Append("(ReadOnlySpan<string> args, global::System.IServiceProvider? services, global::System.Threading.CancellationToken cancellationToken, bool json, string? configPath, out ")
                .Append(model.TypeFullName).AppendLine(" command)");
        }

        _builder.AppendLine("    {");

        if (!model.IsStatic)
        {
            _builder.AppendLine("        command = null!;");
        }

        _builder.Append("        const string commandPath = ").Append(CodeLiteral.String(_node.Path)).AppendLine(";");

        if (model.Options.Count > 0)
        {
            _builder.AppendLine("        var localConfigPath = configPath;");
        }

        EmitLocalDeclarations(model);

        _builder.AppendLine("        var reader = new global::RunesHela.Parsing.CommandLineReader(args);");
        _builder.AppendLine();
        _builder.AppendLine("        while (reader.TryRead(out var token))");
        _builder.AppendLine("        {");
        _builder.AppendLine("            if (token.Kind == global::RunesHela.Parsing.CommandLineTokenKind.Option)");
        _builder.AppendLine("            {");
        _builder.AppendLine("                var name = token.Name;");
        _builder.AppendLine("                if (name.Equals(\"config\", global::System.StringComparison.OrdinalIgnoreCase))");
        _builder.AppendLine("                {");
        _builder.AppendLine("                    if (!reader.TryReadValue(out var configOption))");
        _builder.AppendLine("                        return global::RunesHela.RunesHelaErrors.MissingValue(commandPath, \"config\", json);");

        if (model.Options.Count > 0)
        {
            _builder.AppendLine("                    localConfigPath = global::RunesHela.Parsing.SpanText.ToString(configOption);");
        }

        _builder.AppendLine("                    continue;");
        _builder.AppendLine("                }");

        if (model.Options.Count == 0)
        {
            EmitGlobalOptionChecks("                ");
            _builder.AppendLine("                return global::RunesHela.RunesHelaErrors.UnknownOption(commandPath, name.ToString(), json);");
        }
        else
        {
            EmitOptionMatching(model);
        }

        _builder.AppendLine("            }");
        _builder.AppendLine("            else if (token.Kind == global::RunesHela.Parsing.CommandLineTokenKind.Positional)");
        _builder.AppendLine("            {");

        if (model.Parameters.Count == 0)
        {
            _builder.AppendLine("                return global::RunesHela.RunesHelaErrors.UnexpectedArgument(commandPath, token.Value.ToString(), json);");
        }
        else
        {
            EmitArgumentMatching(model);
        }

        _builder.AppendLine("            }");
        _builder.AppendLine("        }");
        _builder.AppendLine();

        EmitEnvironmentFallbacks(model);
        EmitConfigFallbacks(model);
        EmitRequiredChecks(model);
        EmitValidations(model);
        EmitConstruction(model);

        _builder.AppendLine("    }");
        _builder.AppendLine();

        EmitParserMethods();
    }

    private void EmitLocalDeclarations(CommandModel model)
    {
        for (var i = 0; i < model.Options.Count; i++)
        {
            var option = model.Options[i];
            var local = "o" + i;

            if (option.IsRepeatable)
            {
                _builder.Append("        global::System.Collections.Generic.List<")
                    .Append(NonNullableDisplay(option.Type.ElementType!))
                    .Append(">? ").Append(local).AppendLine(" = null;");
            }
            else if (option.HasDefault)
            {
                _builder.Append("        ").Append(LocalType(option.Type)).Append(' ').Append(local)
                    .Append(" = ").Append(option.DefaultLiteral).AppendLine(";");
            }
            else if (option.Type.IsNullable)
            {
                _builder.Append("        ").Append(LocalType(option.Type)).Append(' ').Append(local).AppendLine(" = null;");
            }
            else if (option.Type.IsReferenceType)
            {
                _builder.Append("        ").Append(LocalType(option.Type)).Append("? ").Append(local).AppendLine(" = null;");
            }
            else
            {
                _builder.Append("        ").Append(option.Type.DisplayName).Append(' ').Append(local).AppendLine(" = default;");
            }

            if (NeedsSetFlag(option))
            {
                _builder.Append("        var ").Append(local).AppendLine("Set = false;");
            }
        }

        for (var i = 0; i < model.Parameters.Count; i++)
        {
            var parameter = model.Parameters[i];
            var local = "a" + i;

            if (parameter.IsParams || parameter.IsRepeatable)
            {
                _builder.Append("        global::System.Collections.Generic.List<")
                    .Append(NonNullableDisplay(parameter.Type.ElementType!))
                    .Append(">? ").Append(local).AppendLine(" = null;");
            }
            else if (parameter.HasDefault)
            {
                _builder.Append("        ").Append(LocalType(parameter.Type)).Append(' ').Append(local)
                    .Append(" = ").Append(parameter.DefaultLiteral).AppendLine(";");
            }
            else if (parameter.Type.IsNullable)
            {
                _builder.Append("        ").Append(LocalType(parameter.Type)).Append(' ').Append(local).AppendLine(" = null;");
            }
            else if (parameter.Type.IsReferenceType)
            {
                _builder.Append("        ").Append(LocalType(parameter.Type)).Append("? ").Append(local).AppendLine(" = null;");
            }
            else
            {
                _builder.Append("        ").Append(parameter.Type.DisplayName).Append(' ').Append(local).AppendLine(" = default;");
            }

            if (parameter.IsRequired && !parameter.IsParams)
            {
                _builder.Append("        var ").Append(local).AppendLine("Set = false;");
            }
        }

        if (model.Parameters.Count > 0)
        {
            _builder.AppendLine("        var positional = 0;");
        }
    }

    private void EmitOptionMatching(CommandModel model)
    {
        var hasNegatable = false;

        foreach (var option in model.Options)
        {
            if (option.IsNegatable)
            {
                hasNegatable = true;
                break;
            }
        }

        _builder.AppendLine("                var optionIndex = -1;");

        if (hasNegatable)
        {
            _builder.AppendLine("                var negated = false;");
        }

        _builder.AppendLine("                if (token.PrefixLength == 2)");
        _builder.AppendLine("                {");

        var first = true;

        for (var i = 0; i < model.Options.Count; i++)
        {
            var option = model.Options[i];
            var prefix = "                    ";

            _builder.Append(prefix).Append(first ? "if (" : "else if (")
                .Append(MatchExpression("name", option.LongName, option.Aliases))
                .Append(") optionIndex = ").Append(i).AppendLine(";");
            first = false;

            if (option.IsNegatable)
            {
                var negatedName = "no-" + option.LongName;
                _builder.Append(prefix).Append("else if (name.Length == ").Append(negatedName.Length)
                    .Append(" && name.Equals(").Append(CodeLiteral.String(negatedName))
                    .Append(", global::System.StringComparison.OrdinalIgnoreCase)) { optionIndex = ").Append(i).AppendLine("; negated = true; }");
            }
        }

        _builder.AppendLine("                }");

        if (HasShortNames(model))
        {
            _builder.AppendLine("                else if (name.Length == 1)");
            _builder.AppendLine("                {");
            _builder.AppendLine("                    switch (name[0])");
            _builder.AppendLine("                    {");

            for (var i = 0; i < model.Options.Count; i++)
            {
                var option = model.Options[i];

                if (option.ShortName != '\0')
                {
                    _builder.Append("                        case ").Append(CodeLiteral.Char(option.ShortName))
                        .Append(": optionIndex = ").Append(i).AppendLine("; break;");
                }
            }

            _builder.AppendLine("                    }");
            _builder.AppendLine("                }");
        }
        _builder.AppendLine();
        _builder.AppendLine("                switch (optionIndex)");
        _builder.AppendLine("                {");

        for (var i = 0; i < model.Options.Count; i++)
        {
            EmitOptionCase(model.Options[i], i, "                    ");
        }

        _builder.AppendLine("                    case -1:");
        EmitGlobalOptionChecks("                        ");
        _builder.Append("                        return global::RunesHela.RunesHelaErrors.UnknownOption(commandPath, name.ToString(), json, OptionCandidates")
            .Append(_nodeIndex).AppendLine(");");
        _builder.AppendLine("                }");
    }

    private void EmitGlobalOptionChecks(string indent)
    {
        _builder.Append(indent).AppendLine("if (name.Equals(\"help\", global::System.StringComparison.OrdinalIgnoreCase)) continue;");
        _builder.Append(indent).AppendLine("if (name.Equals(\"json\", global::System.StringComparison.OrdinalIgnoreCase) || name.Equals(\"plain\", global::System.StringComparison.OrdinalIgnoreCase) || name.Equals(\"no-color\", global::System.StringComparison.OrdinalIgnoreCase) || name.Equals(\"quiet\", global::System.StringComparison.OrdinalIgnoreCase) || name.Equals(\"verbose\", global::System.StringComparison.OrdinalIgnoreCase) || name.Equals(\"yes\", global::System.StringComparison.OrdinalIgnoreCase) || name.Equals(\"no-input\", global::System.StringComparison.OrdinalIgnoreCase)) continue;");
        _builder.Append(indent).AppendLine("if (name.Equals(\"format\", global::System.StringComparison.OrdinalIgnoreCase))");
        _builder.Append(indent).AppendLine("{");
        _builder.Append(indent).AppendLine("    if (!reader.TryReadValue(out var formatValue))");
        _builder.Append(indent).AppendLine("        return global::RunesHela.RunesHelaErrors.MissingValue(commandPath, \"format\", json);");
        _builder.Append(indent).AppendLine("    if (!global::RunesHela.RunesHelaOutputOptions.IsSupportedFormat(formatValue.Span))");
        _builder.Append(indent).AppendLine("        return global::RunesHela.RunesHelaErrors.UnsupportedFormat(commandPath, global::RunesHela.Parsing.SpanText.ToString(formatValue), json);");
        _builder.Append(indent).AppendLine("    continue;");
        _builder.Append(indent).AppendLine("}");
        _builder.Append(indent).AppendLine("if (token.PrefixLength == 1 && name.Length == 1 && name[0] == 'y') continue;");
    }

    private void EmitOptionCase(OptionModel option, int index, string indent)
    {
        var local = "o" + index;
        var setFlag = local + "Set";
        var needsSet = NeedsSetFlag(option);

        _builder.Append(indent).Append("case ").Append(index).AppendLine(":");
        _builder.Append(indent).AppendLine("{");

        if (option.IsNegatable)
        {
            _builder.Append(indent).Append("    if (negated) { ").Append(local).Append(" = false;");

            if (needsSet)
            {
                _builder.Append(' ').Append(setFlag).Append(" = true;");
            }

            _builder.AppendLine(" continue; }");
        }

        if (option.IsFlag)
        {
            _builder.Append(indent).AppendLine("    if (token.HasValue)");
            _builder.Append(indent).AppendLine("    {");
            _builder.Append(indent).AppendLine("        if (!global::System.Boolean.TryParse(token.Value, out var parsed))");
            _builder.Append(indent).Append("        {");
            _builder.AppendLine();
            _builder.Append(indent).Append("            return global::RunesHela.RunesHelaErrors.InvalidValue(commandPath, ")
                .Append(CodeLiteral.String(option.LongName)).Append(", ")
                .Append(SpanToString("token.Value")).Append(", \"true or false\", json);");
            _builder.AppendLine();
            _builder.Append(indent).AppendLine("        }");
            _builder.Append(indent).Append("        ").Append(local).AppendLine(" = parsed;");
            _builder.Append(indent).AppendLine("    }");
            _builder.Append(indent).AppendLine("    else");
            _builder.Append(indent).AppendLine("    {");
            _builder.Append(indent).Append("        ").Append(local).AppendLine(" = true;");
            _builder.Append(indent).AppendLine("    }");

            if (needsSet)
            {
                _builder.Append(indent).Append("    ").Append(setFlag).AppendLine(" = true;");
            }

            _builder.Append(indent).AppendLine("    continue;");
            _builder.Append(indent).AppendLine("}");
            return;
        }

        _builder.Append(indent).AppendLine("    if (!reader.TryReadValue(out var raw))");
        _builder.Append(indent).Append("        return global::RunesHela.RunesHelaErrors.MissingValue(commandPath, ")
            .Append(CodeLiteral.String(option.LongName)).AppendLine(", json);");
        _builder.AppendLine();

        if (option.IsRepeatable)
        {
            EmitArrayValue(option.Type.ElementType!, local, indent + "    ", option.LongName, option.IsSecret);
        }
        else if (option.Type.Kind == TypeKind.String)
        {
            _builder.Append(indent).Append("    ").Append(local).Append(" = global::RunesHela.Parsing.SpanText.ToString(raw);").AppendLine();

            if (needsSet)
            {
                _builder.Append(indent).Append("    ").Append(setFlag).AppendLine(" = true;");
            }
        }
        else
        {
            var parser = EnsureParser(option.Type);
            _builder.Append(indent).Append("    if (!").Append(parser).Append("(raw.Span, out var parsed))").AppendLine();
            _builder.Append(indent).Append("        return global::RunesHela.RunesHelaErrors.InvalidValue(commandPath, ")
                .Append(CodeLiteral.String(option.LongName)).Append(", ")
                .Append(option.IsSecret ? "null" : "global::RunesHela.Parsing.SpanText.ToString(raw)")
                .Append(", ").Append(CodeLiteral.String(ExpectedValue(option.Type))).AppendLine(", json);");
            _builder.Append(indent).Append("    ").Append(local).AppendLine(" = parsed;");

            if (needsSet)
            {
                _builder.Append(indent).Append("    ").Append(setFlag).AppendLine(" = true;");
            }
        }

        _builder.AppendLine();
        _builder.Append(indent).AppendLine("    continue;");
        _builder.Append(indent).AppendLine("}");
    }

    private void EmitArrayValue(
        TypeModel elementType,
        string local,
        string indent,
        string displayName,
        bool isSecret)
    {
        _builder.Append(indent).AppendLine("var text = global::RunesHela.Parsing.SpanText.ToString(raw);");
        _builder.Append(indent).AppendLine("var start = 0;");
        _builder.Append(indent).AppendLine("while (true)");
        _builder.Append(indent).AppendLine("{");
        _builder.Append(indent).AppendLine("    var comma = text.IndexOf(',', start);");
        _builder.Append(indent).AppendLine("    var part = comma < 0 ? text.AsSpan(start) : text.AsSpan(start, comma - start);");

        if (elementType.Kind == TypeKind.String)
        {
            _builder.Append(indent).Append("    (").Append(local).Append(" ??= new global::System.Collections.Generic.List<")
                .Append(NonNullableDisplay(elementType)).AppendLine(">()).Add(part.ToString());");
        }
        else
        {
            var parser = EnsureParser(elementType);
            _builder.Append(indent).Append("    if (!").Append(parser).AppendLine("(part, out var parsed))");
            _builder.Append(indent).Append("        return global::RunesHela.RunesHelaErrors.InvalidValue(commandPath, ")
                .Append(CodeLiteral.String(displayName)).Append(", ")
                .Append(isSecret ? "null" : "global::RunesHela.Parsing.SpanText.ToString(raw)")
                .Append(", ").Append(CodeLiteral.String(ExpectedValue(elementType))).AppendLine(", json);");
            _builder.Append(indent).Append("    (").Append(local).Append(" ??= new global::System.Collections.Generic.List<")
                .Append(NonNullableDisplay(elementType)).AppendLine(">()).Add(parsed);");
        }

        _builder.Append(indent).AppendLine("    if (comma < 0) break;");
        _builder.Append(indent).AppendLine("    start = comma + 1;");
        _builder.Append(indent).AppendLine("}");
    }

    private void EmitArgumentMatching(CommandModel model)
    {
        _builder.AppendLine("                switch (positional++)");
        _builder.AppendLine("                {");

        for (var i = 0; i < model.Parameters.Count; i++)
        {
            var parameter = model.Parameters[i];
            var local = "a" + i;
            var indent = "                    ";

            _builder.Append(indent).Append("case ").Append(i).AppendLine(":");
            _builder.Append(indent).AppendLine("{");

            if (parameter.IsParams || parameter.IsRepeatable)
            {
                EmitArgumentArrayAppend(parameter, local, indent + "    ");
                _builder.Append(indent).AppendLine("    continue;");
            }
            else if (parameter.Type.Kind == TypeKind.String)
            {
                _builder.Append(indent).Append("    ").Append(local).AppendLine(" = token.Source ?? token.Value.ToString();");

                if (parameter.IsRequired)
                {
                    _builder.Append(indent).Append("    ").Append(local).AppendLine("Set = true;");
                }

                _builder.Append(indent).AppendLine("    continue;");
            }
            else
            {
                var parser = EnsureParser(parameter.Type);
                _builder.Append(indent).Append("    if (!").Append(parser).AppendLine("(token.Value, out var parsed))");
                _builder.Append(indent).Append("        return global::RunesHela.RunesHelaErrors.InvalidArgumentValue(commandPath, ")
                    .Append(CodeLiteral.String(parameter.Name)).Append(", token.Value.ToString(), ")
                    .Append(CodeLiteral.String(ExpectedValue(parameter.Type))).AppendLine(", json);");
                _builder.Append(indent).Append("    ").Append(local).AppendLine(" = parsed;");

                if (parameter.IsRequired)
                {
                    _builder.Append(indent).Append("    ").Append(local).AppendLine("Set = true;");
                }

                _builder.Append(indent).AppendLine("    continue;");
            }

            _builder.Append(indent).AppendLine("}");
        }

        var paramsIndex = FindParamsIndex(model);

        _builder.AppendLine("                    default:");

        if (paramsIndex >= 0)
        {
            _builder.AppendLine("                    {");
            EmitArgumentArrayAppend(
                model.Parameters[paramsIndex],
                "a" + paramsIndex,
                "                        ");
            _builder.AppendLine("                        continue;");
            _builder.AppendLine("                    }");
        }
        else
        {
            _builder.AppendLine("                        return global::RunesHela.RunesHelaErrors.UnexpectedArgument(commandPath, token.Value.ToString(), json);");
        }

        _builder.AppendLine("                }");
    }

    private void EmitArgumentArrayAppend(
        ParameterModel parameter,
        string local,
        string indent)
    {
        var elementType = parameter.Type.ElementType!;

        if (elementType.Kind == TypeKind.String)
        {
            _builder.Append(indent).Append("(").Append(local).Append(" ??= new global::System.Collections.Generic.List<")
                .Append(NonNullableDisplay(elementType)).AppendLine(">()).Add(token.Source ?? token.Value.ToString());");
            return;
        }

        var parser = EnsureParser(elementType);
        _builder.Append(indent).Append("if (!").Append(parser).AppendLine("(token.Value, out var parsed))");
        _builder.Append(indent).Append("    return global::RunesHela.RunesHelaErrors.InvalidArgumentValue(commandPath, ")
            .Append(CodeLiteral.String(parameter.Name)).Append(", token.Value.ToString(), ")
            .Append(CodeLiteral.String(ExpectedValue(elementType))).AppendLine(", json);");
        _builder.Append(indent).Append("(").Append(local).Append(" ??= new global::System.Collections.Generic.List<")
            .Append(NonNullableDisplay(elementType)).AppendLine(">()).Add(parsed);");
    }

    private static int FindParamsIndex(CommandModel model)
    {
        for (var i = 0; i < model.Parameters.Count; i++)
        {
            if (model.Parameters[i].IsParams)
            {
                return i;
            }
        }

        return -1;
    }

    private void EmitEnvironmentFallbacks(CommandModel model)
    {
        for (var i = 0; i < model.Options.Count; i++)
        {
            var option = model.Options[i];
            var envName = ConfigurationConventions.EnvVar(_appName, _node.Path, option);
            var local = "o" + i;
            var setFlag = local + "Set";
            var condition = option.IsRepeatable ? local + " is null" : "!" + setFlag;

            _builder.Append("        if (").Append(condition).AppendLine(")");
            _builder.AppendLine("        {");
            _builder.Append("            var envValue = global::System.Environment.GetEnvironmentVariable(")
                .Append(CodeLiteral.String(envName)).AppendLine(");");
            _builder.AppendLine("            if (envValue is not null)");
            _builder.AppendLine("            {");

            if (option.IsRepeatable)
            {
                var elementType = option.Type.ElementType!;
                _builder.AppendLine("                var start = 0;");
                _builder.AppendLine("                while (true)");
                _builder.AppendLine("                {");
                _builder.AppendLine("                    var comma = envValue.IndexOf(',', start);");
                _builder.AppendLine("                    var part = comma < 0 ? envValue.AsSpan(start) : envValue.AsSpan(start, comma - start);");

                if (elementType.Kind == TypeKind.String)
                {
                    _builder.Append("                    (").Append(local)
                        .Append(" ??= new global::System.Collections.Generic.List<string>()).Add(part.ToString());");
                    _builder.AppendLine();
                }
                else
                {
                    var parser = EnsureParser(elementType);
                    _builder.Append("                    if (!").Append(parser).AppendLine("(part, out var parsed))");
                    _builder.Append("                        return global::RunesHela.RunesHelaErrors.InvalidValue(commandPath, ")
                        .Append(CodeLiteral.String(option.LongName)).Append(", ")
                        .Append(option.IsSecret ? "null" : "envValue")
                        .Append(", ").Append(CodeLiteral.String(ExpectedValue(elementType))).AppendLine(", json);");
                    _builder.Append("                    (").Append(local)
                        .Append(" ??= new global::System.Collections.Generic.List<")
                        .Append(NonNullableDisplay(elementType)).AppendLine(">()).Add(parsed);");
                }

                _builder.AppendLine("                    if (comma < 0) break;");
                _builder.AppendLine("                    start = comma + 1;");
                _builder.AppendLine("                }");
            }
            else if (option.IsFlag)
            {
                _builder.AppendLine("                if (!global::RunesHela.RunesHelaEnvironment.TryParseBoolean(envValue, out var parsed))");
                _builder.Append("                    return global::RunesHela.RunesHelaErrors.InvalidValue(commandPath, ")
                    .Append(CodeLiteral.String(option.LongName))
                    .Append(", envValue, \"true or false\", json);");
                _builder.AppendLine();
                _builder.Append("                ").Append(local).AppendLine(" = parsed;");
            }
            else if (option.Type.Kind == TypeKind.String)
            {
                _builder.Append("                ").Append(local).AppendLine(" = envValue;");
            }
            else
            {
                var parser = EnsureParser(option.Type);
                _builder.Append("                if (!").Append(parser).AppendLine("(envValue, out var parsed))");
                _builder.Append("                    return global::RunesHela.RunesHelaErrors.InvalidValue(commandPath, ")
                    .Append(CodeLiteral.String(option.LongName)).Append(", ")
                    .Append(option.IsSecret ? "null" : "envValue")
                    .Append(", ").Append(CodeLiteral.String(ExpectedValue(option.Type))).AppendLine(", json);");
                _builder.Append("                ").Append(local).AppendLine(" = parsed;");
            }

            if (!option.IsRepeatable)
            {
                _builder.Append("                ").Append(setFlag).AppendLine(" = true;");
            }

            _builder.AppendLine("            }");
            _builder.AppendLine("        }");
            _builder.AppendLine();
        }
    }

    private void EmitConfigFallbacks(CommandModel model)
    {
        if (model.Options.Count == 0)
        {
            return;
        }

        _builder.AppendLine("        var config = global::RunesHela.RunesHelaConfiguration.Load(localConfigPath, AppName, json, "
            + (_configFileName is null ? "null" : CodeLiteral.String(_configFileName))
            + ", commandPath);");
        _builder.AppendLine("        if (config.ExitCode != 0)");
        _builder.AppendLine("        {");
        _builder.AppendLine("            return config.ExitCode;");
        _builder.AppendLine("        }");
        _builder.AppendLine();

        for (var i = 0; i < model.Options.Count; i++)
        {
            var option = model.Options[i];
            var key = ConfigurationConventions.ConfigKey(_node.Path, option);
            var local = "o" + i;
            var setFlag = local + "Set";
            var condition = option.IsRepeatable ? local + " is null" : "!" + setFlag;

            _builder.Append("        if (").Append(condition).AppendLine(")");
            _builder.AppendLine("        {");

            if (!option.IsRepeatable)
            {
                _builder.Append("            if (config.TryGetValue(").Append(CodeLiteral.String(key))
                    .Append(", out var configValue").Append(i).AppendLine("))");
                _builder.AppendLine("            {");

                if (option.Type.Kind == TypeKind.String)
                {
                    _builder.Append("                ").Append(local).Append(" = configValue").Append(i).AppendLine(";");
                }
                else
                {
                    var parser = EnsureParser(option.Type);
                    _builder.Append("                if (!").Append(parser).Append("(configValue").Append(i)
                        .Append(", out var configParsed").Append(i).AppendLine("))");
                    _builder.AppendLine("                {");
                    _builder.Append("                    return global::RunesHela.RunesHelaErrors.ConfigInvalidValue(config.Path ?? \"configuration\", ")
                        .Append(CodeLiteral.String(key)).Append(", ")
                        .Append(CodeLiteral.String(ExpectedValue(option.Type))).AppendLine(", json, commandPath);");
                    _builder.AppendLine("                }");
                    _builder.Append("                ").Append(local).Append(" = configParsed").Append(i).AppendLine(";");
                }

                _builder.Append("                ").Append(setFlag).AppendLine(" = true;");
                _builder.AppendLine("            }");
                _builder.AppendLine("        }");
                _builder.AppendLine();
                continue;
            }

            _builder.Append("            if (config.TryGetValues(").Append(CodeLiteral.String(key))
                .Append(", out var configItems").Append(i).AppendLine("))");
            _builder.AppendLine("            {");

            var elementType = option.Type.ElementType!;
            _builder.Append("                var configList").Append(i)
                .Append(" = new global::System.Collections.Generic.List<")
                .Append(NonNullableDisplay(elementType)).AppendLine(">();");
            _builder.Append("                for (var configIndex").Append(i).Append(" = 0; configIndex").Append(i)
                .Append(" < configItems").Append(i).AppendLine(".Length; configIndex" + i + "++)");
            _builder.AppendLine("                {");

            if (elementType.Kind == TypeKind.String)
            {
                _builder.Append("                    configList").Append(i).Append(".Add(configItems").Append(i)
                    .AppendLine("[configIndex" + i + "]);");
            }
            else
            {
                var parser = EnsureParser(elementType);
                _builder.Append("                    if (!").Append(parser).Append("(configItems").Append(i)
                    .Append("[configIndex").Append(i).Append("], out var configItem").Append(i).AppendLine("))");
                _builder.AppendLine("                    {");
                _builder.Append("                        return global::RunesHela.RunesHelaErrors.ConfigInvalidValue(config.Path ?? \"configuration\", ")
                    .Append(CodeLiteral.String(key)).Append(", ")
                    .Append(CodeLiteral.String(ExpectedValue(elementType))).AppendLine(", json, commandPath);");
                _builder.AppendLine("                    }");
                _builder.Append("                    configList").Append(i).Append(".Add(configItem").Append(i).AppendLine(");");
            }

            _builder.AppendLine("                }");
            _builder.Append("                ").Append(local).Append(" = configList").Append(i).AppendLine(";");
            _builder.AppendLine("            }");
            _builder.Append("            else if (config.TryGetValue(").Append(CodeLiteral.String(key))
                .Append(", out var configValue").Append(i).AppendLine("))");
            _builder.AppendLine("            {");

            if (elementType.Kind == TypeKind.String)
            {
                _builder.Append("                ").Append(local)
                    .Append(" = new global::System.Collections.Generic.List<string>(configValue")
                    .Append(i).AppendLine(".Split(','));");
            }
            else
            {
                var parser = EnsureParser(elementType);
                _builder.Append("                ").Append(local)
                    .Append(" = new global::System.Collections.Generic.List<")
                    .Append(NonNullableDisplay(elementType)).AppendLine(">();");
                _builder.AppendLine("                var configStart" + i + " = 0;");
                _builder.AppendLine("                while (true)");
                _builder.AppendLine("                {");
                _builder.Append("                    var configComma").Append(i).Append(" = configValue").Append(i)
                    .AppendLine(".IndexOf(',', configStart" + i + ");");
                _builder.Append("                    var configPart").Append(i).Append(" = configComma").Append(i)
                    .Append(" < 0 ? configValue").Append(i).Append(".AsSpan(configStart" + i + ") : configValue")
                    .Append(i).Append(".AsSpan(configStart").Append(i).Append(", configComma").Append(i)
                    .Append(" - configStart").Append(i).AppendLine(");");
                _builder.Append("                    if (!").Append(parser).Append("(configPart").Append(i)
                    .Append(", out var configParsed").Append(i).AppendLine("))");
                _builder.AppendLine("                    {");
                _builder.Append("                        return global::RunesHela.RunesHelaErrors.ConfigInvalidValue(config.Path ?? \"configuration\", ")
                    .Append(CodeLiteral.String(key)).Append(", ")
                    .Append(CodeLiteral.String(ExpectedValue(elementType))).AppendLine(", json, commandPath);");
                _builder.AppendLine("                    }");
                _builder.Append("                    ").Append(local).Append(".Add(configParsed").Append(i).AppendLine(");");
                _builder.Append("                    if (configComma").Append(i).AppendLine(" < 0) break;");
                _builder.Append("                    configStart").Append(i).Append(" = configComma").Append(i).AppendLine(" + 1;");
                _builder.AppendLine("                }");
            }

            _builder.AppendLine("            }");
            _builder.AppendLine("        }");
            _builder.AppendLine();
        }
    }

    private void EmitRequiredChecks(CommandModel model)
    {
        for (var i = 0; i < model.Options.Count; i++)
        {
            var option = model.Options[i];

            if (!option.IsRequired)
            {
                continue;
            }

            var local = "o" + i;
            var condition = option.IsRepeatable ? local + " is null" : "!" + local + "Set";
            var envName = ConfigurationConventions.EnvVar(_appName, _node.Path, option);

            _builder.Append("        if (").Append(condition).AppendLine(")");
            _builder.Append("            return global::RunesHela.RunesHelaErrors.MissingRequiredOption(commandPath, ")
                .Append(CodeLiteral.String(option.LongName)).Append(", ")
                .Append(CodeLiteral.String(envName))
                .AppendLine(", json);");
            _builder.AppendLine();
        }

        for (var i = 0; i < model.Parameters.Count; i++)
        {
            var parameter = model.Parameters[i];

            if (!parameter.IsRequired || parameter.IsParams || parameter.IsRepeatable)
            {
                continue;
            }

            _builder.Append("        if (!a").Append(i).AppendLine("Set)");
            _builder.Append("            return global::RunesHela.RunesHelaErrors.MissingRequiredArgument(commandPath, ")
                .Append(CodeLiteral.String(parameter.Name)).AppendLine(", json);");
            _builder.AppendLine();
        }
    }

    private void EmitValidations(CommandModel model)
    {
        for (var i = 0; i < model.Options.Count; i++)
        {
            EmitValidation(model.Options[i], "o" + i, i, parameter: null);
        }

        for (var i = 0; i < model.Parameters.Count; i++)
        {
            EmitValidation(option: null, "a" + i, i, model.Parameters[i]);
        }
    }

    private void EmitValidation(
        OptionModel? option,
        string local,
        int index,
        ParameterModel? parameter)
    {
        var validations = option?.Validations ?? parameter!.Validations;
        var type = option?.Type ?? parameter!.Type;
        var displayName = option?.LongName ?? parameter!.Name;
        var validationIndex = 0;

        foreach (var validation in validations)
        {
            switch (validation.Kind)
            {
                case ValidationKind.Range:
                {
                    var condition = BuildRangeCondition(local, type, index, validationIndex, validation.Min, validation.Max);
                    validationIndex++;

                    _builder.Append("        if (").Append(condition).AppendLine(")");
                    _builder.Append("            return global::RunesHela.RunesHelaErrors.ValidationFailed(commandPath, ")
                        .Append(CodeLiteral.String(displayName)).Append(", ")
                        .Append(CodeLiteral.String("must be between " + validation.Min + " and " + validation.Max))
                        .AppendLine(", json);");
                    _builder.AppendLine();
                    break;
                }

                case ValidationKind.Length:
                {
                    var guard = type.IsReferenceType ? local + " is not null && " : string.Empty;
                    var condition = guard + "(" + local + ".Length < " + validation.Min + " || " + local + ".Length > " + validation.Max + ")";

                    _builder.Append("        if (").Append(condition).AppendLine(")");
                    _builder.Append("            return global::RunesHela.RunesHelaErrors.ValidationFailed(commandPath, ")
                        .Append(CodeLiteral.String(displayName)).Append(", ")
                        .Append(CodeLiteral.String("must have a length between " + validation.Min + " and " + validation.Max))
                        .AppendLine(", json);");
                    _builder.AppendLine();
                    break;
                }

                case ValidationKind.Regex:
                {
                    var guard = type.IsReferenceType ? local + " is not null && " : string.Empty;
                    var condition = guard + "!global::System.Text.RegularExpressions.Regex.IsMatch(" + local + ", "
                        + CodeLiteral.String(validation.Pattern) + ")";

                    _builder.Append("        if (").Append(condition).AppendLine(")");
                    _builder.Append("            return global::RunesHela.RunesHelaErrors.ValidationFailed(commandPath, ")
                        .Append(CodeLiteral.String(displayName)).Append(", ")
                        .Append(CodeLiteral.String("must match pattern '" + validation.Pattern + "'"))
                        .AppendLine(", json);");
                    _builder.AppendLine();
                    break;
                }
            }
        }
    }

    private static string BuildRangeCondition(
        string local,
        TypeModel type,
        int index,
        int validationIndex,
        string min,
        string max)
    {
        if (type.IsNullable && !type.IsReferenceType)
        {
            var pattern = "range" + index + "_" + validationIndex;
            return local + " is { } " + pattern + " && ((double)" + pattern + " < " + min + " || (double)" + pattern + " > " + max + ")";
        }

        return "(double)" + local + " < " + min + " || (double)" + local + " > " + max;
    }

    private void EmitConstruction(CommandModel model)
    {
        if (model.IsDestructive)
        {
            _builder.AppendLine("        var guardResult = global::RunesHela.RunesHelaGuard.EnsureConfirmed(commandPath, global::RunesHela.RunesHelaOutputOptions.WantsYes(args), global::RunesHela.RunesHelaOutputOptions.WantsNoInput(args), json);");
            _builder.AppendLine("        if (guardResult != 0)");
            _builder.AppendLine("        {");
            _builder.AppendLine("            return guardResult;");
            _builder.AppendLine("        }");
            _builder.AppendLine();
        }

        if (model.IsStatic)
        {
            _builder.AppendLine("        return 0;");
            return;
        }

        var arguments = new List<string>();

        if (!model.HasParameterlessConstructor)
        {
            _builder.AppendLine("        if (services is null)");
            _builder.AppendLine("            return global::RunesHela.RunesHelaErrors.ServicesMissing(commandPath, json);");
            _builder.AppendLine();

            for (var i = 0; i < model.ConstructorParameters.Count; i++)
            {
                var typeName = model.ConstructorParameters[i];

                if (typeName == "global::System.Threading.CancellationToken")
                {
                    arguments.Add("cancellationToken");
                    continue;
                }

                if (typeName == "global::System.IServiceProvider")
                {
                    arguments.Add("services!");
                    continue;
                }

                _builder.Append("        var service").Append(i).Append(" = services.GetService(typeof(")
                    .Append(typeName).AppendLine("));");
                _builder.Append("        if (service").Append(i).Append(" is not ").Append(typeName)
                    .Append(" param").Append(i).AppendLine(")");
                _builder.Append("            return global::RunesHela.RunesHelaErrors.ServiceMissing(commandPath, ")
                    .Append(CodeLiteral.String(StripGlobal(typeName))).AppendLine(", json);");
                arguments.Add("param" + i);
            }

            _builder.AppendLine();
        }

        var initializer = BuildInitializer(model);
        var constructorArguments = arguments.Count == 0 ? string.Empty : string.Join(", ", arguments);

        _builder.Append("        var instance = new ").Append(model.TypeFullName)
            .Append('(').Append(constructorArguments).Append(')');

        if (initializer.Count > 0)
        {
            _builder.AppendLine();
            _builder.AppendLine("        {");

            foreach (var assignment in initializer)
            {
                _builder.Append("            ").AppendLine(assignment);
            }

            _builder.AppendLine("        };");
        }
        else
        {
            _builder.AppendLine(";");
        }

        if (!model.IsStatic)
        {
            _builder.AppendLine("        command = instance;");
        }

        _builder.AppendLine("        return 0;");
    }

    private List<string> BuildInitializer(CommandModel model)
    {
        var assignments = new List<string>();

        for (var i = 0; i < model.Options.Count; i++)
        {
            var option = model.Options[i];

            if (option.IsRepeatable)
            {
                var elementType = NonNullableDisplay(option.Type.ElementType!);
                assignments.Add(option.PropertyName + " = o" + i + " is null ? global::System.Array.Empty<"
                    + elementType + ">() : o" + i + ".ToArray(),");
                continue;
            }

            var value = "o" + i;

            if (needsNullForgiving(option.Type))
            {
                value += "!";
            }

            assignments.Add(option.PropertyName + " = " + value + ",");
        }

        for (var i = 0; i < model.Parameters.Count; i++)
        {
            var parameter = model.Parameters[i];

            if (parameter.IsRepeatable)
            {
                var elementType = NonNullableDisplay(parameter.Type.ElementType!);
                assignments.Add(parameter.PropertyName + " = a" + i + " is null ? global::System.Array.Empty<"
                    + elementType + ">() : a" + i + ".ToArray(),");
                continue;
            }

            var value = "a" + i;

            if (needsNullForgiving(parameter.Type))
            {
                value += "!";
            }

            assignments.Add(parameter.PropertyName + " = " + value + ",");
        }

        return assignments;

        static bool needsNullForgiving(TypeModel type)
        {
            return type.IsReferenceType && !type.IsNullable;
        }
    }

    private void EmitParserMethods()
    {
        foreach (var parser in _parsers)
        {
            _builder.Append("    private static bool ").Append(parser.Name)
                .Append("(ReadOnlySpan<char> text, out ").Append(NonNullableDisplay(parser.Type)).AppendLine(" value)");
            _builder.AppendLine("    {");

            switch (parser.Type.Kind)
            {
                case TypeKind.Char:
                    _builder.AppendLine("        if (text.Length != 1)");
                    _builder.AppendLine("        {");
                    _builder.AppendLine("            value = default;");
                    _builder.AppendLine("            return false;");
                    _builder.AppendLine("        }");
                    _builder.AppendLine();
                    _builder.AppendLine("        value = text[0];");
                    _builder.AppendLine("        return true;");
                    break;

                case TypeKind.Boolean:
                    _builder.AppendLine("        return global::System.Boolean.TryParse(text, out value);");
                    break;

                case TypeKind.Enum:
                    EmitEnumParser(parser.Type);
                    break;

                case TypeKind.Uri:
                    _builder.AppendLine("        if (!global::System.Uri.TryCreate(text, global::System.UriKind.RelativeOrAbsolute, out var result) || result is null)");
                    _builder.AppendLine("        {");
                    _builder.AppendLine("            value = null!;");
                    _builder.AppendLine("            return false;");
                    _builder.AppendLine("        }");
                    _builder.AppendLine();
                    _builder.AppendLine("        value = result;");
                    _builder.AppendLine("        return true;");
                    break;

                case TypeKind.FileInfo:
                    _builder.AppendLine("        value = new global::System.IO.FileInfo(text.ToString());");
                    _builder.AppendLine("        return true;");
                    break;

                case TypeKind.DirectoryInfo:
                    _builder.AppendLine("        value = new global::System.IO.DirectoryInfo(text.ToString());");
                    _builder.AppendLine("        return true;");
                    break;

                case TypeKind.Parsable when !parser.Type.IsSpanParsable:
                    _builder.AppendLine("        return global::RunesHela.Parsing.ValueParser.TryParseString(text.ToString(), out value);");
                    break;

                default:
                    _builder.AppendLine("        return global::RunesHela.Parsing.ValueParser.TryParse(text, out value);");
                    break;
            }

            _builder.AppendLine("    }");
            _builder.AppendLine();
        }
    }

    private void EmitEnumParser(TypeModel type)
    {
        foreach (var member in type.Members)
        {
            _builder.Append("        if (text.Length == ").Append(member.Length)
                .Append(" && text.Equals(").Append(CodeLiteral.String(member))
                .Append(", global::System.StringComparison.OrdinalIgnoreCase)) { value = ")
                .Append(type.DisplayName).Append('.').Append(member).AppendLine("; return true; }");
        }

        _builder.AppendLine();
        _builder.AppendLine("        value = default;");
        _builder.AppendLine("        return false;");
    }

    private string EnsureParser(TypeModel type)
    {
        var name = "TryParseValue" + _nodeIndex + "_" + _variableIndex++;

        _parsers.Add(new ParserMethod(type, name));
        return name;
    }

    private static bool NeedsSetFlag(OptionModel option)
    {
        return !option.IsRepeatable;
    }

    private static string MatchExpression(string nameExpression, string longName, EquatableArray<string> aliases)
    {
        var builder = new StringBuilder();
        builder.Append('(').Append(nameExpression).Append(".Length == ").Append(longName.Length)
            .Append(" && ").Append(nameExpression).Append(".Equals(").Append(CodeLiteral.String(longName))
            .Append(", global::System.StringComparison.OrdinalIgnoreCase))");

        foreach (var alias in aliases)
        {
            builder.Append(" || (").Append(nameExpression).Append(".Length == ").Append(alias.Length)
                .Append(" && ").Append(nameExpression).Append(".Equals(").Append(CodeLiteral.String(alias))
                .Append(", global::System.StringComparison.OrdinalIgnoreCase))");
        }

        return builder.ToString();
    }

    private static bool HasShortNames(CommandModel model)
    {
        foreach (var option in model.Options)
        {
            if (option.ShortName != '\0')
            {
                return true;
            }
        }

        return false;
    }

    private static string SpanToString(string spanExpression)
    {
        return spanExpression + ".ToString()";
    }

    private static string LocalType(TypeModel type)
    {
        return type.DisplayName;
    }

    private static string NonNullableDisplay(TypeModel type)
    {
        if (!type.IsReferenceType && type.IsNullable && type.DisplayName.EndsWith("?", StringComparison.Ordinal))
        {
            return type.DisplayName.Substring(0, type.DisplayName.Length - 1);
        }

        return type.DisplayName;
    }

    private static string StripGlobal(string typeName)
    {
        const string prefix = "global::";
        return typeName.StartsWith(prefix, StringComparison.Ordinal) ? typeName.Substring(prefix.Length) : typeName;
    }

    private static string ExpectedValue(TypeModel type)
    {
        if (type.Kind == TypeKind.Array && type.ElementType is not null)
        {
            return ExpectedValue(type.ElementType);
        }

        switch (type.Kind)
        {
            case TypeKind.String:
                return "text";
            case TypeKind.Char:
                return "a single character";
            case TypeKind.Boolean:
                return "true or false";
            case TypeKind.Integer:
                return "an integer";
            case TypeKind.Floating:
            case TypeKind.Decimal:
                return "a number";
            case TypeKind.Guid:
                return "a GUID";
            case TypeKind.DateTime:
            case TypeKind.DateTimeOffset:
                return "a timestamp";
            case TypeKind.TimeSpan:
                return "a duration";
            case TypeKind.Uri:
                return "a URI";
            case TypeKind.FileInfo:
            case TypeKind.DirectoryInfo:
                return "a path";
            case TypeKind.Enum:
                return "one of: " + string.Join(", ", GetMembers(type));
            default:
                return "a valid " + type.SimpleName;
        }
    }

    private static IEnumerable<string> GetMembers(TypeModel type)
    {
        foreach (var member in type.Members)
        {
            yield return member;
        }
    }

    private readonly struct ParserMethod
    {
        public ParserMethod(TypeModel type, string name)
        {
            Type = type;
            Name = name;
        }

        public TypeModel Type { get; }

        public string Name { get; }
    }
}
