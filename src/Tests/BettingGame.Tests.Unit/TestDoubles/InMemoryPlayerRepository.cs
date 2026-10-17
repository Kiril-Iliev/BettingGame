using BettingGame.Application.Exceptions;
using BettingGame.Application.Interfaces;
using BettingGame.Application.Models;
using BettingGame.Domain.Bets;
using BettingGame.Domain.Players;
using BettingGame.Domain.Transactions;
using BettingGame.Domain.Wallets;

namespace BettingGame.Tests.Unit.TestDoubles;

internal sealed class InMemoryPlayerRepository : IPlayerRepository, ITransactionHistoryReader
{
    private readonly Dictionary<string, (long Id, decimal Balance)> _players = new(StringComparer.OrdinalIgnoreCase);

    public List<BetRound> Bets { get; } = [];

    public List<TransactionEntry> Transactions { get; } = [];

    public decimal StoredBalance(string username) => _players[username].Balance;

    public void Seed(string username, decimal balance) => _players[username] = (_players.Count + 1, balance);

    public void ChangeBalanceElsewhere(string username, decimal balance) => _players[username] = (_players[username].Id, balance);

    public Task<PlayerLogin> GetOrCreateAsync(Username username, CancellationToken cancellationToken)
    {
        var isReturning = _players.TryGetValue(username.Value, out var stored);
        if (!isReturning)
        {
            stored = (_players.Count + 1, 0);
            _players[username.Value] = stored;
        }

        return Task.FromResult(new PlayerLogin(new Player(stored.Id, username, new Wallet(stored.Balance)), isReturning));
    }

    public Task SaveTransactionAsync(decimal expectedBalance, TransactionEntry transaction, CancellationToken cancellationToken)
    {
        UpdateBalance(transaction.PlayerId, expectedBalance, transaction.BalanceAfter);
        Transactions.Add(transaction);
        return Task.CompletedTask;
    }

    public Task SaveBetAsync(decimal expectedBalance, BetRound bet, IReadOnlyCollection<TransactionEntry> transactions, CancellationToken cancellationToken)
    {
        UpdateBalance(bet.PlayerId, expectedBalance, bet.BalanceAfter);
        Bets.Add(bet);
        long betId = Bets.Count;
        Transactions.AddRange(transactions.Select(transaction => transaction with { BetId = betId }));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TransactionHistoryItem>> GetRecentAsync(long playerId, int limit, CancellationToken cancellationToken)
    {
        IReadOnlyList<TransactionHistoryItem> items = Transactions
            .Where(transaction => transaction.PlayerId == playerId)
            .Reverse()
            .Take(limit)
            .Select(transaction => new TransactionHistoryItem(transaction.Type, transaction.Amount.Value, transaction.BalanceAfter, transaction.CreatedAt))
            .ToList();

        return Task.FromResult(items);
    }

    private void UpdateBalance(long playerId, decimal expectedBalance, decimal balance)
    {
        var (username, stored) = _players.Single(pair => pair.Value.Id == playerId);
        if (stored.Balance != expectedBalance)
        {
            throw new BalanceChangedElsewhereException(playerId);
        }

        _players[username] = (playerId, balance);
    }
}
