namespace FinNavis.Application.Abstractions.Reads;

public sealed record CandidateSnapshot(
    IReadOnlyList<CandidateTransactionRow> Transactions,
    IReadOnlyList<CandidateTransferRow> Transfers);
