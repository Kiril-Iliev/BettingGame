using BettingGame.Domain.Exceptions;

namespace BettingGame.Domain.Players;

public readonly record struct Username
{
    public const int MaxLength = 50;

    private Username(string value) => Value = value;

    public string Value { get; }

    public static Username Of(string? value)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidUsernameException(DomainErrorMessages.UsernameRequired);
        }

        if (trimmed.Length > MaxLength)
        {
            throw new InvalidUsernameException(DomainErrorMessages.UsernameTooLong(MaxLength));
        }

        return new Username(trimmed);
    }

    public override string ToString() => Value;
}
