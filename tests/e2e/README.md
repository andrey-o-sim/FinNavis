# End-to-end tests (placeholder)

Nothing here yet. This folder will hold Playwright tests written in TypeScript.

## Planned

- Playwright with TypeScript
- Runs against the API and the web client started by Docker Compose
- Covers the main user flows: create account, add income, add expense, transfer between accounts

## Why this folder is outside the .NET solution

These tests are a Node project, not a .NET project. They are not part of `FinNavis.slnx`,
so `dotnet test` does not run them. They will get their own npm script.
