namespace FinNavis.Domain.Enums;

/// <summary>
/// The kind of account money sits in. Stored as a string in the database.
/// </summary>
public enum AccountType
{
    Cash,
    Card,
    Savings,
}
