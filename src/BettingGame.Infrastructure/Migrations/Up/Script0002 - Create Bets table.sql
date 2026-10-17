CREATE TABLE dbo.Bets
(
    BetId        bigint         IDENTITY(1, 1) NOT NULL,
    PlayerId     bigint         NOT NULL,
    Stake        decimal(15, 2) NOT NULL,
    Win          decimal(15, 2) NOT NULL,
    Outcome      varchar(10)    NOT NULL,
    BalanceAfter decimal(15, 2) NOT NULL,
    PlacedAt     datetime2(3)   NOT NULL,

    CONSTRAINT PK_Bets PRIMARY KEY CLUSTERED (BetId),
    CONSTRAINT FK_Bets_Players FOREIGN KEY (PlayerId) REFERENCES dbo.Players (PlayerId),
    CONSTRAINT CK_Bets_Stake CHECK (Stake > 0),
    CONSTRAINT CK_Bets_Win CHECK (Win >= 0),
    CONSTRAINT CK_Bets_Outcome CHECK (Outcome IN ('Lose', 'LowWin', 'HighWin')),
    CONSTRAINT CK_Bets_BalanceAfter CHECK (BalanceAfter >= 0)
);
