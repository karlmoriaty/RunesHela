# RunesHela（中文说明）

**把 CLI 当作编译器问题：一份声明，编译出全部产物。**

[English](README.md) | 简体中文

RunesHela 是一个面向 .NET 的编译期 CLI 框架：开发者只声明命令类，编译器生成解析器、
绑定器、路由、帮助、补全、Man 页、JSON Schema 与 MCP 工具。生成代码不使用反射，运行时
零第三方依赖，绑定热路径零分配。

**状态**：V1.0.0 已发布到 NuGet.org 与 GitHub Release（2026-11-28）。

[快速开始](docs/quickstart.md) · [变更日志](CHANGELOG.md) · [文档](#文档) · [English](README.md)

---

## 为什么选择 RunesHela

- **一份模型，全部产物**：帮助、`--help --json`、`describe`、`schema`、补全脚本、Man 页、
  Markdown 文档、MCP Manifest 全部来自同一命令模型，不会漂移。
- **零反射、零运行时依赖**：生成器在编译期生成解析器与绑定器，生成代码不扫描程序集、
  不调用反射。
- **零分配热路径**：5 个选项的解析与绑定除命令实例外不产生分配。
- **Native AOT 优先**：CI 验证 Native AOT、Trim、Single-file 与 ReadyToRun。
- **Agent 原生契约**：非 TTY 行为、带下一步的结构化错误、破坏性守卫、稳定退出码、
  自描述，以及零第三方依赖的 MCP stdio server。

---

## 安装

```text
dotnet add package RunesHela --version 1.0.0
```

目标框架：`net10.0`（C# 14）。包内含运行时、XML 文档与
`analyzers/dotnet/cs` 下的源生成器。

---

## 快速开始

```csharp
using RunesHela;

return RunesHelaApp.Run(args);

/// <summary>
/// Greets a person.
/// </summary>
/// <example>greet Ada --count 2</example>
[Command("greet", Description = "Greets a person.")]
public sealed class GreetCommand
{
    /// <summary>Person to greet.</summary>
    [Argument(0, Description = "Person to greet.")]
    public string Name { get; init; } = "world";

    /// <summary>Repeat count.</summary>
    [Option(Short = 'c', Description = "Repeat count.", Env = "APP_COUNT")]
    [Range(1, 5)]
    public int Count { get; init; } = 1;

    public int Run()
    {
        for (var i = 0; i < Count; i++)
        {
            Console.WriteLine("Hello, " + Name + "!");
        }

        return 0;
    }
}
```

不需要 `partial`、接口、基类或手写解析器。

```text
app greet Ada --count 2
app greet --help
app greet --help --json
app describe
app schema
app mcp --manifest
```

使用文档注释（`<summary>` / `<example>`）时需启用
`<GenerateDocumentationFile>true</GenerateDocumentationFile>`；否则请使用
`[Command(Description = ...)]` 与 `[Option(Description = ...)]`。

---

## 生成的产物

| 产物 | 命令 | 内容 |
|---|---|---|
| 人类帮助 | `app --help`、`app <command> --help` | Usage、参数、选项、默认值、ENV、示例、退出码 |
| 机器帮助 | `--help --json` | 供工具/Agent 消费的稳定 JSON 模型 |
| 完整模型 | `app describe` | 全部命令、选项、参数、默认值、ENV、配置键、退出码 |
| JSON Schema | `app schema` | 配置 Schema（JSON Schema 2020-12） |
| 补全 | `app completion bash\|zsh\|fish\|pwsh` | 静态补全脚本；隐藏 `__complete` 提供动态候选 |
| 文档 | `app docs`、`app man` | 由模型生成的 Markdown 与 roff |
| MCP | `app mcp --manifest`、`app mcp --stdio` | 工具清单与 JSON-RPC stdio server |

保留命令名（`describe`、`schema`、`mcp`、`completion`、`docs`、`man`、`__complete`）
由诊断 `RHL025` 保护。

---

## 绑定、校验与配置

- **语法**：`--opt=value`、`--opt value`、`-o value`、`-o=value`、布尔旗标、
  `--no-flag`、`--` 终止符、重复/逗号分隔数组、负数位置参数。
- **类型**：基元、枚举、`Guid`、`DateTime(Offset)`、`TimeSpan`、`Uri`、`FileInfo`、
  `DirectoryInfo`、数组、可空、`IParsable<T>`。
- **优先级**：CLI > 环境变量 > JSON 配置 > 属性初始化器。
- **环境变量**：显式 `[Option(Env = "...")]` 或 `APP_COMMAND_OPTION` 约定；布尔接受
  `1/0`、`true/false`、`yes/no`、`on/off`。
- **配置**：`--config <PATH>` 或 `RHL_CONFIG`；点路径键（`deploy.environment`），
  可用 `[Option(Config = "...")]` 覆盖；`[assembly: RunesHelaConfigFile]` 启用隐式
  `appsettings.json` 发现。
- **校验**：`[Required]`、`[Range]`、`[Regex]`、`[Length]` 在生成代码中执行。
- **错误**：稳定 `RHL_*` 错误码、did-you-mean 建议、`nextSteps`、`docsUrl`，以及
  0/1/2/3/4/5/6/7/130/141 退出码矩阵；`[Secret]` 在所有输出中脱敏。

完整契约见 [错误码与诊断](docs/errors.md)。

---

## Agent 契约

- 非 TTY 输出不含 ANSI；数据走 stdout，诊断走 stderr。
- 破坏性命令（`IsDestructive = true`）在无交互输入且缺少 `--yes` 时立即失败，不会挂起。
- `describe` 与 `--help --json` 暴露完整模型，供 Agent 生成与修复调用。
- `mcp --stdio` 支持 `initialize`、`tools/list`、`tools/call`；结果包含 stdout/stderr、
  `isError` 与 `structuredContent.exitCode`。
- 错误包含可执行的下一步，Agent 无需猜测即可重试。

---

## 性能与 AOT

| 指标 | 目标 | 当前 |
|---|---|---|
| 解析 + 绑定（5 选项） | ≤ 1 µs，0 B | CI 预算门禁；净分配 0 B |
| 帮助/错误分配 | 0 B / 小 | 性能测试断言 |
| Native AOT 启动增量 | ≤ 1 ms | 0.11 ms |
| Native AOT 体积增量 | 目标 ≤ 100 KB，失败阈值 150 KB | 432.7 KB（已知限制，V1.1） |

体积差距记录在 [M8 基准报告](docs/benchmarks/M8-benchmark-report.md)。1.0.0 已移除
`System.Text.Json`，剩余增量来自原生代码与可达框架成员，而非字符串常量；V1.1 计划
提供 minimal profile。

---

## 兼容性

- 语义化版本；当前为 `1.0.0`。
- 公共 API 由 `tests/RunesHela.Tests/PublicApiTests.approved.txt` 跟踪，变更需显式更新基线。
- 生成的 JSON 均含 `schemaVersion`；次版本允许新增字段，删除/改义需要大版本。
- 退出码与 stdout/stderr 分离属于兼容契约。

参见 [CHANGELOG.md](CHANGELOG.md) 与 [发布流程](docs/release.md)。

---

## 文档

| 文档 | 内容 |
|---|---|
| [Quickstart](docs/quickstart.md) | 安装、声明命令、配置、MCP、AOT |
| [迁移指南](docs/migration.md) | 从 System.CommandLine、Spectre.Console.Cli、ConsoleAppFramework、手写 `Main` 迁移 |
| [错误码](docs/errors.md) | 运行时 `RHL_*`、编译期 `RHLxxx`、退出码 |
| [发布流程](docs/release.md) | 版本、可信发布、回滚、供应链 |
| [基准报告](docs/benchmarks/M8-benchmark-report.md) | M0–M8 性能、AOT、打包报告 |
| [English README](README.md) | English documentation |

更深入的设计文档（`docs/00`–`docs/06`，含架构决策记录与基准报告）已全部为英文。

---

## 从源码构建

```text
dotnet restore RunesHela.slnx
dotnet build RunesHela.slnx -c Release -m:1
dotnet test tests/RunesHela.Tests -c Release
dotnet test tests/RunesHela.Generator.Tests -c Release
dotnet test tests/RunesHela.Integration.Tests -c Release
dotnet test tests/RunesHela.Performance.Tests -c Release
```

打包（运行时 + analyzer + 符号包）：

```text
dotnet pack src/RunesHela/RunesHela.csproj -c Release -o artifacts/packages
```

---

## 贡献、安全与许可

- [贡献指南](CONTRIBUTING.md)：构建、测试、DCO 签名（`git commit -s`）与 PR 检查。
- [安全策略](SECURITY.md)：私密漏洞报告与披露时间线。
- [Apache-2.0](LICENSE)，另见 [NOTICE](NOTICE)。

RunesHela 与其他 CLI 框架无关联；运行时仅依赖 BCL。
