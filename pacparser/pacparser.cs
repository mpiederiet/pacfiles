using System;
using System.Linq;
using System.Net;
using Jint;

namespace pacfiles
{
    public class pacparser
    {
        private static IPAddress[] DnsResolutionHelper(string host) {
            try {
                return Dns.GetHostAddresses(host);
            } catch {
                return new IPAddress[0];
            }
        }

        private static string DnsResolve(string host)
        {
            // Resolves the given DNS hostname into an IP address, and returns it in the dot-separated format as a string.
            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Proxy_servers_and_tunneling/Proxy_Auto-Configuration_(PAC)_file#dnsResolve
            // dnsResolve's failure value is not standardised,
            // fx & chromium return null and IE returns false
            // https://chromium.googlesource.com/chromium/src.git/+/refs/heads/master/services/proxy_resolver/proxy_resolver_v8.cc#56
            // https://dxr.mozilla.org/mozilla-central/source/netwerk/base/ProxyAutoConfig.cpp#525-527

            IPAddress address = DnsResolutionHelper(host).FirstOrDefault();
            return address != null ? address.ToString() : "";
        }

        private static string DnsResolveEx(string host) {
            // Resolve a host string to its IP address
            // Return: A semi-colon delimited string containing IPv6 and IPv4 addresses or an empty string if host is not resolvable.
            // https://docs.microsoft.com/en-us/windows/win32/winhttp/dnsresolveex

            return String.Join(";", 
                DnsResolutionHelper(host).Select(x => x.ToString())
            );
        }

        // TODO: Validate this is in dotted decimal format.
        public string myIpAddress = DnsResolve(Dns.GetHostName());

        // Find all the IP addresses for localhost. Return value: A semi-colon delimited string containing all IP addresses for localhost (IPv6 and/or IPv4), or an empty string if unable to resolve localhost to an IP address.
        // https://docs.microsoft.com/en-us/windows/win32/winhttp/myipaddressex
        // This is like myIpAddress(), but instead of returning a single IP address, it can return multiple IP addresses. It returns a string containing a semi-colon separated list of addresses. On failure it returns an empty string to indicate no results (whereas myIpAddress() returns 127.0.0.1).
        // https://chromium.googlesource.com/chromium/src/+/HEAD/net/docs/proxy.md#resolving-client_s-ip-address-within-a-pac-script-using-myipaddressex
        public string myIpAddressEx = DnsResolveEx(Dns.GetHostName());

        private Engine engine = new Engine();

