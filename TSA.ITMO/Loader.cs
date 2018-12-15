using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using TSA.Interfaces;

namespace TSA.ITMO
{
    public class Document: IDocument
    {
        public string Name { get; set; }
        public string Content { get; set; }
    }


    public class Loader
    {
        public Action<string> LogFunction { get; set; }

        public void Log(string msg, params object[] args)
        {
            if (LogFunction == null) return;
            if ((args != null) && (args.Length > 0)) msg = string.Format(msg, args);
            msg = string.Format("{0} {1}",DateTime.Now.ToString(), msg);
            LogFunction(msg);
        }

        private string baseUrl = "https://books.ifmo.ru/";

        public List<IDocument> Load()
        {
            try
            {
                Log("Начало загрузки...");
                var result = new List<IDocument>();

                Log("base catalog loading...");
                var text = GetBaseCatalogPage();
                Log("base catalog extracting...");
                var catalogLinks = ExtractCatalogLinks(text);
                foreach (var item in catalogLinks)
                {
                    Log("catalog {0} loading...", item);
                    var catalogText = GetCatalogPage(item);
                    Log("catalog {0} extracting links...", item);
                    var bookLinks = ExtractBookLinks(catalogText);
                    Log("processing links...");
                    foreach (var link in bookLinks)
                    {
                        Log("loading book {0}", link);
                        var bookText = GetBookPage(link);
                        if (string.IsNullOrEmpty(bookText)) continue;
                        Log("processing book {0}", link);
                        var document = BuildDocument(bookText);
                        if (document != null)
                        {
                            Log("processing book {0} success!", link);
                            result.Add(document);
                        }
                        else
                        {
                            Log("error: unable ti parse book");
                        }
                    }
                }
                Log("Окончание загрузки.");
                return result;
            }
            catch (Exception ex)
            {
                Log("Ошибка при выполнении загрузки. Загрузка прервана.{0}", ex.ToString());
                return null;
            }
        }

        private IDocument BuildDocument(string bookText)
        {
            var regexp = new Regex("<div class=\"span9\">.*?</div>", RegexOptions.CultureInvariant | RegexOptions.Singleline);
            var match = regexp.Match(bookText);
            if (!match.Success) return null;
            var body = match.Groups[0].ToString();
            var regexp2 = new Regex(".*?<h1>(.*?)</h1>(.*)</div>", RegexOptions.CultureInvariant | RegexOptions.Singleline);
            var match2 = regexp2.Match(body);
            if (!(match2.Success)) return null;
            var header = match2.Groups[1].ToString();
            body = match2.Groups[2].ToString();
            var regexp3 = new Regex("<a[^>]*?[.]pdf.*?</a>(\\s*\\([^)]*?\\))?", RegexOptions.CultureInvariant | RegexOptions.Singleline);
            var match3 = regexp3.Match(body);
            if (match3.Success)
            {
                body = body.Replace(match3.Groups[0].ToString()," ");
            }
            var regexp4 = new Regex("<[^>]+>", RegexOptions.CultureInvariant | RegexOptions.Singleline);
            body = regexp4.Replace(body," ");
            var document = new Document() { Name = header, Content = body };
            return document;
        }

        private string GetBookPage(string link)
        {
            return GetPage(baseUrl + link);
        }

        private List<string> ExtractBookLinks(string catalogText)
        {
            var regexp = new Regex("<a\\s+href=\"/(book/\\d+/)[^\"]+\">", RegexOptions.CultureInvariant | RegexOptions.Singleline);
            var matches = regexp.Matches(catalogText);
            var result = new List<string>();
            foreach (Match match in matches)
            {
                if (!match.Success) continue;
                var url = match.Groups[1].ToString();
                result.Add(url);
            }
            return result;
        }

        private string GetCatalogPage(string item)
        {
            return GetPage(baseUrl + item);
        }

        private List<string> ExtractCatalogLinks(string text)
        {
            var regexp = new Regex("<li(\\s+class=\"active\")?><a\\s+href=\"/(catalog/(\\d+)/catalog_.*?.htm)\"\\s+class=\"years\">\\d+</a></li>", RegexOptions.CultureInvariant | RegexOptions.Singleline);
            var matches = regexp.Matches(text);
            var result = new List<string>();
            foreach (Match match in matches)
            {
                if (!match.Success) continue;
                var url = match.Groups[2].ToString();
                result.Add(url);
            }
            return result;
        }

        public string GetBaseCatalogPage()
        {
            var url = $"{baseUrl}catalog/";
            return GetPage(url);
        }

        private string GetPage(string url)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    return client.DownloadString(url);
                }
            }
            catch (Exception ex)
            {
                Log("Book not found: {0}", url);
                return null;
            }
        }
    }
}
