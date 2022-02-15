using System;
using System.Collections.Generic;
using System.Text;

namespace DataTransfer
{
    public class MonitorModel
    {
        public string ProcessName { get; set; }
        public string app { get; set; }
        public bool EnableGC { get; set; }
        public bool EnableHTTP { get; set; }
        public bool EnableJIT { get; set; }
        public bool EnableContention { get; set; }
        public bool EnableException { get; set; }
        public bool EnableMEM { get; set; }
        public bool EnableCPU { get; set; }
        public int sampleRate { get; set; }
        public int sendRate { get; set; }
        public string url { get; set; }

    }
}
