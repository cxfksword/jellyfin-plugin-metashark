# AGENTS.md

本仓库的人机协作规范，AI agent 与人工提交均适用。

## 提交规范

- commit message 遵循 Conventional Commits，使用英文祈使句，如 `fix: ...`、`ci: ...`。
- 需要关联 issue 时在 commit body 写 `Closes #xxx`（push 后会自动关闭 issue）。

## 发版规范

- 发版通过 annotated tag 触发：`publish.yaml` 监听 tag push，自动构建并发布 Release、更新 manifest。
- tag message 必须是本次发布内容的简短中文总结，例如 `修复刮削标题写入错误`；多条时可换行分条，但保持简洁。
- tag message 会直接用作 GitHub Release 正文和 manifest 的 changelog，不要写英文 commit 式描述。
- 打 tag 前先确认 `dotnet build -c Release` 通过、相关测试通过，且分支已 push。
- tag 命名沿用 `v<major>.<minor>.<patch>`（如 `v2.3.8`）。
