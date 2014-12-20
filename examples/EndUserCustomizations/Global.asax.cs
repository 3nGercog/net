﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using jsreport.Client;
using jsreport.Embedded;

namespace EndUserCustomizations
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static IEmbeddedReportingServer EmbeddedReportingServer { get; set; }

        protected void Application_Start()
        {
            EmbeddedReportingServer = new EmbeddedReportingServer()
            {
                RelativePathToServer = "../App_Data"
            };
            EmbeddedReportingServer.StartAsync().Wait();
            JsReportWebHandler.ReportingService = EmbeddedReportingServer.ReportingService;

            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}