# Betting Game Ultimate – Player Wallet with persistence

Console app that simulates a player wallet with deposits, withdrawals and a simple slot game.
Ultimate adds named players and a SQL Server database holding players, bets and the full transaction history.

Compiled against `net8.0` so it opens and builds in Visual Studio 2022, with `RollForward=LatestMajor` so it runs on the newest installed runtime (.NET 10 and higher).

## Prerequisites

A local SQL Server reachable at `localhost,1433` with Windows authentication (e.g. SQL Server 2022 Developer). The `BettingGame` database and its tables are created automatically on first start (DbUp), and the tests create their own `BettingGame_*Tests` databases on the same server only while they run and drop them afterwards. As a safety guard the tests refuse to drop any database whose name doesn't end with `_IntegrationTests`, `_MigrationTests` or `_AcceptanceTests`, so a misconfigured connection string can't delete `BettingGame` or another service's database. To use another server, override `ConnectionStrings:BettingGame` in `appsettings.json` or on the command line (`--ConnectionStrings:BettingGame=...`).

## Run

```bash
dotnet run --project src/BettingGame.ConsoleApp
```

The game asks for your name. A new name is welcomed with "Welcome to the Betty slot game, {name}!" and starts at $0.00; a returning name sees "Welcome back {name}! Your current balance is: {balance}" and continues with its stored balance. Ending the game (`exit`, end of input or Ctrl+C, which also prints the farewell) never deletes anything: players, bets and transactions stay in the database.

Actions: `deposit <amount>`, `withdraw <amount>`, `bet <amount>`, `history [count]`, `exit`. Amounts must be positive with at most 2 decimals.

## Database migrations

Every start applies pending scripts from `Migrations/Up` (DbUp, journal table `dbo.SchemaVersions`).
To roll back, revert the last N applied scripts with their matching `Migrations/Down` script, newest first:

```bash
dotnet run --project src/BettingGame.ConsoleApp -- --Migrations:Downgrade=1
```

Each revert and its journal delete run in one SQL transaction. The next normal start re-applies the reverted scripts.

> **Warning:** the Down scripts drop tables, so a downgrade deletes the data in them (e.g. reverting `Script0001` removes every player). Use it only on development databases.

The schema is three plain scripts – `Script0001 - Create Players table`, `Script0002 - Create Bets table`, `Script0003 - Create Transactions table` – each a single `CREATE TABLE` with its keys, constraints and indexes, and a Down script that is a single `DROP TABLE`. No triggers or other logic in migrations.

**Adding a migration:** add `Up/ScriptNNNN - Description.sql` and a `Down/` script with the same name (a test fails if a Down script is missing). Keep scripts declarative and free of `GO`.

## Test

```bash
dotnet test src/BettingGame.sln
```

- `Tests/BettingGame.Tests.Unit` – domain, application and console layer in isolation, with in-memory repositories (NUnit, FluentAssertions, Moq, FakeTimeProvider).
- `Tests/BettingGame.Tests.Integration` – the real database: `BettingGame_IntegrationTests` is created from the Up scripts at the start of the run and dropped at the end; tables are cleared before each test. Covers Dapper repositories, atomic rollback, the stale-balance check, concurrent first logins, returning-player detection, constraints, a full wallet session through the application services, and Up/Down migrations on a separate `BettingGame_MigrationTests` database (also dropped at the end).
- `Tests/BettingGame.Tests.Acceptance` – black-box: lets the game create `BettingGame_AcceptanceTests` at the start of the run (dropped again at the end), then starts the real console process per test and drives stdin. Each test plays as a unique player, so tests run in parallel. Game odds are pinned through command-line configuration to make win/loss scenarios deterministic.

## Structure

```
src/
  BettingGame.Domain/            business rules, no dependencies
    Interfaces/                  IGame, IRandomNumberProvider
    Players/                     Player, Username
    Wallets/                     Wallet, Amount
    Games/                       SlotGame, GameRules, OutcomeBand, BetOutcome, SpinResult, CryptoRandomNumberProvider
    Bets/                        BetRound
    Transactions/                TransactionEntry, TransactionType
    Exceptions/                  DomainException + one file per domain error
  BettingGame.Application/       use cases
    Interfaces/                  IPlayerRepository (all writes), ITransactionHistoryReader
    Exceptions/                  BalanceChangedElsewhereException
    Services/                    LoginService, WalletService, HistoryService (the player is passed explicitly)
    Models/                      PlayerLogin, TransactionResult, BetResult, TransactionHistoryItem
    Validation/                  GameRulesConfigurationChecker, GameRulesValidator, GameRulesErrorMessages
    DependencyInjection/         AddBettingGame
  BettingGame.Infrastructure/    the only project that knows about SQL
    Configuration/               DatabaseSettings (ConnectionStrings:BettingGame)
    DataAccess/                  SqlConnectionFactory, Entities, Mapping, Repositories (Dapper)
    Migrations/Up/               upgrade scripts (ScriptNNNN - Description.sql)
    Migrations/Down/             revert scripts, same file names
    Migrations/DatabaseMigrator  Migrate() and Downgrade(steps)
    DependencyInjection/         AddInfrastructure
  BettingGame.ConsoleApp/        presentation + composition root
    Commands/                    Deposit, Withdraw, Bet, History, Exit
    Messages/ Errors/            player-facing text catalogs
    Session/                     LoginPrompt, GameSession, WelcomeMessage
    Migrations/                  DowngradeCommand (--Migrations:Downgrade=N)
  Tests/
    BettingGame.Tests.Unit / .Integration / .Acceptance
```

## Database `BettingGame`

