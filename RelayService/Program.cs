using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;
using System.Net;
using System.Text;

namespace RelayService
{
    public class Program
    {

        public static void Main(string[] args)
        {
            RunAsync(args).GetAwaiter().GetResult();
        }

        static async Task RunAsync(string[] args)
        {
            var ns = "<your namespace url from shared access policy- just the fdqn no protocols>";
            var hc = "<your hybrid connection name - can be found in portal under the relay>"; //This is the name for the current store - one per store to be cofigured when running the service in the store 
            var keyname = "<shared access key name such as RootManageSharedAccessKey>";
            var key = "<key value from the shared access policies>";

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyname, key);
            var listener = new HybridConnectionListener(new Uri(string.Format("sb://{0}/{1}", ns, hc)), tokenProvider);

            // Subscribe to the status events.
            listener.Connecting += (o, e) => { Console.WriteLine("Connecting"); };
            listener.Offline += (o, e) => { Console.WriteLine("Offline"); };
            listener.Online += (o, e) => { Console.WriteLine("Online"); };

            // Provide an HTTP request handler
            listener.RequestHandler = (context) =>
            {
                // Do something with context.Request.Url, HttpMethod, Headers, InputStream...
                var contextRequest = context.Request;
                System.IO.Stream body = contextRequest.InputStream;
                System.IO.StreamReader reader = new System.IO.StreamReader(body, Encoding.UTF8);
                string targetUrl = reader.ReadToEnd();
                body.Close();

                context.Response.StatusCode = HttpStatusCode.OK;
                context.Response.StatusDescription = "OK";
                string result = "";

                using (var sw = new StreamWriter(context.Response.OutputStream))
                {

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(targetUrl);
                    request.Method = "Get";
                    request.KeepAlive = true;
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    using (System.IO.StreamReader sr = new System.IO.StreamReader(response.GetResponseStream()))
                    {
                        result = sr.ReadToEnd();
                    }

                    Console.WriteLine(result);
                    sw.WriteLine(result);
                }

                context.Response.Close();
            };
            // Opening the listener establishes the control channel to
            // the Azure Relay service. The control channel is continuously 
            // maintained, and is reestablished when connectivity is disrupted.
            await listener.OpenAsync();
            Console.WriteLine("Server listening");

            // Start a new thread that will continuously read the console.
            await Console.In.ReadLineAsync();

            // Close the listener
            await listener.CloseAsync();
        }
    }
}

