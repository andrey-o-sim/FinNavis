namespace FinNavis.Domain.Enums;

// No "record with no category" target: a transaction always has a category.
public enum MappingRuleTarget
{
    Category = 1,

    TransferBetweenOwnAccounts = 2,
}
