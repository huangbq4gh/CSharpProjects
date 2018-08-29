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
            login();

            //string rootUrl = "https://www.alarm.com";
            //List<string> allowedPrefix = new List<string>() { rootUrl };
            //httpClient.BaseAddress = new Uri(rootUrl);
            //DateTime start = DateTime.Now;
            //BFSCrawler(rootUrl, 2, SecurityProtocolType.Tls12, allowedPrefix);

            //exportResults();

            //TimeSpan usedTime = DateTime.Now.Subtract(start);
            //Console.WriteLine($"Used {usedTime.Hours} hr, {usedTime.Minutes} min, {usedTime.Seconds} sec, {usedTime.Milliseconds} ms");
            //Console.ReadLine();
        }

        private static void login()
        {
            string pageSource = string.Empty;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            CookieContainer loginCookie = new CookieContainer();


            HttpWebRequest request = WebRequest.CreateHttp(@"https://alarmadmin.alarm.com/Default.aspx");
            request.Credentials = new NetworkCredential("adcregression", "regression1");
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            string param = string.Format(@"__EVENTTARGET=&__EVENTARGUMENT=&__VIEWSTATE={0}&__VIEWSTATEGENERATOR={1}&__VIEWSTATEENCRYPTED=&__EVENTVALIDATION={2}&txtUsername={3}&txtPassword={4}&butLogin={5}",
                "DJDCWv4zY3DcbwoJIOoZ3g98+02LXyh6/dmR+YmizbD+tXc8Ik8TK5KowI1ad/z+WOA0Oo1xhcqM3SMRSEz0sOOG7gXSC6+Z4ClXMKhY1qjOdihIOv8cHDtoupiTaWI89ylyJgOBJSeUdpgcgPgo3AG7pGJ2IBwvGjj8pOeXxj3qKPkr3i3av/rmEHz5DTukaXjowt805yPHkJLsHR+Im6gFy6lt4X/BJHsDydxg+DFfAKmdD4i/jLw29bubJ3HyN479vQLpYtRoUEYghtXmNtwfYgfVwG1EoAWSiFu9BBq9B/YhzrCtbtmwEHlbmhCu",
                "CA0B0334",
                "dIIbjCpWEjayJ0kFrMjdCnnyAKXGyvSt7x/vgdHfPwbaHmU5HwiNc2NisZNekcLYUMpnyR+XQdSigFsrDgI3J9N+RnkacH45IwI9peTSEgGNoiRmi5jyR87ELocQJNLHOfZ4O+VZXtqDmA3P2iSoxHXqPOvo2jMseXF/86vdlrtQEYJjTOTdPy7kH1SrhT6z",
                "adcregression",
                "regression1",
                "Login");
            byte[] bytes = Encoding.ASCII.GetBytes(param);
            request.ContentLength = bytes.Length;
            request.CookieContainer = new CookieContainer();
            request.AllowAutoRedirect = false;
            using (Stream os = request.GetRequestStream())
            {
                os.Write(bytes, 0, bytes.Length);
            }
            WebResponse response = request.GetResponse();
            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
            {
                pageSource = sr.ReadToEnd();
            }
            string cookieHeader = response.Headers["Set-cookie"];

            // THIS IS THE WORKING PART
            loginCookie.Add(new Cookie("ASP.NET_SessionId", "0f5xkog5bf0izgsv0zpd3xrh","/", "alarmadmin.alarm.com"));
            loginCookie.Add(new Cookie("DealerAuthProvider", "ADCRepLogin", "/", "alarmadmin.alarm.com"));
            loginCookie.Add(new Cookie("auth_AdminDotNet", "2F21A595B040D21E93960C4B9844087DF862E0695854BEAA6B63367CDEDEEB41C64A204E1393A7CF8C574D86607AD23F26788EB0653C1DDAE5406A364EDAB91F902DF1C1023693372F242475B055F99D77ED8077C989B629F7F3B2FE076056790C406C9D069661CB67DAE87EDEE72BDC99DBCA9B2121EC095FD7EAB002F53F6C763AF42B099EC6B5F927C83E1C8F3802", "/", "alarmadmin.alarm.com"));
            loginCookie.Add(new Cookie("loggedInAsRep", "1", "/", "alarm.com"));
            loginCookie.Add(new Cookie("twoFactorAuthenticationId", "E710C17337E3C77E8191EC62FBF2BF587FD3AA6EFBCB235E344F6D128AA6D05F", "/", "alarmadmin.alarm.com"));

            HttpWebRequest afterLoginRequest = WebRequest.CreateHttp(@"https://alarmadmin.alarm.com/Support/FindCustomer.aspx");
            afterLoginRequest.Headers.Add("Cookie", cookieHeader);
            afterLoginRequest.Method = "GET";
            afterLoginRequest.CookieContainer = loginCookie;
            WebResponse afterLoginResponse = afterLoginRequest.GetResponse();
            using (StreamReader sr = new StreamReader(afterLoginResponse.GetResponseStream()))
            {
                pageSource = sr.ReadToEnd();
            }

        }

        private static void exportResults()
        {
            var csv = new StringBuilder();

            foreach (ResourceNode node in visitedImg)
            {
                csv.Append($"{node.ParentPage},{node.Tag},{node.DisplayedUrl},{node.LandedUrl},{node.StatusCode}{Environment.NewLine}");
            }

            File.WriteAllText(@"D:\C#Projects\WebCrawler\visitedImg.csv", csv.ToString());

            csv.Clear();

            foreach (ResourceNode node in visitedPages)
            {
                csv.Append($"{node.ParentPage},{node.Tag},{node.DisplayedUrl},{node.LandedUrl},{node.StatusCode}{Environment.NewLine}");
            }

            File.WriteAllText(@"D:\C#Projects\WebCrawler\visitedPages.csv", csv.ToString());
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

                    Console.WriteLine($"Queue size: {queue.Count}");
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
