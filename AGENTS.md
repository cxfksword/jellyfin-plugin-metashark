# AGENTS.md

本仓库的人机协作规范，AI agent 与人工提交均适用。

## 提交规范

- commit message 遵循 Conventional Commits，使用英文祈使句，如 `fix: ...`、`ci: ...`。
- 需要关联 issue 时在 commit body 写 `Closes #xxx`（push 后会自动关闭 issue）。

## 测试规范

- 跑测试一律使用 `-c Debug`，例如 `dotnet test Jellyfin.Plugin.MetaShark.Test/Jellyfin.Plugin.MetaShark.Test.csproj -c Debug`。
- **不要**用 `-c Release` 跑测试：Release 构建会触发 `ILRepack` 把 `TMDbLib` 合并并内部化进插件 dll，与测试引用的独立 `TMDbLib.dll` 形成两套类型身份，导致 `TmdbApiTest` 等方法报 `MissingMethodException`（即 TMDbLib API mismatch）。这不是代码问题，用 Debug 即可规避。

## 发版规范

- 发版通过 annotated tag 触发：`publish.yaml` 监听 tag push，自动构建并发布 Release、更新 manifest。
- tag message 必须是本次发布内容的简短中文总结，例如 `修复刮削标题写入错误`；多条时可换行分条，但保持简洁。
- tag message 会直接用作 GitHub Release 正文和 manifest 的 changelog，不要写英文 commit 式描述。
- 打 tag 前先确认 `dotnet build -c Release` 通过、相关测试通过（测试按上文用 `-c Debug` 跑），且分支已 push。
- tag 命名沿用 `v<major>.<minor>.<patch>`（如 `v2.3.8`）。

## 开发原则

- **不考虑向后兼容**：删除过时实现，不添加兼容层、回退逻辑或迁移方案。
- **保持简单**：优先选择满足当前需求的最简单实现，避免过度设计、过早抽象和不必要的配置。
- **渐进式开发**：先实现完整可用的最小版本，再逐步扩展功能。不要为了未完成的复杂功能破坏现有可用性。
- **模块化设计**：保持组件职责单一，明确划分模块与关注点。
- **优先使用成熟依赖**：合理利用项目现有依赖，必要时选择成熟、维护良好的第三方库，避免重复造轮子。
- **先查证再实现**：使用现有依赖前，先检查其文档和 API，不要未经确认就认定其不支持某项功能。
- **重视长期设计**：架构决策应考虑长期维护，避免明知需要替换的临时方案。
