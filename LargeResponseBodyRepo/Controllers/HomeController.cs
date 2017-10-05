using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace LargeResponseBodyRepo.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult GetFile()
        {
            var fileResult = File("~/lib/jquery/dist/jquery.js", "text/plain");
            return fileResult;
        }
    }
}
