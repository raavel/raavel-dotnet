using System;
using System.IO;
using System.Net;
using System.Reflection;
using Raavel.Payload;
using Newtonsoft.Json;

namespace Raavel
{
    internal class Notifier
    {
        public const string Name = ".NET Raavel Notifier";
        public static readonly Uri Url = new Uri("https://github.com/raavel/");

        public static readonly string Version =
            Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        private static readonly IWebProxy DetectedProxy = WebRequest.DefaultWebProxy;
        private static readonly JsonSerializerSettings JsonSettings =
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

        private Configuration Config { get; set; }
        private NotificationFactory Factory { get; set; }

        public Notifier(Configuration config)
        {
            Config = config;
            Factory = new NotificationFactory(config);
        }

        public void Send(Event errorEvent)
        {
            var notification = Factory.CreateFromError(errorEvent);
            if (notification != null)
                Send(notification);
        }

        private void Send(Notification notification)
        {
            try
            {
                //  Post JSON to server:
                var request = WebRequest.Create(Config.EndpointUrl);

                request.Method = WebRequestMethods.Http.Post;
                request.ContentType = "application/json";
                request.Proxy = DetectedProxy;

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(notification, JsonSettings);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                }
                request.GetResponse().Close();
            }
            catch (Exception e)
            {
                Logger.Warning("Raavel failed to send error report with exception: " + e.ToString());

                // Deliberate empty catch for now

                // Must never double fault - i.e. can't leak an exception from an exception handler
                // Also the caller might have just wanted to report an "information" Raavel and we
                // certainly don't want to fault then

                // However, "offline" handling should be addressed. I.e. Raavel should be queued
                // until network is available and sent then, see
                // https://github.com/raavel
            }
        }
    }
}
