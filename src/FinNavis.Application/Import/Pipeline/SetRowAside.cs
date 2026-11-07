using FinNavis.Domain.Enums;

namespace FinNavis.Application.Import.Pipeline;

public sealed record SetRowAside(SetAsideReason Reason, string? SourceCategory) : RowDecision;
