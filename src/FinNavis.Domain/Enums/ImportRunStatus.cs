namespace FinNavis.Domain.Enums;

// There is no Failed value: a load that fails writes no run at all.
public enum ImportRunStatus
{
    Completed = 1,

    Undone = 2,
}
