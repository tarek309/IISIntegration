using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace LargeResponseBodyRepo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.Run(async (context) =>
            {
                try
                {
                    // FileInfo("C:\\Users\\jukotali\\code\\IISIntegration\\LargeResponseBodyRepo\\wwwroot\\lib\\jquery\\dist\\jquery.js")
                    var text = File.ReadAllText("C:\\Users\\jukotali\\code\\IISIntegration\\LargeResponseBodyRepo\\wwwroot\\lib\\jquery\\dist\\jquery.js");
                    context.Response.ContentType = "text/plain";
                    context.Response.ContentLength = text.Length;
                    await context.Response.WriteAsync(text);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            });
        }
    }
}
