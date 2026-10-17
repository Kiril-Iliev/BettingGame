CREATE TABLE dbo.Players
(
    PlayerId  bigint         IDENTITY(1, 1) NOT NULL,
    Username  nvarchar(50)   NOT NULL,
    Balance   decimal(15, 2) NOT NULL CONSTRAINT DF_Players_Balance DEFAULT (0),
    CreatedAt datetime2(3)   NOT NULL,
    UpdatedAt datetime2(3)   NOT NULL,

    CONSTRAINT PK_Players PRIMARY KEY CLUSTERED (PlayerId),
    CONSTRAINT UQ_Players_Username UNIQUE (Username),
    CONSTRAINT CK_Players_Balance CHECK (Balance >= 0)
);
