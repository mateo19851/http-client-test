using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace TestHttpClient
{
    [TestFixture]
    public class HttpClientApprachTests
    {
        private static readonly string WebApiForecast = $"https://localhost:{ApplicationPort}/WeatherForecast";
        private const int ApplicationPort = 7187;
        private IHttpClientFactory _httpClientFactoryAsExtension;
        private List<TcpConnectionInformation> _initialConnectionsCnt;

        private Func<TcpConnectionInformation, bool> ActiveTcpConnectionsPredicate = x =>
        {
            return x.RemoteEndPoint.Port == ApplicationPort;
        };

        [SetUp]
        public void Initialize()
        {
            var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
            _httpClientFactoryAsExtension = serviceProvider.GetService<IHttpClientFactory>();
            _initialConnectionsCnt = ShowActiveTcpConnections().Where(ActiveTcpConnectionsPredicate).ToList();
        }

        [Test]
        public async Task ShouldNotLeaveUnwantedConnections_WhenHttpClientUsedDirectly()
        {
            // Arrange
            var listOfResponse = new List<HttpResponseMessage>();

            // Act
            for (var i = 100; i > 0; i--)
            {
                using (var httpClient = new HttpClient())
                {
                    listOfResponse.Add(await httpClient.GetAsync(WebApiForecast));
                }
            }

            var tcpConnections = ShowActiveTcpConnections().Where(ActiveTcpConnectionsPredicate).ToList();

            // Assert
            var connectionDiff = tcpConnections.Count() - _initialConnectionsCnt.Count();
            Assert.IsTrue(listOfResponse.All(x => x.IsSuccessStatusCode));
            Assert.That(connectionDiff, Is.EqualTo(1));
        }


        [Test]
        public async Task ShouldNotLeaveUnwantedConnections_WhenHttpClientFromHttpClientFactory()
        {
            // Arrange
            var listOfResponse = new List<HttpResponseMessage>();

            // Act
            for (var i = 100; i > 0; i--)
            {
                using (var httpClient = HttpClientFactory.Create())
                {
                    listOfResponse.Add(await httpClient.GetAsync(WebApiForecast));
                }
            }

            var tcpConnections = ShowActiveTcpConnections().Where(ActiveTcpConnectionsPredicate).ToList();

            // Assert
            var connectionDiff = tcpConnections.Count() - _initialConnectionsCnt.Count();
            Assert.IsTrue(listOfResponse.All(x => x.IsSuccessStatusCode));
            Assert.That(connectionDiff, Is.EqualTo(1));
        }

        [Test]
        public async Task ShouldNotLeaveUnwantedConnections_WhenHttpClientFromIHttpClientFactory()
        {
            // Arrange
            var listOfResponse = new List<HttpResponseMessage>();

            // Act
            for(var i = 100; i > 0; i--)
            {
                using (var httpClient = _httpClientFactoryAsExtension.CreateClient())
                {
                    listOfResponse.Add(await httpClient.GetAsync(WebApiForecast));
                }
            }

            var tcpConnections = ShowActiveTcpConnections().Where(ActiveTcpConnectionsPredicate).ToList();

            // Assert
            var connectionDiff = tcpConnections.Count() - _initialConnectionsCnt.Count();
            var someConns = ShowActiveTcpConnections().Where(x => x.RemoteEndPoint.Port == ApplicationPort);
            Assert.IsTrue(listOfResponse.All(x => x.IsSuccessStatusCode));
            Assert.That(connectionDiff, Is.EqualTo(1));
        }

        public static IEnumerable<TcpConnectionInformation> ShowActiveTcpConnections()
        {
           IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
           return  properties.GetActiveTcpConnections();
        }
    }
}