| Table | Purpose |
|---|---|
| `dbo.Players` | `PlayerId bigint IDENTITY`, `Username nvarchar(50) UNIQUE`, `Balance decimal(15,2) CHECK >= 0`, `CreatedAt`, `UpdatedAt` |
| `dbo.Bets` | one row per round: `Stake`, `Win`, `Outcome` (`Lose`/`LowWin`/`HighWin`), `BalanceAfter`, `PlacedAt`; FK to Players |
| `dbo.Transactions` | transaction history (the app only inserts): `Type` (`Deposit`/`Withdrawal`/`BetStake`/`BetWin`), positive `Amount`, `BalanceAfter`, `BetId` for bet rows, `CreatedAt`; FKs to Players and Bets; covering index on `(PlayerId, TransactionId DESC)`; history is ordered by `TransactionId` (true insert order, independent of the app clock). |

## Design decisions

- **Common .NET service conventions, simplified** – Dapper with inline SQL, DbUp with embedded `ScriptNNNN - Description.sql` scripts run at startup and same-named revert scripts (`Up` / `Down`), `*Entity` row classes with `ToDomain()`/`ToEntity()` mappers, `bigint` identities and `decimal(15,2)` money. No events, outbox or external settings store; downgrade is a small in-repo runner.
- **One database, separate tables** – game state (`Players`, `Bets`) and the money audit trail (`Transactions`) live side by side, so foreign keys protect the links between them.
- **One SQL transaction per action** – `IPlayerRepository.SaveTransactionAsync` (deposit/withdrawal) and `SaveBetAsync` (balance update + `Bets` insert + its `BetStake`/`BetWin` rows, linked by `BetId`) commit or roll back as a whole, so balance and history can never drift apart.
- **Optimistic balance check** – every balance update is `WHERE Balance = @ExpectedBalance`. If another console changed the balance in the meantime, nothing is written, the player sees "Your balance was changed in another console…" and the session ends (exit code 3), instead of silently overwriting.
- **Race-safe first login** – `GetOrCreateAsync` is a single `INSERT … WHERE NOT EXISTS (… WITH (UPDLOCK, HOLDLOCK))` followed by the read, so simultaneous first logins with the same name create exactly one player.
- **Read/write split** – writes go through `IPlayerRepository`; `ITransactionHistoryReader` returns a slim `TransactionHistoryItem` projection with a mandatory row limit (`history` default 10, max 100).
- **Nothing is ever deleted** – the application only inserts players, bets and transactions and only updates a player's balance; there is no delete path in the code. History keeps every movement, and the index keeps "latest N" fast regardless of table size.
- **No ambient state** – after login the `Player` is passed explicitly to `WalletService`, `HistoryService` and every command; services are plain classes without one-implementation interfaces.
- **Outcome band stored** – each bet records which band hit, so the 50/40/10 split can be verified with SQL. `GameRulesValidator` makes sure the configured bands make that column trustworthy: `Lose` pays 0, every winning band pays more than 0, and no outcome appears twice (a band missing its `Outcome` key would otherwise default to `Lose`).
- **Configuration fails fast with one exact message** – before binding, `GameRulesConfigurationChecker` checks every raw `Game` value (e.g. `'Loose' at Game:Outcomes:0:Outcome is not a valid outcome`, `'abc' at Game:MinBet is not a number`), and `DatabaseSettings` checks the connection string (missing, malformed, unknown keyword, no Server or Database). Any problem prints `Invalid configuration: …` and exits with code 1 – no stack traces.
- **Randomness** – `IRandomNumberProvider` abstraction; production uses `CryptoRandomNumberProvider` in `Domain/Games` next to `SlotGame` (it is a plain .NET `RandomNumberGenerator` call – crypto-secure, not predictable like `System.Random` – with no I/O, so it belongs with the game, not in Infrastructure), tests use a fixed sequence. Infrastructure is database only.
- **Constraints in the database too** – CHECK constraints and FKs mirror domain rules (non-negative balance, positive amounts, valid types, BetId required for bet rows, known player).
- **Amount / Username value objects** – `readonly record struct`, validated once.
- **Balance limit** – capped at `decimal(15,2)` max (9,999,999,999,999.99) so the domain never produces a value the DB rejects.
- **Exit codes** – 0 success, 1 invalid configuration (game settings, connection string or `--Migrations:Downgrade` value), 2 database unavailable, 3 balance changed in another console.
- **No session lock** – two consoles may log in as the same name; the optimistic check stops the second writer instead of letting it overwrite the first. Verified with two overlapping consoles depositing as the same player: one finished, the other stopped with exit code 3, and the stored balance equalled the sum of the recorded transactions (no lost update). Simultaneous first logins with one name create a single player.
- **Startup order** – configuration (game rules, connection string) is read and validated before the database is touched, so a config mistake is always reported as `Invalid configuration` (exit 1). Errors stay visible: "Press any key to exit." is shown whenever the game runs in a real console.
- **Timestamps** – sent to SQL Server as `datetime2` (Dapper type map) to match the `datetime2(3)` columns.

## Differences from the PDF and its example output

- The game first asks for a name. A **new** name starts at $0.00 as the PDF requires; a **returning** name is greeted with "Welcome back {name}!" and continues with its saved balance (persistence is the point of Ultimate).
- A welcome screen lists the actions and bet limits before the first prompt.
- Extra action: `history [count]` shows the latest transactions with the balance after each.
- The game normally ends only when the player leaves (`exit`), as the PDF says. One extra case: if the same name changes the balance in another console, the session ends with an explanation (exit code 3) instead of overwriting that balance.
- Win bands are read as: 50% lose (win 0), 40% win more than 0 and up to x2, 10% win between x2 and x10. Wins are rounded down to the cent with a minimum of $0.01, so every bet in a winning band really pays.
- All money is printed with two decimals (`$10.00` instead of `$10`) for consistency.
- Rejected actions print the reason followed by the unchanged balance.
