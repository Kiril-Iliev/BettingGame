# BettingGame Ultimate – instructions

Console player wallet + slot game for the Betty technical task, extended with SQL Server persistence. `README.md` describes behaviour and design; this file is the working rules for changing the code.

## Ground rules

- **Stick to the PDF.** $0 start for a new player, `deposit` / `withdraw` / `bet` / `exit`, bets $1–$10, 50% lose / 40% win up to x2 / 10% win x2–x10, new balance = old − bet + win, amounts always positive, balance shown after every operation. Ultimate adds only: name login ("Welcome back {name}!" for returning players), persistence and `history`.
- **Keep it simple (KISS).** No new layers, patterns, interfaces with a single implementation, or features that the PDF or the user did not ask for. Prefer the smallest change that works.
- **Nothing is deleted at runtime.** The app only inserts players, bets and transactions and only updates a player's balance.
- **Migrations are plain.** `Infrastructure/Migrations/Up/ScriptNNNN - Description.sql` = declarative `CREATE`/`ALTER` only (no triggers, `THROW`, procedures or `GO`); each has a same-named `Down/` script. Never edit a script that may already be applied – add a new one.
- **Every text the player sees** lives in `ConsoleApp/Messages/PlayerMessages.cs` or `ConsoleApp/Errors/ErrorMessages.cs`; domain/config errors in their `*ErrorMessages` classes.
- **Code style:** one type per file, namespaces follow folders, `sealed` by default, no comments unless asked, analyzers at `latest-recommended` with warnings as errors (the build must stay at 0 warnings).

## Layout

```
src/BettingGame.Domain          rules: Wallet, Amount, Username, Player, SlotGame + crypto RNG, exceptions
src/BettingGame.Application     LoginService, WalletService, HistoryService, config validation
src/BettingGame.Infrastructure  database only: Dapper repositories, DbUp migrations
src/BettingGame.ConsoleApp      Program (composition root), GameSession, commands, messages
src/Tests/...Unit | ...Integration | ...Acceptance
```

The logged-in `Player` is passed explicitly to services and commands (no ambient/current-player state).

## Run and test

Requires SQL Server at `localhost,1433` with Windows authentication (connection string `ConnectionStrings:BettingGame` in `src/BettingGame.ConsoleApp/appsettings.json`).

```bash
dotnet run --project src/BettingGame.ConsoleApp
dotnet test src/BettingGame.sln
```

- Tests create their own `BettingGame_*Tests` databases and drop them at the end; a guard refuses to drop any other database.
- All three test projects must stay green; add a test with every behaviour change (unit first, acceptance only for end-to-end wiring).
- Downgrade (dev only, drops tables and their data): `dotnet run --project src/BettingGame.ConsoleApp -- --Migrations:Downgrade=1`.

## Exit codes

0 success · 1 invalid configuration · 2 database unavailable · 3 balance changed in another console.
