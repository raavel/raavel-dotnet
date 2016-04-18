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

        private static void SendError(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var ex = application.Server.GetLastError();

            if (ex is HttpUnhandledException)
                ex = ex.InnerException;

            new BaseClient().Notify(ex);
        }
    }
}
