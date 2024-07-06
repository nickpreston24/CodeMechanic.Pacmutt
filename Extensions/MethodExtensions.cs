public static class MethodExtensions
{
    /// <summary>
    /// Play: I wanted to see if I could attach a debug to a method instance.  
    /// </summary>
    /// <param name="mtd"></param>
    /// <returns></returns>
    public static async Task<object> AttachDebug(this object mtd)
    {
        Console.WriteLine("blah");
        return null;
    }
}