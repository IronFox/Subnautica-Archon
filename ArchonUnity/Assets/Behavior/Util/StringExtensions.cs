using System.Text;

namespace Behavior.Util
{
    public static class StringExtensions
    {
        public static string Repeat(this string s, int count)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < count; i++)
                b.Append(s);
            return b.ToString();
        }
    }
}