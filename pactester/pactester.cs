using System;

namespace pacfiles
{
    class pactester
    {
        static void Main(string[] args)
        {
            pacparser pacparser = new pacparser();

            // The .pac file
            string pacfile = @"
function FindProxyForURL(url, host) {
              // our local URLs from the domains below example.com don't need a proxy:
              if (shExpMatch(host, "" *.example.com"")) return ""DIRECT"";

              // URLs within this network are accessed through
              // port 8080 on fastproxy.example.com:
                        if (isInNet(host, ""10.0.0.0"", ""255.255.248.0"")) return ""PROXY fastproxy.example.com:8080"";

                        if (dnsDomainIs(host, ""example.com"")) return ""PROXY proxy2.example.com:8888"";

                        // All other requests go through port 8080 of proxy.example.com.
                        // should that fail to respond, go directly to the WWW:
                        return ""PROXY proxy.example.com:8080; DIRECT"";
                    }
            ";

            pacparser.Execute(pacfile);

            Uri url = new Uri("http://example.com");
            string host = "example.com";

            Console.WriteLine(pacparser.FindProxyForURL(url, host));
        }
    }
}
