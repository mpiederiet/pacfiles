using System;
using Xunit;

namespace pacfiles.tests
{
    public class PacparserTests
    {
        pacparser parser;
        Uri Url;
        string host;

        public PacparserTests()
        {
            parser = new pacparser();
            Url = new Uri("https://example.org");
            host = Url.Host;
        }

        [Theory]
        [InlineData("function FindProxyForURL(url, host) {return \"DIRECT\"}", "DIRECT")]
        [InlineData("function FindProxyForURL(url, host) {return \"PROXY proxy.example.org 8080\"}", "PROXY proxy.example.org 8080")]
        public void ReturnsReturnValue(string pacfunction, string proxy)
        {
            parser.Execute(pacfunction);

            Assert.Equal(proxy, parser.FindProxyForURL(Url, host));
        }
    }

    public class DotNetHelperFunctionTests
    {
        [Theory]
        // Test cases from https://docs.microsoft.com/en-us/windows/win32/winhttp/isinnetex
        [InlineData("198.95.249.79", "198.95.249.79/32", true)]
        [InlineData("1.1.1.1", "198.95.249.79/32", false)]
        [InlineData("198.95.1.1", "198.95.0.0/16", true)]
        [InlineData("1.1.1.1", "198.95.0.0/16", false)]
        [InlineData("3ffe:8311:ffff::", "3ffe:8311:ffff::/48", true)]
        [InlineData("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff", "3ffe:8311:ffff::/48", false)]
        [InlineData("1.1.1.1", "3ffe:8311:ffff::/48", false)] // Test mismatched prefix & address types
        [InlineData("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff", "198.95.0.0/16", false)] // Test mismatched prefix & address types
        public void TestsIpIsInNetEx(string ipaddress, string ipprefix, bool expectedResult)
        {
            Assert.Equal(expectedResult, pacparser.isInNetEx(ipaddress, ipprefix));
        }
    }

    public class JavascriptHelperFunctionTests
    {
        pacparser parser;
        Uri Url;
        string host;

        public JavascriptHelperFunctionTests()
        {
            parser = new pacparser();
            Url = new Uri("https://example.org");
            host = Url.Host;
        }

        [Theory]
        [InlineData("127.0.0.1","127.0.0.1")]
        public void ResolvesAHost(string host, string expectedIp)
        {
            string pacfunction = "function FindProxyForURL(url, host) {return dnsResolve(host)}";
            parser.Execute(pacfunction);

            Assert.Equal(expectedIp, parser.FindProxyForURL(Url, host));
        }
        
        [Theory]
        [InlineData("127.0.0.1","127.0.0.1")]
        public void ResolvesAHostEx(string host, string expectedIp)
        {
            string pacfunction = "function FindProxyForURL(url, host) {return dnsResolveEx(host)}";
            parser.Execute(pacfunction);

            Assert.Equal(expectedIp, parser.FindProxyForURL(Url, host));
        }

        [Theory]
        [InlineData("127.0.0.1","true")]
        [InlineData("0.0.0.0","false")]
        public void TestsHostResolution(string host, string expectedResult)
        {
            string pacfunction = "function FindProxyForURL(url, host) {return isResolvable(host)}";
            parser.Execute(pacfunction);

            Assert.Equal(expectedResult, parser.FindProxyForURL(Url, host));
        }

        [Theory]
        [InlineData("127.0.0.1","true")]
        [InlineData("0.0.0.0","false")]
        public void TestsHostResolutionEx(string host, string expectedResult)
        {
            string pacfunction = "function FindProxyForURL(url, host) {return isResolvableEx(host)}";
            parser.Execute(pacfunction);

            Assert.Equal(expectedResult, parser.FindProxyForURL(Url, host));
        }

        [Fact]
        public void FakesMyIpAddress()
        {
            string pacfunction = "function FindProxyForURL(url, host) {return myIpAddress()}";
            parser.Execute(pacfunction);

            string FakeIpAddress = "1.2.3.4";
            parser.myIpAddress = FakeIpAddress;

            Assert.Equal(FakeIpAddress, parser.FindProxyForURL(Url, host));
        }

        [Fact(Skip = "Not implemented")]
        public void FakesMyIpAddressEx()
        {
            string pacfunction = "function FindProxyForURL(url, host) {return myIPAddressEx()}";
            parser.Execute(pacfunction);

            string FakeIpAddress = "1.2.3.4";
            parser.myIpAddress = FakeIpAddress;

            Assert.Equal(FakeIpAddress, parser.FindProxyForURL(Url, host));
        }

        [Theory]
        [InlineData("198.95.249.79","198.95.249.79/32", "true")]
        [InlineData("1.1.1.1","198.95.249.79/32", "false")]
        [InlineData("198.95.1.1", "198.95.0.0/16", "true")]
        [InlineData("1.1.1.1", "198.95.0.0/16", "false")]
        [InlineData("3ffe:8311:ffff::", "3ffe:8311:ffff::/48", "true")]
        [InlineData("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff", "3ffe:8311:ffff::/48", "false")]
        [InlineData("1.1.1.1", "3ffe:8311:ffff::/48", "false")] // Test mismatched prefix & address types
        [InlineData("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff", "198.95.0.0/16", "false")] // Test mismatched prefix & address types
        public void TestsIpIsInNetEx(string ipaddress, string ipprefix, string expectedResult)
        {
            string pacfunction = string.Format(
                "function FindProxyForURL(url, host) {{return isInNetEx(\"{0}\", \"{1}\")}}", ipaddress, ipprefix
                );
            parser.Execute(pacfunction);
            Assert.Equal(expectedResult, parser.FindProxyForURL(Url, host));
        }
    }
}
