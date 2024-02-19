using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace FarmerPro
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            var response = context.Response;
            //allow-origin������* �N�������}�A�άO�o��O�n�]�w�ݹﱵ���H����O�h��
            //response.AddHeader("Access-Control-Allow-Origin", "*");
            //response.AddHeader("X-Frame-Options", "ALLOW-FROM *");

            if (context.Request.HttpMethod == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, DELETE, PATCH, PUT");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept");
                response.AddHeader("Access-Control-Max-Age", "1000000");
                response.End();
            }
        }
    }
}
