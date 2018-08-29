using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net;
using WebCrawler.Objects;
using System.IO;

namespace WebCrawler
{
    class Program
    {
        static string imgXPath = "//img";
        static string aXPath = "//a";
        static HttpClient httpClient = new HttpClient();
        static List<ResourceNode> visitedImg = new List<ResourceNode>();
        static List<ResourceNode> visitedPages = new List<ResourceNode>();
        static List<ResourceNode> heldJavascript = new List<ResourceNode>();
        static List<ResourceNode> heldEmail = new List<ResourceNode>();

        static void Main(string[] args)
        {
            string rootUrl = "https://www.alarm.com";
            List<string> allowedPrefix = new List<string>() { rootUrl };
            httpClient.BaseAddress = new Uri(rootUrl);

            BFSCrawler(rootUrl, 3, SecurityProtocolType.Tls12, allowedPrefix);

            var csv = new StringBuilder();

            foreach (ResourceNode node in visitedImg)
            {
                csv.Append($"{node.ParentPage},{node.Tag},{node.DisplayedUrl},{node.LandedUrl},{node.StatusCode}{Environment.NewLine}");
            }

            File.WriteAllText(@"D:\C#Projects\WebCrawler\visitedImg.csv", csv.ToString());

            csv.Clear();

            foreach(ResourceNode node in visitedPages)
            {
                csv.Append($"{node.ParentPage},{node.Tag},{node.DisplayedUrl},{node.LandedUrl},{node.StatusCode}{Environment.NewLine}");
            }

            File.WriteAllText(@"D:\C#Projects\WebCrawler\visitedPages.csv", csv.ToString());

            //Console.ReadLine();
        }

        private static void BFSCrawler(string rootUrl, int maxDepth, SecurityProtocolType securityProtocolType, List<string> allowedPrefix)
        {
            ServicePointManager.SecurityProtocol = securityProtocolType;

            Queue<ResourceNode> queue = new Queue<ResourceNode>();

            queue.Enqueue(new ResourceNode()
            {
                ParentPage = null,
                Tag = "root",
                DisplayedUrl = rootUrl
            });

            int level = 0;

            while(queue.Count > 0 && level < maxDepth)
            {
                int size = queue.Count;
                level++;

                while(size > 0)
                {
                    size--;

                    ResourceNode cur = queue.Dequeue();

                    // Access the url first in order to get the absolute uri.
                    var pageResponseMessage = httpClient.GetAsync(cur.DisplayedUrl).Result;

                    if (!visitedPages.Any(x => x.LandedUrl == pageResponseMessage.RequestMessage.RequestUri.AbsoluteUri))
                    {
                        cur.StatusCode = pageResponseMessage.StatusCode;
                        cur.LandedUrl = pageResponseMessage.RequestMessage.RequestUri.AbsoluteUri;

                        visitedPages.Add(cur);

                        processImgOnCurrentPage(pageResponseMessage);

                        List<ResourceNode> nextPages = getNextPages(pageResponseMessage, allowedPrefix);

                        for (int i = 0; i < nextPages.Count; i++)
                        {
                            queue.Enqueue(nextPages[i]);
                        }
                    }
                }
            }
        }

        private static void processImgOnCurrentPage(HttpResponseMessage httpResponseMessage)
        {
            var htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(httpResponseMessage.Content.ReadAsStringAsync().Result);

            var results = htmlDoc.DocumentNode.SelectNodes(imgXPath);

            List<string> nodeList = new List<string>();

            if (results != null && results.Count > 0)
            {
                nodeList = results
                    .Where(x => x.Attributes.Any(y => y.Name == "src"))
                    .Select(z => z.Attributes.First(c => c.Name == "src").Value)
                    .ToList();
            }

            foreach (string node in nodeList)
            {
                if (!visitedImg.Any(x => x.DisplayedUrl == node))
                {
                    var response = httpClient.GetAsync(node).Result;

                    visitedImg.Add(new ResourceNode()
                    {
                        ParentPage = httpResponseMessage.RequestMessage.RequestUri.AbsoluteUri,
                        Tag = "img",
                        DisplayedUrl = node,
                        LandedUrl = response.RequestMessage.RequestUri.AbsoluteUri,
                        StatusCode = response.StatusCode
                    });
                }
            }
        }

        private static List<ResourceNode> getNextPages(HttpResponseMessage httpResponseMessage, List<string> allowedPrefix)
        {
            List<ResourceNode> ret = new List<ResourceNode>();
            var htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(httpResponseMessage.Content.ReadAsStringAsync().Result);
            var results = htmlDoc.DocumentNode.SelectNodes(aXPath);
            List<string> nodeList = new List<string>();

            if (results != null && results.Count > 0)
            {
                nodeList = results
                    .Where(x => x.Attributes.Any(y => y.Name == "href"))
                    .Select(z => z.Attributes.First(c => c.Name == "href").Value)
                    .Where(b => !(b.Contains("javascript") || b.Contains("mailto")))
                    .ToList();
            }

            foreach (string node in nodeList)
            {
                if(node.Contains("http") && !allowedPrefix.Any(x => node.Contains(x)))
                {
                    continue;
                }

                ret.Add(new ResourceNode()
                {
                    ParentPage = httpResponseMessage.RequestMessage.RequestUri.AbsoluteUri,
                    Tag = "a",
                    DisplayedUrl = node
                });
            }

            return ret;
        }

        
    }
}
