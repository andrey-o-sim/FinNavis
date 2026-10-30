using FinNavis.Application.Abstractions;

namespace FinNavis.Application.Transfers;

/// <summary>
/// Commits a transfer and turns a failed commit into the refusal AC-06 words.
/// </summary>
/// <remarks>
/// Recording and editing both need it, and both have to say the same sentence, so the wrapping
/// lives in one place.
/// <para>
/// <b>Why wrap at all.</b> A transfer is one row, so a commit that did not go through changed
/// neither account — but the owner would otherwise read a technical failure instead of the
/// promise AC-06 makes. The cause is kept as the inner exception, so nothing is swallowed.
/// </para>
/// <para>
/// <b>A cancellation is not a failed recording.</b> The owner walked away; nothing refused them,
/// so it travels on untouched.
/// </para>
/// </remarks>
internal static class TransferCommit
{
    public static async Task RunAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            throw TransferErrors.NotRecorded(failure);
        }
    }
}
