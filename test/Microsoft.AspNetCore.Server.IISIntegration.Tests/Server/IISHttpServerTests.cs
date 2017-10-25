using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tests
{
    public class IISHttpServerTests
    {
        [Fact]
        public async Task HelloWorldTest()
        {
            // Flow
            // Add in managed request (HttpRequest) that will be read appropriately.
            var mockFunctions = new MockIISFunctions();
            using (var server = CreateServer(mockFunctions))
            {
                StartDummyApplication(server);

                var httpContext = new DefaultHttpContext();
                var request = httpContext.Request;
                request.Headers["Content-Type"] = "application/json";
                request.Method = "GET";
                request.Headers["Test"] = "123";

                await TestHelpers.SendRequest(mockFunctions, httpContext, (IntPtr)server._httpServerHandle);
            }
        }

        [Fact]
        public async Task PostWithBody_WritesCorrectMessage()
        {
            // Flow
            // Add in managed request (HttpRequest) that will be read appropriately.
            var mockFunctions = new MockIISFunctions();
            using (var server = CreateServer(mockFunctions))
            {
                StartDummyApplication(server);

                var httpContext = new DefaultHttpContext();
                var request = httpContext.Request;
                var expected = "application/json";
                request.Headers["Content-Type"] = expected;
                var body = Encoding.ASCII.GetBytes("hello world");
                request.Headers["Content-Length"] = new StringValues($"{body.Length}");
                request.Method = "POST";
                request.Headers["Test"] = "123";
                request.Body = new MemoryStream();

                await TestHelpers.SendRequest(mockFunctions, httpContext, (IntPtr)server._httpServerHandle);
                var buffer = new byte[expected.Length];
                var bodyResponse = await httpContext.Response.Body.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal("application/json", Encoding.ASCII.GetString(buffer));
            }
        }

        private HttpApiTypes.HTTP_REQUEST CreateMockRequest()
        {
            var request = new HttpApiTypes.HTTP_REQUEST();
            return request;
        }

        private static void StartDummyApplication(IServer server)
        {
            server.StartAsync(new DummyApplication(async context =>
            await context.Response.WriteAsync(context.Request.Headers["Content-Type"]))
            , CancellationToken.None);
        }

        private static IISHttpServer CreateServer(MockIISFunctions functions)
        {
            return new IISHttpServer(null, functions);
        }
    }
}
