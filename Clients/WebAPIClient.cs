using System;
using System.Web;
using System.Web.Http.Filters;

namespace Raavel.Clients
{
    public static class WebAPIClient
    {
        [AttributeUsageAttribute(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
        public sealed class RaavelExceptionHandler : ExceptionFilterAttribute
        {
            internal RaavelExceptionHandler()
            {
            }

            public override void OnException(HttpActionExecutedContext context)
            {
                base.OnException(context);
                if (context == null || context.Exception == null)
                    return;

                if (Config.AutoNotify)
                    Client.Notify(context.Exception);
            }
        }

        public static Configuration Config;
        private static BaseClient Client;

        static WebAPIClient()
        {
            Client = new BaseClient(ConfigurationStorage.ConfigSection.Settings);
            Config = Client.Config;
        }

        public static void Start()
        {

        }

        public static RaavelExceptionHandler ErrorHandler()
        {
            return new RaavelExceptionHandler();
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
    }
}
