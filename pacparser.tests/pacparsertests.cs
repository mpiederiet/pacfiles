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

    public class HelperFunctionTests
    {
        pacparser parser;
        Uri Url;
        string host;

        public HelperFunctionTests()
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
    }
}
