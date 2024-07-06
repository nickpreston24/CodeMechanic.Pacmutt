public static class HashExtensions
{
    public static HashSet<string> ToHashSet<T>(this List<T> outer,
        Func<T, string> selector
    )
    {
        var set = outer.Select(selector).ToHashSet();
        return set;
    }
}