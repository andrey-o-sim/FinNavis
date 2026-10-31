namespace FinNavis.Domain.Enums;

// An unknown account is not a reason here: it refuses the whole load instead of one row.
public enum SetAsideReason
{
    NoRuleCovers = 1,

    TransferBetweenOwnAccounts = 2,

    ForeignCurrency = 3,
}
