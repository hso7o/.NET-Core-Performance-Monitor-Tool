﻿using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;
using System.Dynamic;

namespace DataTransfer
{
    public class Monitor
    {
        public String process;
        public String app;
        public int sampleRate;
        public int sendRate;

        //public Monitor()
        //{
        //    this.process = "MyProcess";
        //    this.app = "UnnamedApp";
        //    this.sampleRate = 1000;
        //    this.sendRate = 5000;
        //}
        //public Monitor(String process = "MyProcess", String app = "UnnamedApp", int sampleRate = 1000, int sendRate = 5000)
        //{
        //    this.process = process;
        //    this.app = app;
        //    this.sampleRate = sampleRate;
        //    this.sendRate = sendRate;
        //}
        //public Monitor(String process = "MyProcess", int sampleRate = 1000, int sendRate = 5000)
        //{
        //    this.process = process;
        //    this.app = "UnnamedApp";
        //    this.sampleRate = sampleRate;
        //    this.sendRate = sendRate;
        //}

        //public Monitor(int sampleRate = 1000, int sendRate = 5000)
        //{
        //    this.process = "MyProcess";
        //    this.app = "UnnamedApp";
        //    this.sampleRate = sampleRate;
        //    this.sendRate = sendRate;
        //}



        /*
         * VARIABLE DECLARATION BLOCK
         */

        // by default, no metric tracking enabled outside of CPU and mem
        private static bool CPUEnabled = true;
        private static bool MemEnabled = true;
        private static bool ContentionEnabled = false;
        private static bool ExceptionEnabled = false;
        private static bool GCEnabled = false;
        private static bool HttpEnabled = false;
        private static bool JitEnabled = false;

        // these methods all turn metric tracking on or off
        public void DisableCPU()
        {
            CPUEnabled = false;
        }
        public void DisableMem()
        {
            MemEnabled = false;
        }
        public void EnableContention()
        {
            ContentionEnabled = true;
        }
        public void EnableException()
        {
            ExceptionEnabled = true;
        }
        public void EnableGC()
        {
            GCEnabled = true;
        }
        public void EnableHttp()
        {
            HttpEnabled = true;
        }
        public void EnableJit()
        {
            JitEnabled = true;
        }

        // fetching properties unique to current data collection session
        public Process myProcess;
        private static String myOS = Environment.OSVersion.ToString();
        private static int processorTotal = Environment.ProcessorCount;
        private static Session instance;
        public PerformanceCounter performanceCounter;


        // variable used to detect data sending for testing purposes
        public static int hold = 0;
        public int getHold()
        {
            return hold;
        }
        public bool running;
        public bool isRunning()
        {
            return running;
        }

        // creates an HTTP client so that server requests can be made
        private HttpClient client;

        // time object used to check if data should be transmitted
        Stopwatch timer;
        // tracks duration of HTTP requests to adjust for delays
        Stopwatch duration;

        // CPU block:
        private static double oldTime = 0;
        private static DateTime oldStamp = DateTime.Now;
        private static double newTime = 0;
        private static DateTime newStamp = DateTime.Now;
        private static double change = 0;
        private static double period = 0;
        public static DateTime CPUMemTime = DateTime.Now;
        // list containing instances of CPU readings
        public List<CPU_Usage> CPUVals;
        public int getCPUCount()
        {
            return CPUVals.Count;
        }

        // Mem block:
        // list containing instances of Memory readings
        public List<Mem_Usage> MemVals;

        // Exception block:
        public List<Exceptions> ExceptionVals;

        // HTTP Request block:
        public List<Http_Request> RequestVals;

        // Contention block:
        public List<Contention> ContentionVals;

        // Garbage Collection block:
        public List<GC> GCVals;

        // Jit block:
        public List<Jit> JitVals;

        // lock object to keep things thread-safe
        Object lockObject;



