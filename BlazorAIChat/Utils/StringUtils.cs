using System.Text.RegularExpressions;
namespace BlazorAIChat.Utils
{
    public static class StringUtils
    {
        public static List<string> GetURLsFromString(string content)
        {
            List<string> urls = new List<string>();
            string pattern = @"http[s]?://\S+/?";

            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(content);

            foreach (Match match in matches)
            {
                string url = match.Value;
                if (char.IsPunctuation(url[^1]))
                {
                    url = url.TrimEnd(url[^1]);
                }
                urls.Add(url);
            }

            return urls;
        }

        public static string RemoveURLsFromString(string content)
        {
            string pattern = @"http[s]?://\S+/?";
            return Regex.Replace(content, pattern, string.Empty);
        }
    }
}
