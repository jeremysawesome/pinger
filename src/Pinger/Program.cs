using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

namespace Pinger
{
    class Pinger
    {

        static void PingHost(string host)
        {
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
            ping.SendAsync(host, timeout, buffer, options, waiter);
        }

        static void Main()
        {
            var hosts = new List<string>
            {
                "8.8.8.8",
                "192.168.86.1"
            };

            foreach (var host in hosts)
            {
                PingHost(host);  
            }
            Console.WriteLine("Ping example completed.");
            Console.Read();
        }

        static void PingCompletedCallback( object sender, PingCompletedEventArgs eventArgs )
        {
            // If the operation was canceled, display a message to the user.  
            if (eventArgs.Cancelled)
            {
                Console.WriteLine("Ping canceled.");

                // Let the main thread resume.   
                // UserToken is the AutoResetEvent object that the main thread      
                // is waiting for.  
                ((AutoResetEvent)eventArgs.UserState).Set();
            }

            // If an error occurred, display the exception to the user.  
            if (eventArgs.Error != null)
            {
                Console.WriteLine("Ping failed:");
                Console.WriteLine(eventArgs.Error.ToString());

                // Let the main thread resume.   
                ((AutoResetEvent)eventArgs.UserState).Set();
            }

            var reply = eventArgs.Reply;

            DisplayReply(reply);

            // Let the main thread resume.  
            ((AutoResetEvent)eventArgs.UserState).Set();
        }

        static void DisplayReply(PingReply reply)
        {
            if (reply == null)
                return;

            Console.WriteLine("ping status: {0}", reply.Status);
            if (reply.Status != IPStatus.Success) return;

            Console.WriteLine("Address: {0}", reply.Address.ToString());
            Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
            Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
            Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
            Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
        }
    }
}
