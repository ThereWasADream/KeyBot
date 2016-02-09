using System;

namespace SteamBot
{
    public static class StaticConversions
    {
        //This is no longer used anywhere but I'm keeping this file, because, why not. Good place as any to put static methods I need and have nowhere else to put...
        public static string TrimOffStart(this string s, string str)
        {
            int index = s.IndexOf("]");
            if (index > 0)
                str = s.Substring(index);
            return str;
        }
    }
}

