# Betting Game

A small console app for the Betty technical task: a player wallet with deposits, withdrawals and a simple slot game.

I took it a bit further than the task asks. Players log in with a name, and everything (players, bets, transactions) is stored in SQL Server, so you can quit and pick up where you left off.

## What you need

- .NET 8 SDK or newer. The projects target `net8.0` so they open in Visual Studio 2022, and `RollForward=LatestMajor` lets them run on newer runtimes too.
- A local SQL Server on `localhost,1433` with Windows authentication. I used SQL Server 2022 Developer.

You don't need to create anything in SQL Server. The app creates the `BettingGame` database and its tables the first time it starts.

If your server is somewhere else, change `ConnectionStrings:BettingGame` in `src/BettingGame.ConsoleApp/appsettings.json`, or pass it on the command line:

```bash
dotnet run --project src/BettingGame.ConsoleApp -- --ConnectionStrings:BettingGame="Server=...;Database=BettingGame;..."
```

## Running it

```bash
dotnet run --project src/BettingGame.ConsoleApp
```

The game asks for your name first. A new player starts with $0.00. If the name already exists, you get "Welcome back {name}!" and your saved balance.

Then you can type:

```
deposit <amount>
withdraw <amount>
bet <amount>        (between $1 and $10)
history [count]     (last 10 transactions by default, up to 100)
exit
```

Amounts have to be positive, with at most two decimals. Your balance is printed after every action.

Leaving the game (`exit`, Ctrl+C or end of input) doesn't delete anything. Your player, bets and transaction history stay in the database.

### How the slot game pays

The odds come straight from the task and live in `appsettings.json`:

- 50% of bets lose.
- 40% win up to x2 the bet.
- 10% win between x2 and x10.

The new balance is always `old balance - bet + win`. Wins are rounded down to the cent, with a minimum of $0.01, so a winning spin always pays something.

## Tests

```bash
dotnet test src/BettingGame.sln
```

There are three test projects:

- **Unit:** domain rules, services and the console session, using in-memory fakes.
- **Integration:** the real SQL Server. The run creates a `BettingGame_IntegrationTests` database (plus one for the migration tests) and drops it at the end.
- **Acceptance:** start the actual console app and type into it. They also create and drop their own database.

The test cleanup will only drop databases whose names end in `_IntegrationTests`, `_MigrationTests` or `_AcceptanceTests`, so a wrong connection string can't wipe your real data.

## Project layout

```
src/
  BettingGame.Domain           wallet, amounts, players, the slot game itself
  BettingGame.Application      login, wallet and history services, config validation
  BettingGame.Infrastructure   SQL Server: Dapper repositories and DbUp migrations
  BettingGame.ConsoleApp       Program.cs, the game loop, commands and messages
  Tests/                       Unit, Integration, Acceptance
```

## Database

There are three tables:

- `Players`: name (unique) and current balance.
- `Bets`: one row per spin, with the stake, the win, which outcome hit and the balance afterwards.
- `Transactions`: every money movement (deposit, withdrawal, bet stake, bet win) with the balance after it.

Each action runs in a single SQL transaction, so the balance and the history always match.

Migrations are plain SQL scripts in `Infrastructure/Migrations/Up`, and each has a matching script in `Down`. They run automatically on startup. To roll back the last N scripts (development only, because it drops the tables):

```bash
dotnet run --project src/BettingGame.ConsoleApp -- --Migrations:Downgrade=1
```

## A few design notes

- If you open the game twice with the same name, the second console to save loses. It doesn't overwrite the first one's balance; the game tells you the balance changed somewhere else and stops (exit code 3). Every balance update checks the old value first, which is what catches this.
- Bad configuration (wrong odds, a typo in an outcome name, a broken connection string) is reported in one line before the database is touched, and the app exits with code 1. If the database can't be reached, the exit code is 2.
- The random numbers come from `RandomNumberGenerator` rather than `System.Random`, which felt like the right default for anything casino-like.
- Money is always shown with two decimals (`$10.00`, not `$10` like in the task's example output). Apart from that, the only differences from the task are the extras on top: the name login, the welcome screen listing the actions, and `history`.