        /*
         * METHOD DECLARATION BLOCK
         */
        public int Record(MonitorModel monitorModel = null, string url = "http://performanceapi.billcall.net/")  // sets timer that calls Collect every five seconds
        {
            bool isExitedwithError = false;
            if (monitorModel is null)
            {
                monitorModel = new MonitorModel();
                process = monitorModel.ProcessName = "BSS.RAManager";
                monitorModel.EnableContention = false;
                monitorModel.EnableCPU = true;
                monitorModel.EnableException = true;
                monitorModel.EnableGC = false;
                monitorModel.EnableHTTP = false;
                monitorModel.EnableJIT = false;
                monitorModel.EnableMEM = true;
                sampleRate =monitorModel.sampleRate = 1000;
                sendRate =monitorModel.sendRate = 5000;
                app= monitorModel.app = "BillCall RA Manager 20";
            }
            else
            {
                process = monitorModel.ProcessName;
                sampleRate = monitorModel.sampleRate;
                sendRate = monitorModel.sendRate;
                app = monitorModel.app;
            }
            

            if (monitorModel.EnableContention)
                EnableContention();
            
            if (monitorModel.EnableException)
                EnableException();
            
            if (!monitorModel.EnableCPU)
                DisableCPU();
            
            if (!monitorModel.EnableMEM)
                DisableMem();
            
            if (monitorModel.EnableGC)
                EnableGC();
            
            if (monitorModel.EnableHTTP)
                EnableHttp();

            if (monitorModel.EnableJIT)
                EnableJit();

            running = true;
            MemVals = new List<Mem_Usage>();
            CPUVals = new List<CPU_Usage>();
            ExceptionVals = new List<Exceptions>();
            RequestVals = new List<Http_Request>();
            instance = new Session();
            GCVals = new List<GC>();
            JitVals = new List<Jit>();
            client = new HttpClient();
            session = new TraceEventSession(process);
            ContentionVals = new List<Contention>();
            duration = new Stopwatch();
            timer = new Stopwatch();
            lockObject = new object();
            myProcess = Process.GetProcessesByName(monitorModel.ProcessName).FirstOrDefault();
            try
            {
                // sets base address for HTTP requests - won't be hard-coded in future
                client.BaseAddress = new Uri(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.IsNullOrEmpty(ex.Message) ? ex.InnerException != null ? ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message : ex.Message : ex.Message);
                isExitedwithError = true;
            }

            if (isExitedwithError)
                return 0;

            // assign all properties of the current process to the Session class instance
            instance.process = this.process;
            instance.sampleRate = sampleRate;
            instance.sendRate = sendRate;
            instance.processorCount = processorTotal;
            instance.os = myOS;
            instance.application = app;
            Console.WriteLine("Process:-" + myProcess.ProcessName + Environment.NewLine + "O/S:-" + myOS + Environment.NewLine + "ProcessorCount:-" + processorTotal);
            // starts event collection via TraceEvent in separate task if necessary
            if (ContentionEnabled | ExceptionEnabled | GCEnabled | HttpEnabled | JitEnabled)
            {
                Task.Factory.StartNew(() =>
                {
                    TraceEvents();
                });
            }

            // begins loop that samples CPU/mem and issues HTTP requests in separate task
            Task.Factory.StartNew(async () =>
            {
                Console.WriteLine(Environment.NewLine + "Starting CPU & Memory Monitoring:-");
                timer.Start();
                while (true)
                {
                    if (!running)
                    {
                        break;
                    }

                    CPUMemTime = DateTime.Now;
                    if (CPUEnabled)
                    {
                        Console.WriteLine(Environment.NewLine + "CPU Enabled.");
                        //FetchCPU();
                        GetCPUUsageByProcess();
                        Console.WriteLine(Environment.NewLine + "CPU Fetched.");
                    }
                    if (MemEnabled)
                    {
                        Console.WriteLine(Environment.NewLine + "Memory Enabled.");
                        FetchMem();
                        Console.WriteLine(Environment.NewLine + "Memory Fetched.");
                    }

                    // if specified time has passed since HTTP request was made
                    if (timer.ElapsedMilliseconds >= sendRate)
                    {
                        timer.Restart();
                        // creates object that will store all event instances
                        Metric_List list = new Metric_List();

                        lock (lockObject)
                        {
                            list.session = instance;
                            list.cpu = CPUVals;
                            list.mem = MemVals;
                            list.exceptions = ExceptionVals;
                            list.requests = RequestVals;
                            list.contentions = ContentionVals;
                            list.gc = GCVals;
                            list.jit = JitVals;

                            // creates new pointers for metric value collections
                            CPUVals = new List<CPU_Usage>();
                            MemVals = new List<Mem_Usage>();
                            ExceptionVals = new List<Exceptions>();
                            RequestVals = new List<Http_Request>();
                            ContentionVals = new List<Contention>();
                            GCVals = new List<GC>();
                            JitVals = new List<Jit>();
                        }

                        // starts measurement of HTTP request duration for adjustment to task delay
                        duration.Start();
                        hold = 1;

                        String output;
                        try
                        {
                            output = JsonConvert.SerializeObject(list);

                            // escapes string so that JSON object is interpreted as a single string
                            output = JsonConvert.ToString(output);
                            Console.WriteLine(Environment.NewLine + "Output:- " + output);
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress + "api/v1/General");
                            request.Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json");
                            // sends POST request to server, containing JSON representation of events
                            try
                            {
                                HttpResponseMessage response = client.SendAsync(request).Result;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(string.IsNullOrEmpty(ex.Message) ? ex.InnerException != null ? ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message : ex.Message : ex.Message);
                                isExitedwithError = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(string.IsNullOrEmpty(ex.Message) ? ex.InnerException != null ? ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message : ex.Message : ex.Message);
                            isExitedwithError = true;
                            break;
                        }

                        hold = 0;

                        // if HTTP request took longer than sampling rate, skip delay to minizime data loss
                        // else, delay for difference between desired sampling rate and duration of request
                        if (sampleRate < duration.ElapsedMilliseconds)
                        {
                            duration.Reset();
                        }
                        else
                        {
                            await Task.Delay(sampleRate - (int)duration.ElapsedMilliseconds);
                            duration.Reset();
                        }
                    }
                    else           // delay for the amount of time between samples if request wasn't sent
                    {
                        await Task.Delay(sampleRate);
                    }
                }
            });
            return isExitedwithError ? 1 : 0;
        }

