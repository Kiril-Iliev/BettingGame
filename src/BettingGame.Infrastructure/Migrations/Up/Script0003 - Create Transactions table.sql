CREATE TABLE dbo.Transactions
(
    TransactionId bigint         IDENTITY(1, 1) NOT NULL,
    PlayerId      bigint         NOT NULL,
    Type          varchar(20)    NOT NULL,
    Amount        decimal(15, 2) NOT NULL,
    BalanceAfter  decimal(15, 2) NOT NULL,
    BetId         bigint         NULL,
    CreatedAt     datetime2(3)   NOT NULL,

    CONSTRAINT PK_Transactions PRIMARY KEY CLUSTERED (TransactionId),
    CONSTRAINT FK_Transactions_Players FOREIGN KEY (PlayerId) REFERENCES dbo.Players (PlayerId),
    CONSTRAINT FK_Transactions_Bets FOREIGN KEY (BetId) REFERENCES dbo.Bets (BetId),
    CONSTRAINT CK_Transactions_Type CHECK (Type IN ('Deposit', 'Withdrawal', 'BetStake', 'BetWin')),
    CONSTRAINT CK_Transactions_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Transactions_BalanceAfter CHECK (BalanceAfter >= 0),
    CONSTRAINT CK_Transactions_BetId CHECK (
        (Type IN ('BetStake', 'BetWin') AND BetId IS NOT NULL)
        OR (Type IN ('Deposit', 'Withdrawal') AND BetId IS NULL))
);

CREATE NONCLUSTERED INDEX IX_Transactions_PlayerId_TransactionId
    ON dbo.Transactions (PlayerId, TransactionId DESC)
    INCLUDE (Type, Amount, BalanceAfter, CreatedAt);
