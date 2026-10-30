using FinNavis.Application.Abstractions.Reads;
using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Accounts;

public sealed record AccountBalances(IReadOnlyList<AccountBalanceRow> Accounts, Money Total);
