using FinNavis.Domain.Enums;

namespace FinNavis.Application.Common;

internal static class CategoryKindText
{
    public static string AsLedgerSide(this CategoryKind kind) => kind.ToString().ToLowerInvariant();
}
