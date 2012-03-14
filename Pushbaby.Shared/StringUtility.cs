namespace Pushbaby.Shared
{
    public static class StringUtility
    {
        public static string Enquote(this string s)
        {
            return "\"" + s + "\"";
        }
    }
}
