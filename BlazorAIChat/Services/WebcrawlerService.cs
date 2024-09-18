using HtmlAgilityPack;

namespace BlazorAIChat.Services
{
    public class WebcrawlerService
    {

        private static readonly HttpClient client = new HttpClient();
        private HashSet<string> visitedUrls = new HashSet<string>();


        public async Task<List<string>> CrawlAsync(string url, int depth)
        {
            //add / to end of url if it doesn't exist
            if (!url.EndsWith("/"))
                url += "/";

            List<string> urls = new List<string>();
            await CrawlAsync(url, depth, urls);
            return urls;
        }

        private async Task CrawlAsync(string url, int depth, List<string> urls)
        {
            if (depth < 0 || visitedUrls.Contains(url) || urls.Contains(url))
            {
                return;
            }

            visitedUrls.Add(url);
            urls.Add(url);

            string html = await GetHtmlAsync(url);
            if (html == null)
            {
                return;
            }

            List<string> links = GetLinksFromHtml(html, url);
            foreach (string link in links)
            {
                await CrawlAsync(link, depth - 1, urls);
            }
        }

        private async Task<string> GetHtmlAsync(string url)
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.Content.Headers.ContentType.MediaType == "text/html")
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching URL {url}: {ex.Message}");
            }
            return null;
        }

        private List<string> GetLinksFromHtml(string html, string baseUrl)
        {
            List<string> links = new List<string>();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            foreach (HtmlNode link in document.DocumentNode.SelectNodes("//a[@href]"))
            {
                string href = link.GetAttributeValue("href", string.Empty);
                if (Uri.TryCreate(new Uri(baseUrl), href, out Uri result))
                {
                    if (result.Host == new Uri(baseUrl).Host)
                    {
                        //Ensure that there isn't /# in the URL
                        if (!result.AbsoluteUri.Contains("#"))
                            links.Add(result.AbsoluteUri);
                    }
                }
            }
            return links;
        }
    }
}
