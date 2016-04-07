using System;

#if !NET35
// Tasks for Async versions of Notify()
using System.Threading.Tasks;

// Provide exception attribute for global filters (> .NET 4.0 )
using System.Web.Mvc;
#endif

namespace Raavel.Clients
{
    public static class WebMVCClient
    {
        public static Configuration Config;
        private static BaseClient Client;

        static WebMVCClient()
        {
            Client = new BaseClient(ConfigurationStorage.ConfigSection.Settings);
            Config = Client.Config;
        }

        public static void Start()
        {

        }

        public static void Notify(Exception error)
        {
            Client.Notify(error);
        }

        public static void Notify(Exception error, Metadata metadata)
        {
            Client.Notify(error, metadata);
        }

        public static void Notify(Exception error, Severity severity)
        {
            Client.Notify(error, severity);
        }

        public static void Notify(Exception error, Severity severity, Metadata metadata)
        {
            Client.Notify(error, severity, metadata);
        }

#if !NET35
        public static Task NotifyAsync(Exception error)
        {
            return Client.NotifyAsync(error);
        }

        public static Task NotifyAsync(Exception error, Metadata metadata)
        {
            return Client.NotifyAsync(error, metadata);
        }

        public static Task NotifyAsync(Exception error, Severity severity)
        {
            return Client.NotifyAsync(error, severity);
        }

        public static Task NotifyAsync(Exception error, Severity severity, Metadata metadata)
        {
            return Client.NotifyAsync(error, severity, metadata);
        }
#endif

#if !NET35
        /// <summary>
        /// Exception attribute to automatically handle errors when registered (requires > .NET 4.0)
        /// </summary>
        [AttributeUsageAttribute(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
        public sealed class BugsnagExceptionHandler : HandleErrorAttribute
        {
            internal BugsnagExceptionHandler()
            {
            }

            public override void OnException(ExceptionContext filterContext)
            {
                if (filterContext == null || filterContext.Exception == null)
                    return;

                if (Config.AutoNotify)
                    Client.Notify(filterContext.Exception);
            }
        }

        public static BugsnagExceptionHandler ErrorHandler()
        {
            return new BugsnagExceptionHandler();
        }
#endif
    }
}
