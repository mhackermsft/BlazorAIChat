namespace BlazorAIChat.Utils;

using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

public static class MarkdownToHtmlConverter
{
    public static string ConvertTableToHtml(string markdown)
    {
        // Convert the table header to HTML
        string headerPattern = @"^\|(.+?)\|$";
        string headerReplacement = "<tr>$1</tr>";
        string html = Regex.Replace(markdown, headerPattern, match =>
        {
            string[] columns = match.Groups[1].Value.Split('|');
            return "<tr>" + string.Join("", columns.Select(col => $"<th>{col.Trim()}</th>")) + "</tr>";
        }, RegexOptions.Multiline);

        // Convert the table rows to HTML
        string rowPattern = @"^\|(.+?)\|$";
        string rowReplacement = "<tr>$1</tr>";
        html = Regex.Replace(html, rowPattern, match =>
        {
            string[] columns = match.Groups[1].Value.Split('|');
            return "<tr>" + string.Join("", columns.Select(col => $"<td>{col.Trim()}</td>")) + "</tr>";
        }, RegexOptions.Multiline);

        // Wrap the entire table with <table> tags
        string tablePattern = @"(<tr>.*?</tr>)";
        string tableReplacement = "<table>$1</table>";
        html = Regex.Replace(html, tablePattern, tableReplacement, RegexOptions.Singleline);

        return html;
    }
}
