using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Raavel.Clients
{
    class HttpModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.Error += SendError;
        }

        public void Dispose()
        {
        }

        private void SendError(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var lastError = application.Server.GetLastError();

            new BaseClient().Notify(Unwrap(lastError));
        }

        private Exception Unwrap(Exception exception)
        {
            if (exception is HttpUnhandledException)
            {
                return exception.GetBaseException();
            }

            return exception;
        }
    }
}
