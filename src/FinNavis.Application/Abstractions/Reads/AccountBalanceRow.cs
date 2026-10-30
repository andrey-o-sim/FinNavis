using FinNavis.Domain.ValueObjects;

namespace FinNavis.Application.Abstractions.Reads;

public sealed record AccountBalanceRow(Guid AccountId, string Name, Money Balance);