        private void Init()
        {
            // Helpers not defined in Javascript
            // TODO: add the IPv6 extension helpers? https://docs.microsoft.com/en-us/windows/win32/winhttp/ipv6-aware-proxy-helper-api-definitions
            // isResolvableEx() is defined in Chromium https://chromium.googlesource.com/chromium/src.git/+/refs/heads/master/services/proxy_resolver/pac_js_library.h
            engine.SetValue("alert", new Action<object>(Console.WriteLine));
            engine.SetValue("dnsResolve", new Func<string, string>(host => DnsResolve(host)));
            engine.SetValue("myIpAddress", new Func<string>(() => {return myIpAddress;}));
            engine.SetValue("myIpAddressEx", new Func<string>(() => { return myIpAddressEx; }));

            // Javascript helper functions, from Mozilla
            // https://hg.mozilla.org/mozilla-central/raw-file/tip/netwerk/base/ProxyAutoConfig.cpp
            #region javascripthelpers
            string sAsciiPacUtils =
"function dnsDomainIs(host, domain) {\n"
+ "    return (host.length >= domain.length &&\n"
+ "            host.substring(host.length - domain.length) == domain);\n"
+ "}\n"
+ ""
+ "function dnsDomainLevels(host) {\n"
+ "    return host.split('.').length - 1;\n"
+ "}\n"
+ ""
+ "function isValidIpAddress(ipchars) {\n"
+ "    var matches = "
+ "/^(\\d{1,3})\\.(\\d{1,3})\\.(\\d{1,3})\\.(\\d{1,3})$/.exec(ipchars);\n"
+ "    if (matches == null) {\n"
+ "        return false;\n"
+ "    } else if (matches[1] > 255 || matches[2] > 255 || \n"
+ "               matches[3] > 255 || matches[4] > 255) {\n"
+ "        return false;\n"
+ "    }\n"
+ "    return true;\n"
+ "}\n"
+ ""
+ "function convert_addr(ipchars) {\n"
+ "    var bytes = ipchars.split('.');\n"
+ "    var result = ((bytes[0] & 0xff) << 24) |\n"
+ "                 ((bytes[1] & 0xff) << 16) |\n"
+ "                 ((bytes[2] & 0xff) <<  8) |\n"
+ "                  (bytes[3] & 0xff);\n"
+ "    return result;\n"
+ "}\n"
+ ""
+ "function isInNet(ipaddr, pattern, maskstr) {\n"
+ "    if (!isValidIpAddress(pattern) || !isValidIpAddress(maskstr)) {\n"
+ "        return false;\n"
+ "    }\n"
+ "    if (!isValidIpAddress(ipaddr)) {\n"
+ "        ipaddr = dnsResolve(ipaddr);\n"
+ "        if (ipaddr == null) {\n"
+ "            return false;\n"
+ "        }\n"
+ "    }\n"
+ "    var host = convert_addr(ipaddr);\n"
+ "    var pat  = convert_addr(pattern);\n"
+ "    var mask = convert_addr(maskstr);\n"
+ "    return ((host & mask) == (pat & mask));\n"
+ "    \n"
+ "}\n"
+ ""
+ "function isPlainHostName(host) {\n"
+ "    return (host.search('\\\\.') == -1);\n"
+ "}\n"
+ ""
+ "function isResolvable(host) {\n"
+ "    var ip = dnsResolve(host);\n"
+ "    return (ip != null);\n"
+ "}\n"
+ ""
+ "function localHostOrDomainIs(host, hostdom) {\n"
+ "    return (host == hostdom) ||\n"
+ "           (hostdom.lastIndexOf(host + '.', 0) == 0);\n"
+ "}\n"
+ ""
+ "function shExpMatch(url, pattern) {\n"
+ "   pattern = pattern.replace(/\\./g, '\\\\.');\n"
+ "   pattern = pattern.replace(/\\*/g, '.*');\n"
+ "   pattern = pattern.replace(/\\?/g, '.');\n"
+ "   var newRe = new RegExp('^'+pattern+'$');\n"
+ "   return newRe.test(url);\n"
+ "}\n"
+ ""
+ "var wdays = {SUN: 0, MON: 1, TUE: 2, WED: 3, THU: 4, FRI: 5, SAT: 6};\n"
+ "var months = {JAN: 0, FEB: 1, MAR: 2, APR: 3, MAY: 4, JUN: 5, JUL: 6, "
+ "AUG: 7, SEP: 8, OCT: 9, NOV: 10, DEC: 11};\n"
+ ""
+ "function weekdayRange() {\n"
+ "    function getDay(weekday) {\n"
+ "        if (weekday in wdays) {\n"
+ "            return wdays[weekday];\n"
+ "        }\n"
+ "        return -1;\n"
+ "    }\n"
+ "    var date = new Date();\n"
+ "    var argc = arguments.length;\n"
+ "    var wday;\n"
+ "    if (argc < 1)\n"
+ "        return false;\n"
+ "    if (arguments[argc - 1] == 'GMT') {\n"
+ "        argc--;\n"
+ "        wday = date.getUTCDay();\n"
+ "    } else {\n"
+ "        wday = date.getDay();\n"
+ "    }\n"
+ "    var wd1 = getDay(arguments[0]);\n"
+ "    var wd2 = (argc == 2) ? getDay(arguments[1]) : wd1;\n"
+ "    return (wd1 == -1 || wd2 == -1) ? false\n"
+ "                                    : (wd1 <= wd2) ? (wd1 <= wday && wday "
+ "<= wd2)\n"
+ "                                                   : (wd2 >= wday || wday "
+ ">= wd1);\n"
+ "}\n"
+ ""
+ "function dateRange() {\n"
+ "    function getMonth(name) {\n"
+ "        if (name in months) {\n"
+ "            return months[name];\n"
+ "        }\n"
+ "        return -1;\n"
+ "    }\n"
+ "    var date = new Date();\n"
+ "    var argc = arguments.length;\n"
+ "    if (argc < 1) {\n"
+ "        return false;\n"
+ "    }\n"
+ "    var isGMT = (arguments[argc - 1] == 'GMT');\n"
+ "\n"
+ "    if (isGMT) {\n"
+ "        argc--;\n"
+ "    }\n"
+ "    // function will work even without explict handling of this case\n"
+ "    if (argc == 1) {\n"
+ "        var tmp = parseInt(arguments[0]);\n"
+ "        if (isNaN(tmp)) {\n"
+ "            return ((isGMT ? date.getUTCMonth() : date.getMonth()) ==\n"
+ "                     getMonth(arguments[0]));\n"
+ "        } else if (tmp < 32) {\n"
+ "            return ((isGMT ? date.getUTCDate() : date.getDate()) == "
+ "tmp);\n"
+ "        } else { \n"
+ "            return ((isGMT ? date.getUTCFullYear() : date.getFullYear()) "
+ "==\n"
+ "                     tmp);\n"
+ "        }\n"
+ "    }\n"
+ "    var year = date.getFullYear();\n"
+ "    var date1, date2;\n"
+ "    date1 = new Date(year,  0,  1,  0,  0,  0);\n"
+ "    date2 = new Date(year, 11, 31, 23, 59, 59);\n"
+ "    var adjustMonth = false;\n"
+ "    for (var i = 0; i < (argc >> 1); i++) {\n"
+ "        var tmp = parseInt(arguments[i]);\n"
+ "        if (isNaN(tmp)) {\n"
+ "            var mon = getMonth(arguments[i]);\n"
+ "            date1.setMonth(mon);\n"
+ "        } else if (tmp < 32) {\n"
+ "            adjustMonth = (argc <= 2);\n"
+ "            date1.setDate(tmp);\n"
+ "        } else {\n"
+ "            date1.setFullYear(tmp);\n"
+ "        }\n"
+ "    }\n"
+ "    for (var i = (argc >> 1); i < argc; i++) {\n"
+ "        var tmp = parseInt(arguments[i]);\n"
+ "        if (isNaN(tmp)) {\n"
+ "            var mon = getMonth(arguments[i]);\n"
+ "            date2.setMonth(mon);\n"
+ "        } else if (tmp < 32) {\n"
+ "            date2.setDate(tmp);\n"
+ "        } else {\n"
+ "            date2.setFullYear(tmp);\n"
+ "        }\n"
+ "    }\n"
+ "    if (adjustMonth) {\n"
+ "        date1.setMonth(date.getMonth());\n"
+ "        date2.setMonth(date.getMonth());\n"
+ "    }\n"
+ "    if (isGMT) {\n"
+ "    var tmp = date;\n"
+ "        tmp.setFullYear(date.getUTCFullYear());\n"
+ "        tmp.setMonth(date.getUTCMonth());\n"
+ "        tmp.setDate(date.getUTCDate());\n"
+ "        tmp.setHours(date.getUTCHours());\n"
+ "        tmp.setMinutes(date.getUTCMinutes());\n"
+ "        tmp.setSeconds(date.getUTCSeconds());\n"
+ "        date = tmp;\n"
+ "    }\n"
+ "    return (date1 <= date2) ? (date1 <= date) && (date <= date2)\n"
+ "                            : (date2 >= date) || (date >= date1);\n"
+ "}\n"
+ ""
+ "function timeRange() {\n"
+ "    var argc = arguments.length;\n"
+ "    var date = new Date();\n"
+ "    var isGMT= false;\n"
+ ""
+ "    if (argc < 1) {\n"
+ "        return false;\n"
+ "    }\n"
+ "    if (arguments[argc - 1] == 'GMT') {\n"
+ "        isGMT = true;\n"
+ "        argc--;\n"
+ "    }\n"
+ "\n"
+ "    var hour = isGMT ? date.getUTCHours() : date.getHours();\n"
+ "    var date1, date2;\n"
+ "    date1 = new Date();\n"
+ "    date2 = new Date();\n"
+ "\n"
+ "    if (argc == 1) {\n"
+ "        return (hour == arguments[0]);\n"
+ "    } else if (argc == 2) {\n"
+ "        return ((arguments[0] <= hour) && (hour <= arguments[1]));\n"
+ "    } else {\n"
+ "        switch (argc) {\n"
+ "        case 6:\n"
+ "            date1.setSeconds(arguments[2]);\n"
+ "            date2.setSeconds(arguments[5]);\n"
+ "        case 4:\n"
+ "            var middle = argc >> 1;\n"
+ "            date1.setHours(arguments[0]);\n"
+ "            date1.setMinutes(arguments[1]);\n"
+ "            date2.setHours(arguments[middle]);\n"
+ "            date2.setMinutes(arguments[middle + 1]);\n"
+ "            if (middle == 2) {\n"
+ "                date2.setSeconds(59);\n"
+ "            }\n"
+ "            break;\n"
+ "        default:\n"
+ "          throw 'timeRange: bad number of arguments'\n"
+ "        }\n"
+ "    }\n"
+ "\n"
+ "    if (isGMT) {\n"
+ "        date.setFullYear(date.getUTCFullYear());\n"
+ "        date.setMonth(date.getUTCMonth());\n"
+ "        date.setDate(date.getUTCDate());\n"
+ "        date.setHours(date.getUTCHours());\n"
+ "        date.setMinutes(date.getUTCMinutes());\n"
+ "        date.setSeconds(date.getUTCSeconds());\n"
+ "    }\n"
+ "    return (date1 <= date2) ? (date1 <= date) && (date <= date2)\n"
+ "                            : (date2 >= date) || (date >= date1);\n"
+ "\n"
+ "}\n"
+ ""
;
            #endregion javascripthelpers
            engine.Execute(sAsciiPacUtils);
        }

        public pacparser()
        {
            Init();
        }

        public pacparser(string pacfile)
        {
            Init();
            Execute(pacfile);
        }

        public void Execute(string pacfile)
        {
            engine.Execute(pacfile);
        }

        public string FindProxyForURL(Uri Url)
        {
            return FindProxyForURL(Url, Url.Host);
        }

        public string FindProxyForURL(Uri Url, string Host)
        {
            var FindProxyForURL = engine.GetValue("FindProxyForURL");

            if (Host == null) { Host = Url.Host; }
            string proxy = FindProxyForURL.Invoke(Url.ToString(), Host).ToString();

            return proxy.ToString();
        }
    }
}
