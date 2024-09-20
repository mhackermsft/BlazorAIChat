using BlazorAIChat.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;

namespace BlazorAIChat.Services
{

    public enum WebCrawlerServiceStatusEnum
    {
        Idle,
        Crawling
    }

    public enum CrawlerStatusEnum
    {
        Queued,
        Crawling,
        Embedding,
        Completed
    }

    public class WebCrawlerService
    {

        private static readonly HttpClient client = new HttpClient();
        private HashSet<string> visitedUrls = new HashSet<string>();
        private Queue<Dictionary<string,string>> newURLQueue = new Queue<Dictionary<string, string>>();
        private readonly AIChatDBContext dbContext;
        private bool isCrawling = false;
        private System.Timers.Timer crawlerTimer = new(5000);

        public int MaxCrawlDepth { get; set; } = 2;
        public bool IsCrawling { get { return isCrawling; } }

        public event EventHandler<WebCrawlerServiceStatusEnum>? OnCrawlStateChanged;

        public WebCrawlerService(IOptions<AppSettings> settings)
        {
            dbContext = new AIChatDBContext(settings);
            crawlerTimer.Elapsed += CrawlerTimer_Elapsed;
            crawlerTimer.AutoReset = true;
            crawlerTimer.Enabled = true;
        }

        private void CrawlerTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (!isCrawling)
            {
                Dictionary<string, string>? nextURL = GetNextURLFromQueue();
                if (nextURL != null)
                {
                    Task.Run(async () =>
                    {
                        isCrawling = true;
                        OnCrawlStateChanged?.Invoke(this, WebCrawlerServiceStatusEnum.Crawling);

                        //Add URL as a parent URL to the database.
                        Guid? urlId = AddURLtoDatabase(nextURL["url"], nextURL["userId"], null);
                        if (urlId == Guid.Empty)
                        {
                            //URL already exists in database for the user, so we just jump out.
                            isCrawling = false;
                            OnCrawlStateChanged?.Invoke(this, WebCrawlerServiceStatusEnum.Idle);
                            return;
                        }

                        List<string> urls = await CrawlAsync(nextURL["url"], MaxCrawlDepth);
                        foreach (string url in urls)
                        {
                            //Add child URLs to the database
                            AddURLtoDatabase(url, nextURL["userId"], urlId);
                        }

                        isCrawling = false;
                        OnCrawlStateChanged?.Invoke(this, WebCrawlerServiceStatusEnum.Idle);
                    });
                }
            }
        }

        public void AddURLToQueue(string url, string userId)
        {
            //add / to end of url if it doesn't exist
            if (!url.EndsWith("/"))
                url += "/";

            Dictionary<string, string> newURL = new Dictionary<string, string>();
            newURL.Add("url", url);
            newURL.Add("userId", userId);
            newURLQueue.Enqueue(newURL);
        }

        public void RemoveURLFromQueue(string url)
        {
            newURLQueue = new Queue<Dictionary<string, string>>(newURLQueue.Where(x => x["url"] != url));
        }


        public Guid? AddURLtoDatabase(string url, string userId, Guid? parentId)
        {
            //ensure that the URL is not already in the database for the user
            if (dbContext.CrawlerStatuses.Any(x => x.URL == url && x.UserId==userId))
            {
                return Guid.Empty;
            }

            CrawlerStatus newStatus = new CrawlerStatus
            {
                Id = Guid.NewGuid(),
                ParentId = parentId,
                URL = url,
                UserId = userId,
                LastStatus = CrawlerStatusEnum.Queued,
                LastUpdate = DateTime.Now,
                IsActive = true
            };
            dbContext.CrawlerStatuses.Add(newStatus);
            dbContext.SaveChanges();
            return newStatus.Id;
        }


        private Dictionary<string, string>? GetNextURLFromQueue()
        {
            if (newURLQueue.Count > 0)
            {
                return newURLQueue.Dequeue();
            }
            return null;
        }


        private async Task<List<string>> CrawlAsync(string url, int depth)
        {
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
