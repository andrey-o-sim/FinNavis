namespace FinNavis.Application.Transactions;

public readonly record struct NoteChange(bool IsSet, string? Value)
{
    public static NoteChange Unchanged => default;

    public static NoteChange To(string? note) => new(IsSet: true, note);

    public string? Or(string? current) => IsSet ? Value : current;
}
