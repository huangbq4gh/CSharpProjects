using System.Net;

namespace WebCrawler.Objects
{
    class ResourceNode
    {
        public string ParentPage { get; set; }

        /// <summary>
        /// The value of href or src
        /// </summary>
        public string DisplayedUrl { get; set; }

        public string LandedUrl { get; set; }

        /// <summary>
        /// Sample: a, img
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// http status code
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }
    }
}
