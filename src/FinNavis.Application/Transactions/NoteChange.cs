namespace FinNavis.Application.Transactions;

/// <summary>
/// What an edit says about the note: nothing at all, or a value to write — which may be
/// <c>null</c> to clear it.
/// </summary>
/// <remarks>
/// A plain <c>string?</c> cannot carry this. The contract lets the owner send
/// <c>"note": null</c> to clear the note and leave the property out to keep it, and those two mean
/// different things — every other field on an edit is unambiguous, so only the note needs this.
/// </remarks>
public readonly record struct NoteChange(bool IsSet, string? Value)
{
    /// <summary>The owner did not mention the note, so the entry keeps the one it has.</summary>
    public static NoteChange Unchanged => default;

    /// <summary>The owner sent a note, or sent <c>null</c> to leave the entry without one.</summary>
    public static NoteChange To(string? note) => new(IsSet: true, note);

    /// <summary>The note to write, given the entry's current one.</summary>
    public string? Or(string? current) => IsSet ? Value : current;
}
