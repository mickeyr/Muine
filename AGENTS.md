<INSTRUCTIONS>
## MCP Tooling Policy

This project has MCP servers available. Use them proactively and consistently when they are available.
If any MCP server or tool is unavailable (not installed, not configured, or fails), fall back to the normal non-MCP workflow and continue the task.

### 1) Serena (code + project operations)
When to use:
- Any codebase exploration, file reads, symbol search, refactors, or edits.
- Any time you would otherwise use raw shell commands to inspect or edit files.

How to use:
- Activate the project with mcp__serena__activate_project when starting work.
- Run mcp__serena__check_onboarding_performed and mcp__serena__onboarding if needed.
- Use mcp__serena__list_dir, mcp__serena__find_file, mcp__serena__search_for_pattern for discovery.
- Use mcp__serena__get_symbols_overview, mcp__serena__find_symbol, mcp__serena__find_referencing_symbols for code navigation.
- Use mcp__serena__read_file for file content.
- Use mcp__serena__replace_content, mcp__serena__replace_symbol_body, mcp__serena__insert_before_symbol, mcp__serena__insert_after_symbol, or mcp__serena__rename_symbol for edits.
- Always call mcp__serena__think_about_collected_information after a non-trivial search sequence.
- Always call mcp__serena__think_about_task_adherence before applying edits.

Do not default to shell tools for code exploration if Serena can do the job.

### 2) Microsoft Learn MCP (Microsoft/Azure docs and code)
When to use:
- Any question involving Microsoft/Azure products, SDKs, or APIs.
- Any time you are writing Microsoft/Azure-related code.

How to use:
- Start with mcp__microsoft-learn__microsoft_docs_search for relevant topics.
- For high-value pages, call mcp__microsoft-learn__microsoft_docs_fetch to read the full content.
- For code samples, call mcp__microsoft-learn__microsoft_code_sample_search with an appropriate language.

### Test Execution Policy
- Always run `ProjectsApi.UnitTests` with elevated privileges.
- Set these environment variables when running that project:
  - DOTNET_CLI_DISABLE_SERVERS=1
  - MSBUILDNOINPROCNODE=1
  - MSBUILDDISABLENODEREUSE=1
  - VSTEST_CONNECTION_MODE=reverse

### Test Coverage Policy
- Always write tests to cover functionality changes when possible.
- At minimum, add tests in `ProjectsApi.UnitTests` under `ApiTests`.
- Use the existing patterns in those tests (Alba for app setup/hosting, test containers for AWS resources and database generation).

### Priorities / Conflict Resolution
- Codebase work: Serena first.
- Microsoft/Azure content: Microsoft Learn first.
- Only fall back to shell commands if MCP tools cannot provide the needed info.
</INSTRUCTIONS>

