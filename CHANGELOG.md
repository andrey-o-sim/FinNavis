# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-11-21

### Changed

- The CI workflow `.github/workflows/ci.yml` skips build and test on a pull request opened by a bot, such as the Release-PR. The Release-PR changes only `CHANGELOG.md`, and the push to `master` after its merge still runs the job.

## [0.3.0] - 2026-11-21

### Changed

- The Claude code review workflow `.github/workflows/claude-code-review.yml` skips a pull request opened by a bot, such as the Release-PR. Before, the review check failed on every Release-PR.
- The release workflow error for a merge with one parent now tells you to use `Merge pull request`, not a squash or a rebase. It named a button that the repository does not show.

## [0.2.0] - 2026-11-21

### Added

- A merged pull request now runs the release workflow `.github/workflows/release.yml`. When the merge does not raise the newest version in `CHANGELOG.md`, it opens a Release-PR from `docs/release-<x.y.z>` with the new changelog entries for the owner to review.
- When a merge raises the newest version in `CHANGELOG.md`, the workflow tags the merge commit as `v<x.y.z>` and publishes a GitHub Release with release notes. The release also lists the merged pull requests, grouped by the type of the pull request title.

## [0.1.0] - 2026-11-08

### Added

- `POST /api/v1/mapping-profiles/inspect` reads an uploaded CSV file and answers its `shape_key`, the categories the file uses, and the `mapping_profile_id` already confirmed for that shape, if there is one.
- `POST /api/v1/mapping-profiles` stores the rules for one file shape. Each rule sends a file category to a ledger category or marks it as a transfer between own accounts. It answers `201` for a new shape and `200` when the rules of a known shape are confirmed again.
- `POST /api/v1/import-runs/statement` loads a bank statement (`file`, `account_id`) onto one account by the confirmed rules. A row that is already in the ledger within one day is counted as already present and is not loaded twice.
- `POST /api/v1/import-runs/history-migration` moves the old history across from another tracker's export. Accounts and categories that do not exist yet are created by name. A row is counted as already present only on the same date.
- A finished load answers `200` with its counts and every set-aside row with its reason: no rule covers it, it is a transfer between own accounts, or it is in a foreign currency.
- A load is all or nothing. If it stops part-way, nothing reaches the ledger, every balance stays the same, and the request is refused with `500 Import.NotRecorded`.
- `GET /api/v1/import-runs/{import_run_id}` shows what a load did: its counts and one page of its set-aside rows. The rows are paged with an opaque cursor (`after`, `before`, `limit`).
- `POST /api/v1/import-runs/{import_run_id}/undo` undoes a whole load. It removes every movement the load recorded, and the accounts and categories it created when nothing else uses them.
- Import refusals in the flat error envelope `{code, message, details?}`:
  - `400`: `Import.MissingRequiredColumn`, `Import.FileNotReadable`, `Import.InvalidPaging`, `MappingProfile.InvalidRule`, `MappingProfile.ShapeKeyInvalid`.
  - `404`: `Import.NotFound`, `Account.NotFound`, `Category.NotFound`.
  - `409`: `Import.MappingProfileNotConfirmed`, `Import.MovementsChangedByHand`, `Import.AlreadyUndone`, `MappingProfile.DuplicateSourceCategory`.
- `MappingProfile.InvalidRule` names the refused rule in `details` (`source_category`, `target`).
- The API reads JSON strictly. A body that leaves out a required field, an enum sent as a number, and a number sent as a string are refused with `400`.
- The `AddCsvImport` migration adds eight tables: `mapping_profiles`, `mapping_profile_rules`, `import_runs`, `import_transaction_links`, `import_transfer_links`, `import_created_account_links`, `import_created_category_links` and `import_set_aside_rows`. Run `./build.sh db-update` to apply it. Existing tables keep their columns. When an account, a category, a transaction or a transfer is deleted, its import link row is deleted with it. When an account is deleted, `import_runs.account_id` becomes `NULL`.
