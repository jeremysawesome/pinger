namespace Pinger
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.NetworkInformation;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using Quartz;
    using Quartz.Impl;


    internal class Pinger
    {
        class Host
        {
            public string Name { get; set; }
            public string Address { get; set; }

            public int Attempts { get; set; }
            public int Successes { get; set; }
            public int Fails { get; set; }
            public int Exceptions { get; set; }
        }

        static readonly Dictionary<string, Host> Hosts = new Dictionary<string, Host>
        {
            { "127.0.0.1", new Host { Address = "127.0.0.1", Name="localhost" } },
            { "192.168.0.1", new Host { Address = "192.168.0.1", Name="Wireless Router" } },
            { "192.168.0.105", new Host { Address = "192.168.0.105", Name="WD Mycloud (Intranet)" } },
            { "8.8.8.8", new Host { Address = "8.8.8.8", Name="External (Internet)" } },
        };

        static void PingHost(Host host)
        {
            // increment the host attempts
            host.Attempts++;

            // Ping Reply class
            //https://msdn.microsoft.com/en-us/library/system.net.networkinformation.ping%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
            var ping = new Ping();
            var waiter = new AutoResetEvent(false);
            ping.PingCompleted += PingCompletedCallback;


            // Create a buffer of 32 bytes of data to be transmitted.  
            const string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            var buffer = Encoding.ASCII.GetBytes(data);

            // Wait 12 seconds for a reply.  
            const int timeout = 12000;

            // Set options for transmission:  
            // The data can go through 64 gateways or routers  
            // before it is destroyed, and the data packet  
            // cannot be fragmented.  
            var options = new PingOptions(64, true);

            // Send the ping asynchronously.  
            // Use the waiter as the user token.  
            // When the callback completes, it can wake up this thread.  
            try
            {
                ping.SendAsync(host.Address, timeout, buffer, options, waiter);
            }
            catch
            {
                host.Exceptions++;
            }

        }

        static void Main()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Schedule();
        }

        static void Log()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = appData + "\\Pinger\\" + "\\pinger_log.txt";

            if (!File.Exists(path))
            {
                Directory.CreateDirectory(appData + "\\Pinger");
            }

            // build log
                var hostInfos = new List<string>();
            foreach (var value in Hosts)
            {
                var host = value.Value;
                var upPercent = ((double)host.Successes / host.Attempts) * 100f;
                hostInfos.Add( String.Format( "[{6:s}] Host: {0} was up {1} percent of the time. Attempts: {2}, Success: {3}, Exceptions: {4}, Failures: {5}",
                    host.Name, upPercent, host.Attempts, host.Successes, host.Exceptions, host.Fails, DateTime.Now) );
            }

            File.AppendAllLines(path, hostInfos);
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            Log();
        }

        static void PingCompletedCallback(object sender, PingCompletedEventArgs eventArgs)
        {
            if (eventArgs.Cancelled || eventArgs.Error != null)
            {
                // Let the main thread resume.   
                // UserToken is the AutoResetEvent object that the main thread      
                // is waiting for.  
                ((AutoResetEvent) eventArgs.UserState).Set();
            }

            var reply = eventArgs.Reply;
            Host host;
            if (Hosts.TryGetValue(reply.Address.ToString(), out host))
            {
                if (reply.Status == IPStatus.Success) host.Successes++;
                else host.Fails++;
            }

            // Let the main thread resume.  
            ((AutoResetEvent) eventArgs.UserState).Set();
        }

        static void Schedule()
        {
            var scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Start();

            var job = JobBuilder.Create<JobRunner>()
                .WithIdentity("pingerJob")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity("pingerJob")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(10)
                    .RepeatForever())
                .Build();

            scheduler.ScheduleJob(job, trigger);
        }

        static int runs = 0;

        static void Run()
        {
            foreach (var host in Hosts)
            {
                PingHost(host.Value);
            }
            runs++;

            if (runs % 20 != 0) return;
            runs = 0;
            Log();
        }

        class JobRunner : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                Run();
            }
        }
    }
}