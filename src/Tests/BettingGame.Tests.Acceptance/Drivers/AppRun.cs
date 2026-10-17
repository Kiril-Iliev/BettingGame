namespace BettingGame.Tests.Acceptance.Drivers;

internal sealed record AppRun(int ExitCode, string Output, string Error)
{
    public const string Prompt = "Please, submit action:";

    public string Welcome { get; } = Output.Split(Prompt)[0].Trim();

    public IReadOnlyList<string> Responses { get; } = Output
        .Split(Prompt)
        .Skip(1)
        .Select(response => response.Trim())
        .ToList();
}
