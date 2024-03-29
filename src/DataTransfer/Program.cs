﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DataTransfer;

namespace DataTransfer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Monitor monitor = new Monitor("BSS.RAManger","BillCall RA Manager 20");
            //monitor.EnableHttp();
            //monitor.EnableException();
            //monitor.EnableGC();
            //monitor.EnableJit(); 
            //monitor.DisableCPU();
            ////monitor.DisableMem();
            //monitor.Record();
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
