using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TSA.Interfaces;
using TSA.ML;
using TSA.Web.Models;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace TSA.Web.Controllers
{
    public class Document : IDocument
    {
        public string Name { get; set; }
        public string Content { get; set; }
    }

    internal class JsonDocumentSource : IDocumentSource
    {
        public IEnumerable<IDocument> GetDocuments()
        {
            var docs = JsonConvert.DeserializeObject<List<Document>>(
                File.ReadAllText(@"..\itmo.json"));
            return docs;
        }
    }

    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.step = "SelectSource";
            return View();
        }

        [HttpPost]
        public IActionResult Index(string step)
        {
            return View();
        }

        protected static DateTime? lastStart = null;

        [HttpPost]
        public object StartTeach()
        {
            string error = "";
            lastStart = lastStart ?? DateTime.Now;
            var thread = new Thread(TeachModel);
            thread.Start();
            return new { error = error, success = string.IsNullOrEmpty(error) };
        }

        private static IReadOnlyList<ITopic> topics;
        private static object locker = new object();

        public void TeachModel()
        {
            var topicGrouper = new TopicGrouper();
            var tops = topicGrouper.GroupDocuments(new JsonDocumentSource(), 30);
            lock (locker)
            {
                topics = tops;
            }
        }

        [HttpPost]
        public object IsTeachFinished()
        {
            string error = "";
            var ls = lastStart;
            var success = false;
            lock (locker)
            {
                success = topics != null; // (ls != null) && (DateTime.Now.Subtract(ls.Value).TotalSeconds >= 45);
            }
            if (success) lastStart = null;
            return new { success = success };
        }

        [HttpPost]
        public IActionResult ModelSelect(string step)
        {
            return View();
        }

        public IActionResult Train(int source = -1, int model = -1)
        {
            return View();
        }

        public IActionResult Topics()
        {
            return View();
        }

        public IActionResult Results()
        {
            if (topics != null)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < topics.Count; i++)
                {
                    sb.AppendLine(string.Format("<h2> Категория номер {0} </h2>", i + 1));
                    sb.AppendLine("<ul>");
                    var counter = 0;
                    foreach (var item in topics[i].Documents)
                    {
                        counter++;
                        if (counter > 20)
                        {
                            sb.AppendLine(string.Format("<li>...</li>"));
                            break;
                        }
                        sb.AppendLine(string.Format("<li>{0}</li>", System.Net.WebUtility.HtmlEncode(item.Name)));
                    }
                    sb.AppendLine("</ul>");
                }
                ViewBag.results = sb.ToString();
            }
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
