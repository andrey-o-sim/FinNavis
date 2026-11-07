using FinNavis.Application.Abstractions.Reads;

namespace FinNavis.Application.Import.Pipeline;

// The bank's text and the owner's note almost never agree, so text plays no part in the match.
// A movement stands for one purchase, so once it covers a row it cannot cover another one: a
// second real purchase of the same thing would otherwise vanish with nothing shown to the owner.
internal sealed class DuplicateMatcher(
    IReadOnlyList<CandidateTransactionRow> candidates,
    int matchWindowDays)
{
    private readonly HashSet<Guid> _claimedIds = [];

    public bool TryClaim(RecordAsTransaction record)
    {
        CandidateTransactionRow? closest = candidates
            .Where(candidate => !_claimedIds.Contains(candidate.Id)
                && candidate.AccountId == record.AccountId
                && candidate.Amount == record.Amount
                && DaysApart(candidate, record) <= matchWindowDays)
            .OrderBy(candidate => DaysApart(candidate, record))
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefault();

        return closest is not null && _claimedIds.Add(closest.Id);
    }

    private static int DaysApart(CandidateTransactionRow candidate, RecordAsTransaction record) =>
        Math.Abs(candidate.OccurredOn.DayNumber - record.OccurredOn.DayNumber);
}