        // will attempt to create a TraceEvent session named "MySession"
        TraceEventSession session;
        // lock to protect against attempting to use session while it's stopped
        Object sessionLock = new object();

        public int Pause()  // deconstructor (really a pauser) of a Monitor class instance
        {
            running = false;
            lock (sessionLock)
            {
                // stops TraceEvent session so events aren't being tracked
                session.Stop();
            }

            // sends an HTTP request with the data stored before pausing monitor
            // creates object that will store all event instances
            Metric_List list = new Metric_List();

            lock (lockObject)
            {
                list.session = instance;
                list.cpu = CPUVals;
                list.mem = MemVals;
                list.exceptions = ExceptionVals;
                list.requests = RequestVals;
                list.contentions = ContentionVals;
                list.gc = GCVals;
                list.jit = JitVals;

                // creates new pointers for metric value collections
                CPUVals = new List<CPU_Usage>();
                MemVals = new List<Mem_Usage>();
                ExceptionVals = new List<Exceptions>();
                RequestVals = new List<Http_Request>();
                ContentionVals = new List<Contention>();
                GCVals = new List<GC>();
                JitVals = new List<Jit>();
            }

            String output;
            try
            {
                output = JsonConvert.SerializeObject(list);

                // escapes string so that JSON object is interpreted as a single string
                output = JsonConvert.ToString(output);
                Console.WriteLine(Environment.NewLine + "Output:- " + output);
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, client.BaseAddress + "api/v1/General");
                request.Content = new StringContent(output, System.Text.Encoding.UTF8, "application/json");
                // sends POST request to server, containing JSON representation of events
                try
                {
                    HttpResponseMessage response = client.SendAsync(request).Result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.IsNullOrEmpty(ex.Message) ? ex.InnerException != null ? ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message : ex.Message : ex.Message);
                }
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.IsNullOrEmpty(ex.Message) ? ex.InnerException != null ? ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message : ex.Message : ex.Message);
                return 0;
            }
        }

        private void TraceEvents()
        {
            // track events only if Monitor is running
            if (running)
            {
                using (session)
                {
                    // set up Ctrl-C to stop the session
                    SetupCtrlCHandler(() => { session.Stop(); });

                    // set up parser to read CLR events
                    var clrParser = new ClrTraceEventParser(session.Source);

                    if (ExceptionEnabled)
                    {
                        // subscribe to all exception start events
                        clrParser.ExceptionStart += delegate (ExceptionTraceData data)
                        {
                            // if exception was in user process, add it to list of exceptions
                            if (data.ProcessID == myProcess.Id)
                            {
                                Exceptions e = new Exceptions();
                                e.type = data.ExceptionType;
                                e.timestamp = DateTime.Now;
                                e.App = instance;
                                lock (lockObject)
                                {
                                    ExceptionVals.Add(e);
                                }
                            }
                        };
                    }

                    if (ContentionEnabled)
                    {
                        // subscribe to all contention start events
                        clrParser.ContentionStart += delegate (ContentionTraceData data)
                        {
                            if (data.ProcessID == myProcess.Id)
                            {
                                Contention c = new Contention();
                                c.type = "Start";
                                c.id = data.ActivityID;
                                c.timestamp = DateTime.Now;
                                c.App = instance;
                                lock (lockObject)
                                {
                                    ContentionVals.Add(c);
                                }
                            }
                        };
                        // subscribe to all contention stop events
                        clrParser.ContentionStop += delegate (ContentionTraceData data)
                        {
                            if (data.ProcessID == myProcess.Id)
                            {
                                Contention c = new Contention();
                                c.type = "Stop";
                                c.id = data.ActivityID;
                                c.timestamp = DateTime.Now;
                                c.App = instance;
                                lock (lockObject)
                                {
                                    ContentionVals.Add(c);
                                }
                            }
                        };
                    }

                    if (GCEnabled)
                    {
                        // subscribe to all GC start events
                        clrParser.GCStart += delegate (GCStartTraceData data)
                        {
                            if (data.ProcessID == myProcess.Id)
                            {
                                GC gc = new GC();
                                gc.type = "Start";
                                gc.timestamp = DateTime.Now;
                                gc.id = data.ThreadID;
                                gc.App = instance;
                                lock (lockObject)
                                {
                                    GCVals.Add(gc);
                                }
                            }
                        };
                        // subscribe to all GC stop events
                        clrParser.GCStop += delegate (GCEndTraceData data)
                        {
                            if (data.ProcessID == myProcess.Id)
                            {
                                GC gc = new GC();
                                gc.type = "Stop";
                                gc.timestamp = DateTime.Now;
                                gc.id = data.ThreadID;
                                gc.App = instance;
                                lock (lockObject)
                                {
                                    GCVals.Add(gc);
                                }
                            }
                        };
                        // subscribe to all GC memory allocation ticks
                        clrParser.GCAllocationTick += delegate (GCAllocationTickTraceData data)
                        {
                            if (data.ProcessID == myProcess.Id)
                            {
                                GC gc = new GC();
                                gc.type = "Allocation Tick";
                                gc.timestamp = DateTime.Now;
                                gc.id = data.ThreadID;
                                gc.App = instance;
                                lock (lockObject)
                                {
                                    GCVals.Add(gc);
                                }
                            }
                        };
                        // subscribe to all creations of concurrent threads for GC
                        clrParser.GCCreateConcurrentThread += delegate (GCCreateConcurrentThreadTraceData data)
                        {
                            if (data.ProcessID == myProcess.Id)
                            {
                                GC gc = new GC();
                                gc.type = "Create Concurrent Thread";
                                gc.timestamp = DateTime.Now;
                                gc.id = data.ThreadID;
                                gc.App = instance;
                                lock (lockObject)
                                {
                                    GCVals.Add(gc);
                                }
                            }
                        };
                        // subscribe to all restart stops
                        clrParser.GCRestartEEStop += delegate (GCNoUserDataTraceData data)
                        {
                            if (data.ProcessID == myProcess.Id)
                            {
                                GC gc = new GC();
                                gc.type = "Restart EE Stop";
                                gc.timestamp = DateTime.Now;
                                gc.id = data.ThreadID;
                                gc.App = instance;
                                lock (lockObject)
                                {
                                    GCVals.Add(gc);
                                }
                            }
                        };
                        // subscribe to all suspension starts
                        clrParser.GCSuspendEEStart += delegate (GCSuspendEETraceData data)
                        {
                            if (data.ProcessID == myProcess.Id)
                            {
                                GC gc = new GC();
                                gc.type = "Suspend EE Start";
                                gc.timestamp = DateTime.Now;
                                gc.id = data.ThreadID;
                                gc.App = instance;
                                lock (lockObject)
                                {
                                    GCVals.Add(gc);
                                }
                            }
                        };
                        // subscribe to all concurrent thread terminations
                        clrParser.GCTerminateConcurrentThread += delegate (GCTerminateConcurrentThreadTraceData data)
                        {
                            if (data.ProcessID == myProcess.Id)
                            {
                                GC gc = new GC();
                                gc.type = "Concurrent Thread Termination";
                                gc.timestamp = DateTime.Now;
                                gc.id = data.ThreadID;
                                gc.App = instance;
                                lock (lockObject)
                                {
                                    GCVals.Add(gc);
                                }
                            }
                        };
                        // subscribe to all GC triggers
                        clrParser.GCTriggered += delegate (GCTriggeredTraceData data)
                        {
                            if (data.ProcessID == myProcess.Id)
                            {
                                GC gc = new GC();
                                gc.type = "Triggered";
                                gc.timestamp = DateTime.Now;
                                gc.id = data.ThreadID;
                                gc.App = instance;
                                lock (lockObject)
                                {
                                    GCVals.Add(gc);
                                }
                            }
                        };
                    }

                    if (JitEnabled)
                    {
                        // subscribe to all Jit start events
                        clrParser.MethodJittingStarted += delegate (MethodJittingStartedTraceData data)
                        {
                            if (data.ProcessID == myProcess.Id)
                            {
                                Jit j = new Jit();
                                j.method = data.MethodName;
                                j.timestamp = DateTime.Now;
                                j.App = instance;
                                lock (lockObject)
                                {
                                    JitVals.Add(j);
                                }
                            }
                        };
                    }

                    if (HttpEnabled)
                    {
                        // subscribe to all dynamic events (used for HTTP request event tracking)
                        session.Source.Dynamic.All += delegate (TraceEvent data)
                        {
                            Console.WriteLine(Environment.NewLine + "ProcessID:-" + data.ProcessID);
                            if (data.ProcessID == myProcess.Id && data.EventName == "Request/Start")
                            {
                                Http_Request request = new Http_Request();
                                request.type = "Start";
                                request.timestamp = DateTime.Now;
                                request.activityID = data.ActivityID;
                                request.App = instance;
                                request.method = data.PayloadString(0);
                                request.path = data.PayloadString(1);
                                lock (lockObject)
                                {
                                    RequestVals.Add(request);
                                }
                            }
                            else if (data.ProcessID == myProcess.Id && data.EventName == "Request/Stop")
                            {
                                Http_Request request = new Http_Request();
                                request.type = "Stop";
                                request.timestamp = DateTime.Now;
                                request.activityID = data.ActivityID;
                                request.App = instance;
                                lock (lockObject)
                                {
                                    RequestVals.Add(request);
                                }
                            }
                        };
                    }

                    lock (sessionLock)
                    {
                        // set up providers for events using GUIDs - first EnableProvider command is to receive activity IDs for associated events
                        session.EnableProvider(new Guid("2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5"), TraceEventLevel.Informational, 0x80);
                        var AspSourceGuid = TraceEventProviders.GetEventSourceGuidFromName("Microsoft-AspNetCore-Hosting");
                        session.EnableProvider(AspSourceGuid);
                        session.EnableProvider(ClrTraceEventParser.ProviderGuid, TraceEventLevel.Verbose, ( 0x8000 | 0x4000 | 0x1 | 0x10 ));
                        session.Source.Process();    // call the callbacks for each event
                    }
                }
            }
        }


        private void GetCPUUsageByProcess()
        {
            try
            {
                if (CPUEnabled)
                {
                    Console.WriteLine(Environment.NewLine + "Starting CPU Monitoring:-");
                    // clear the process' cached information
                    myProcess.Refresh();
                    CPU_Usage cpu = new CPU_Usage();
                    string name = string.Empty;
                    //myProcess
                    foreach (var inst in new PerformanceCounterCategory("Process").GetInstanceNames())
                    {
                        if (inst.StartsWith(myProcess.ProcessName))
                        {
                            using (var processId = new PerformanceCounter("Process", "ID Process", inst, true))
                            {
                                if (myProcess.Id == (int)processId.RawValue)
                                {
                                    name = inst;
                                    break;
                                }
                            }
                        }
                    }

                    var cpuUsage = new PerformanceCounter("Process", "% Processor Time", name, true);

                    // Getting first initial values
                    cpuUsage.NextValue();

                    dynamic result = new ExpandoObject();

                    // If system has multiple cores, that should be taken into account
                    result.CPU = Math.Round(cpuUsage.NextValue() / Environment.ProcessorCount, 2);
                    cpu.usage = result.CPU;
                    cpu.timestamp = CPUMemTime;
                    //cpu.AppId = instance.Id;
                    lock (lockObject)
                    {
                        CPUVals.Add(cpu);
                    }
                    Console.WriteLine(Environment.NewLine + "CPU APP ID:- " + cpu.AppId + "\t CPU TimeStamp" + cpu.timestamp + "\t CPU Usage" + cpu.usage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.IsNullOrEmpty(ex.Message) ? ex.InnerException != null ? ex.InnerException.InnerException != null ? ex.InnerException.InnerException.Message : ex.InnerException.Message : ex.Message : ex.Message);
            }

        }

        private void FetchCPU()  // calculates CPU usage
        {
            if (CPUEnabled)
            {
                Console.WriteLine(Environment.NewLine + "Starting CPU Monitoring:-");
                // clear the process' cached information
                myProcess.Refresh();
                Console.WriteLine(Environment.NewLine + "Process refreshed.");
                Console.WriteLine(Environment.NewLine + "Process Name:- " + myProcess.ProcessName);
                Console.WriteLine(Environment.NewLine + "Process Name:- " + myProcess.UserProcessorTime);
                Console.WriteLine(Environment.NewLine + "Process Started on:- " + myProcess.StartTime);
                Console.WriteLine(Environment.NewLine + "Process time:- " + myProcess.TotalProcessorTime.TotalHours);
                //myProcess.Start();
                CPU_Usage cpu = new CPU_Usage();
                //myProcess
                newTime = myProcess.TotalProcessorTime.TotalMilliseconds;

                Console.WriteLine(Environment.NewLine + "Process Total time:- " + newTime);
                newStamp = DateTime.Now;
                // calculates CPU usage since last measurement
                change = newTime - oldTime;
                Console.WriteLine(Environment.NewLine + "Change time:- " + change);
                // calculates time between CPU measurements
                period = newStamp.Subtract(oldStamp).TotalMilliseconds;
                oldTime = newTime;
                oldStamp = newStamp;
                Console.WriteLine(Environment.NewLine + "Time period:- " + period);
                // finds CPU usage for process as a percentage of total CPU time across the machine
                double use = ( change / ( period * processorTotal ) * 100.0 );
                // if sampling error causes CPU to read over 100, set to 100
                Console.WriteLine(Environment.NewLine + "Cpu Usage:- " + use);
                if (use > 100.0)
                {
                    use = 100.0;
                }
                cpu.usage = use;
                cpu.timestamp = CPUMemTime;
                lock (lockObject)
                {
                    CPUVals.Add(cpu);
                }
                Console.WriteLine(Environment.NewLine + "CPU APP ID:- " + cpu.AppId + "\t CPU TimeStamp" + cpu.timestamp + "\t CPU Usage" + cpu.usage);
            }
        }

        private void FetchMem()  // fetches Memory usage
        {
            if (MemEnabled)
            {
                Console.WriteLine(Environment.NewLine + "Starting Memory Monitoring:-");
                // clear the process' cached information
                myProcess.Refresh();
                Mem_Usage mem = new Mem_Usage();
                mem.usage = myProcess.WorkingSet64;
                mem.timestamp = CPUMemTime;
                lock (lockObject)
                {
                    MemVals.Add(mem);
                }
            }
        }

        // setup to stop TraceEvent session upon application termination
        private static bool s_bCtrlCExecuted;
        private static ConsoleCancelEventHandler s_CtrlCHandler;
        private static void SetupCtrlCHandler(Action action)
        {
            s_bCtrlCExecuted = false;
            // uninstall previous handler
            if (s_CtrlCHandler != null)
                Console.CancelKeyPress -= s_CtrlCHandler;

            s_CtrlCHandler =
                (object sender, ConsoleCancelEventArgs cancelArgs) =>
                {
                    if (!s_bCtrlCExecuted)
                    {
                        s_bCtrlCExecuted = true;    // ensure non-reentrant

                        Console.WriteLine("Stopping monitor");

                        action();                   // execute custom action

                        // terminate normally (i.e. when the monitoring tasks complete b/c we've stopped the sessions)
                        cancelArgs.Cancel = true;
                    }
                };
            Console.CancelKeyPress += s_CtrlCHandler;
        }
    }
}
