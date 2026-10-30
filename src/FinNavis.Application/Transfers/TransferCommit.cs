using FinNavis.Application.Abstractions;

namespace FinNavis.Application.Transfers;

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
