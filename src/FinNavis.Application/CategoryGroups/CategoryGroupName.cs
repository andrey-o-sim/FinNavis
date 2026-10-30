using FinNavis.Application.Reporting;

namespace FinNavis.Application.CategoryGroups;

internal static class CategoryGroupName
{
    // The breakdown produces one bucket under this name for every category that belongs to no
    // group. A row carrying the same name would put two entries with the same name side by side
    // in the breakdown, one of them with no id. Case is ignored because the clash the owner sees
    // is in the reading, not in an index.
    public static void RequireNotReserved(string name)
    {
        if (string.Equals(name, DefaultGroup.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw CategoryGroupErrors.NameReserved();
        }
    }
}
