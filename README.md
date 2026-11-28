# RunesHela · 设计文档集

> **项目名称**：RunesHela（已选定，2026-11-26；见 [06 决策记录](./docs/06-决策记录.md) D-06）
> **状态**：**M6 已完成（2026-11-28）**，下一步 M7（MCP，可选）
> **日期**：2026-11-28

---

## 这个项目是什么

**RunesHela 是一个面向 .NET 的编译期 CLI 框架**：开发者只声明命令（类或方法），编译器生成解析器、绑定器、帮助、补全、文档、Schema 与 MCP 工具；运行时零反射、零第三方依赖、热路径零分配，并默认对人、脚本与 AI Agent 三类消费者友好。

一句话：

> **把 CLI 当作编译器问题：一份声明，编译出全部产物。**

---

## 快速开始（5 分钟）

1. 新建控制台项目，引用 `RunesHela`（本地开发用 `ProjectReference` 同时引用 `RunesHela.Generator`，并设置 `OutputItemType="Analyzer"`）。
2. 定义命令类（**不需要** `partial`、接口或基类）：

```csharp
using RunesHela;

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

3. 入口只写一行：`return RunesHelaApp.Run(args);`
4. 运行：

```text
app greet Ada --count 2
app greet --help
app greet --help --json
```

> 用文档注释（`<summary>`/`<example>`）补充帮助时，项目需启用
> `<GenerateDocumentationFile>true</GenerateDocumentationFile>`；否则请用
> `[Command(Description = ...)]` / `[Option(Description = ...)]`。

### M2 已具备的能力

- **纯 POCO 绑定**：`[Option]`/`[Argument]` 属性绑定，支持 `set`/`init`，无需 `partial`。
- **选项语法**：`--opt=value`、`--opt value`、`-o value`、`-o=value`、布尔旗标、`--no-flag`、`--` 终止符、负数位置参数。
- **类型转换**：基元/枚举/`Guid`/`DateTime(Offset)`/`TimeSpan`/`Uri`/`FileInfo`/`DirectoryInfo`/数组/可空/`IParsable`。
- **优先级**：CLI > 环境变量（`Env = "..."`）> 属性初始化器默认值。
- **校验**：`[Required]`、`[Range]`、`[Regex]`、`[Length]` 在生成代码中执行，零反射。
- **帮助**：人类可读（Usage/参数/命令/选项/示例/退出码）与 `--help --json` 机器可读，同源于模型。
- **错误契约**：`RunesHelaError`（`code`/`message`/`hint`/`nextSteps`/`docsUrl`/`exitCode`），文本与 JSON 两种渲染；数据走 stdout，诊断走 stderr。
- **安全与终端**：`[Secret]` 在帮助/错误中脱敏；`NO_COLOR`/`TERM=dumb`/非 TTY 自动去色；帮助不含 ANSI。
- **DI**：命令无无参构造时，生成器通过 `IServiceProvider` 解析构造参数（`RunAsync(args, services)`）。
- **诊断**：`RHL001`–`RHL040` 编译期诊断，见 [errors.md](./docs/errors.md)。

### M3 已具备的能力

- **热路径优化**：选项名称长度前置比较、短选项 `switch`、枚举长度判断、`WantsJson` 单次扫描、位置参数字符串复用。
- **零分配验证**：5 选项绑定净分配 0 B（不含命令实例），帮助 0 B，错误 304 B；Release 下以 `GC.GetAllocatedBytesForCurrentThread` 断言。
- **性能门禁**：`tests/RunesHela.Performance.Tests` 以绝对预算（含 +10% 失败阈值）在 CI 执行，独立 `perf` job 运行微基准并上传 JSON。
- **生成器性能**：1/10/50/200 命令 no-op 0.02–0.04 ms（预算 ≤30 ms，失败阈值 50 ms）。
- **AOT 矩阵**：linux-x64 / win-x64 / osx-arm64 三平台 AOT + Trim/SingleFile + ReadyToRun 冒烟全部通过；启动增量 0.14 ms。
- **DI 验证**：缺失服务提供器/缺失注册分别给出稳定错误码与退出码，不挂起。

### M4 已具备的能力（Agent 契约）

- **非 TTY 自动模式**：帮助恒无 ANSI；错误仅在交互式终端着色（`NO_COLOR`/`TERM=dumb`/重定向自动关闭）；数据走 stdout、诊断走 stderr。
- **全局选项前置**：`--json`、`--plain`、`--no-color`、`--quiet`、`--verbose`、`--no-input`、`--yes`、`--format <text|plain|json>` 可在命令前解析，未知格式返回退出码 2。
- **结构化错误**：`code`/`message`/`hint`/`nextSteps`/`docsUrl`/`exitCode`，未知命令与选项给出 did-you-mean 建议。
- **破坏性守卫**：`[Command(IsDestructive = true)]`；非 TTY 或缺 `--yes` 立即失败（退出码 2），TTY 下交互确认，`--no-input` 禁止提示。
- **退出码矩阵**：0/1/2/3/4/5/6/7/130/141，`RunesHelaExitCode` 常量；取消 → 130，EPIPE → 141。
- **自描述**：`describe` 输出全树 JSON，与 `--help --json` 同源（选项/默认值/ENV/退出码）。
- **过滤器链**：`[Command(Filters = new[] { nameof(AuditFilter) })]`，按声明顺序包裹 handler，`CommandContext` 提供路径/服务/取消令牌；生成期校验契约与构造（RHL037–RHL039）。

### M5 已具备的能力（补全与文档产物）

- **静态补全**：`app completion bash|zsh|fish|pwsh` 从命令模型生成脚本，覆盖命令/子命令/选项；未知 shell 返回退出码 2 并给出建议。
- **动态补全**：隐藏命令 `app __complete <words...>` 按前缀返回候选（命令/子命令/选项），供脚本或 Agent 消费。
- **文档产物**：`app docs` 输出 Markdown（参数/选项/默认值/ENV/子命令），`app man` 输出 roff（`man -l` 可用）。
- **产物一致性**：help、describe、completion、docs 同源；集成测试逐项比对选项集合与命令集合，防止漂移。
- **保留名保护**：`describe`/`completion`/`docs`/`man`/`__complete` 被注册为保留命令名（RHL025）。

### M6 已具备的能力（配置）

- **优先级**：CLI > ENV > JSON 配置文件 > 属性默认值；CLI 显式空字符串也覆盖其他来源。
- **配置键**：默认 `command.path.option-name`（如 `deploy.environment`），可用 `[Option(Config = "auth.token")]` 覆盖；重复键报 RHL016，非法键报 RHL040。
- **JSON 解析**：`System.Text.Json.JsonDocument`，支持点路径嵌套、标量数组与 `"a,b"` 逗号串，零反射、AOT 安全。
- **显式配置源**：`--config <PATH>`（可放在命令前后）与 `RHL_CONFIG`；显式文件缺失/非法 → 退出码 3（`RHL_CONFIG_NOT_FOUND` / `RHL_CONFIG_INVALID` / `RHL_CONFIG_INVALID_VALUE`），错误信息不含配置值。
- **隐式发现（opt-in）**：`[assembly: RunesHelaConfigFile]` 后按当前目录 `appsettings.json` → `~/.config/<app>/appsettings.json` 查找；未启用时保持绑定热路径零 IO、零分配（AC-04）。
- **ENV 约定**：未显式设置 `Env` 时按 `APP_COMMAND_OPTION` 生成（如 `AOTCLI_GREET_TEXT`）；布尔接受 `1/0`、`true/false`、`yes/no`、`on/off`。
- **Schema**：`app schema`（兼容 `app schema --config`）输出 JSON Schema 2020-12，键/类型/默认值与 `describe` 一致；`[Secret]` 不输出默认值。
- **脱敏**：配置/ENV/CLI 的密钥值在错误与 JSON 中均为 `***` 或完全不出现；Unix 下世界可读且含敏感键的配置文件给出警告（`RHL_NO_CONFIG_WARNINGS=1` 可关闭）。
- **AOT**：`System.Text.Json` 进入产物后体积增量升至 560.7 KB（启动增量 0.28 ms 仍在预算内），与 M4/M5 一并记为 M8 发布前技术债。

---

## 文档地图

| # | 文档 | 内容 | 读者 | 状态 |
|---|---|---|---|---|
| 00 | [深度调研与机会分析](./docs/00-深度调研与机会分析.md) | 市场数据、竞品拆解、刚性/隐形需求、机会缺口、路线评分 | 所有人 | ✅ 完成 |
| 01 | [产品需求文档（PRD）](./docs/01-产品需求文档-PRD.md) | 目标/非目标、55 条 FR、34 条 NFR、20 条 AC、版本范围 | 产品/开发 | ✅ 完成 |
| 02 | [技术设计文档](./docs/02-技术设计文档.md) | 包结构、编译期模型、生成器、运行时、Agent 契约、性能/AOT、诊断、测试、安全 | 开发/架构 | ✅ 完成 |
| 03 | [实现任务分解](./docs/03-实现任务分解.md) | M0~M8 里程碑、55 个任务、DAG、AC 追溯矩阵 | 开发/项目管理 | ✅ 完成 |
| 04 | [验收与质量检查清单](./docs/04-验收与质量检查清单.md) | 文档/设计/功能/性能/AOT/契约/安全/发布检查项 | 测试/发布 | ✅ 完成 |
| 05 | [架构决策记录（ADR）](./docs/05-架构决策记录-ADR.md) | 16 个关键决策的背景、选项、影响、风险 | 架构/维护者 | ✅ 已决议（待审核） |
| 06 | [决策记录](./docs/06-决策记录.md) | **你已做出的 14 项决议** + 复核提示 | 你 | ✅ 已决议 |
| 07 | [M0 基准报告](./docs/benchmarks/M0-基准报告.md) | 解析/帮助/错误/路由/AOT 实测与预算对照 | 开发/评审 | ✅ M0 通过 |
| 08 | [M2 基准报告](./docs/benchmarks/M2-基准报告.md) | M2 回归：帮助 0 B、错误 304 B、集成测试 103 项 | 开发/评审 | ✅ M2 通过 |
| 09 | [M3 基准报告](./docs/benchmarks/M3-基准报告.md) | 热路径 0 B、性能门禁、生成器 no-op、AOT/Trim/R2R 矩阵 | 开发/评审 | ✅ M3 通过 |
| 10 | [M4 基准报告](./docs/benchmarks/M4-基准报告.md) | Agent 契约：非 TTY、错误建议、破坏性守卫、退出码、EPIPE、过滤器 | 开发/评审 | ✅ M4 通过 |
| 11 | [M5 基准报告](./docs/benchmarks/M5-基准报告.md) | 四 shell 补全、动态补全、Man/Markdown、产物一致性 | 开发/评审 | ✅ M5 通过 |
| 12 | [M6 基准报告](./docs/benchmarks/M6-基准报告.md) | 配置优先级、JSON/ENV、Schema、脱敏与零分配快路径 | 开发/评审 | ✅ M6 通过 |
| 13 | [错误码与诊断](./docs/errors.md) | 运行时 `RHL_*` 错误码、`RHLxxx` 诊断码、退出码矩阵 | 开发/使用 | ✅ 随 M6 更新 |

---

## 核心结论（先读这 6 条）

1. **机会不在“又一个 CLI 框架”，而在“编译期 CLI 编译器 + Agent 原生契约”。**
   现有框架要么偏性能（ConsoleAppFramework），要么偏优雅（CliFx），没有同时覆盖“类优先 + 零反射 + 产物同源 + Agent 契约”的方案。

2. **Star 预期要校准。**
   ConsoleAppFramework 单库 2,246 stars、431 万下载；Spectre.Console 11,717 stars、7,400 万下载。Cysharp 的“近 5 万 stars”是组织级累计。**下载量/真实采用**比 star 更能衡量成功。

3. **双引擎是同一模型的两个入口。**
   类优先负责工程体验，方法优先负责“一行 CLI”的传播点；两者共享解析/绑定/产物，避免两套实现。

4. **Agent 是 2026 年的新变量。**
   非 TTY 自动结构化、错误带下一步、破坏性操作不挂起、`describe`/`schema` 自描述——这些是现有 .NET 框架的空白。

5. **性能必须变成工程预算。**
   热路径 0 B、解析 ≤ 1 µs、AOT 启动额外 ≤ 1 ms、二进制增量 ≤ 100 KB、生成器 no-op ≤ 30 ms；全部进 CI 门禁，公开可复现。

6. **最大风险是范围失控。**
   明确不做：TUI、通用 DI、序列化框架、插件市场、Unity、.NET Framework。MVP 只做“类优先 + 核心 + Agent 基础”。

---

## 已决议事项（待你审核）

你已于 **2026-11-26** 做出选择，06 已转为决策记录。**在你审核通过并明确“开始开发”之前，不会进入编码。**

| 编号 | 决策 | 你的选择 |
|---|---|---|
| D-01 | 路线 | **C：双引擎 + Agent 原生** |
| D-02 | 目标框架 | **C：net10.0 单目标 + C# 14** |
| D-03 | 包形态 | **B：极小运行时 + 生成器（单包）** |
| D-04 | API 主轴 | **C：类优先 + 方法优先（V0.2）** |
| D-05 | MVP 边界 | **B：标准 MVP** |
| D-06 | 名称 | **RunesHela** |
| D-07 | 许可证 | **B：Apache-2.0** |
| D-08 | 帮助来源 | **C：特性 > 文档注释 > 约定** |
| D-09 | 渲染 | **B：自研极简 + 适配层** |
| D-10 | 文档语言 | **B：英文优先，中文按需** |
| D-11 | MCP | **C：Manifest + stdio server** |
| D-12 | 国际化 | **C：V1.x 可插拔** |
| D-13 | 交互提示 | **B：V1.x 独立包** |
| D-14 | 遥测/治理 | **A：零遥测 + 赞助** |

> **D-02 已复核**：采用 **net10.0（LTS，支持至 2028-11）单目标**。net9 已于 2026-05 结束支持，net8 LTS 已于 2026-11 结束支持，故不再纳入目标；换取最小 CI 矩阵与 C# 14 能力。

---

## 阅读路径建议

- **只想判断“值不值得做”**：00 → 本文“核心结论” → 06。
- **要评审产品范围**：00 → 01 → 06。
- **要评审技术方案**：02 → 05 → 03。
- **准备开始开发**：01 → 02 → 03 → 04。
- **准备发布**：04 → 05（版本/兼容）→ 06（治理）。

---

## 边界声明

- 当前已完成 **M6**（2026-11-28）：JSON 配置、ENV 约定与优先级、配置 Schema、脱敏与退出码 3；下一步 M7（MCP，可选）。
- 以上决议已按你的选择写入文档；项目已按决议进入开发。

---

## 目录结构

```text
work111/
└─ docs/
   ├─ 00-深度调研与机会分析.md
   ├─ 01-产品需求文档-PRD.md
   ├─ 02-技术设计文档.md
   ├─ 03-实现任务分解.md
   ├─ 04-验收与质量检查清单.md
   ├─ 05-架构决策记录-ADR.md
   └─ 06-决策记录.md
```
