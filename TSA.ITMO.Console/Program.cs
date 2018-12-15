using System;
using System.IO;
using TSA.ITMO;
using Newtonsoft.Json;

using sc = System.Console;

namespace TSA.ITMO.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var loader = new Loader()
            {
                LogFunction = ws => sc.WriteLine(ws)
            };
            var docs = loader.Load();
            if (docs != null)
            {
                //
                var json = JsonConvert.SerializeObject(docs, Formatting.Indented);
                File.WriteAllText("itmo.json", json);
                //
            }
            sc.WriteLine("press enter to exit...");
            sc.ReadLine();
        }
    }
}
