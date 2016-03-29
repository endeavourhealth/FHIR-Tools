using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FhirProfilePublisher.Specification;

namespace FhirProfilePublisher.Engine
{
    internal class Pages
    {
        private static readonly string TemplatesResourceLocation = typeof(Styles).Assembly.GetName().Name + ".WebContent.Pages.";
        private static readonly string TemplateRedirectPageFileName = TemplatesResourceLocation + "RedirectPage.html";
        
        private static Pages _instance = null;

        public static Pages Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Pages();

                return _instance;
            }
        }

        private string _templateRedirectPage;

        public string TemplatePage { get; set; }
        public string PageHeader { get; set; }
        public string PageTitleSuffix { get; set; }

        private Pages()
        {
            _templateRedirectPage = ResourceHelper.LoadStringResource(TemplateRedirectPageFileName);
            PageHeader = "FHIR Implementation Guide (Draft)";
            PageTitleSuffix = string.Empty;
        }

        public string GetPage(string title, string content, string version, DateTime dateGenerated)
        {
            return TemplatePage
                    .Replace("%PAGE_HEADER%", PageHeader)
                    .Replace("%TITLE%", title + PageTitleSuffix)
                    .Replace("%CONTENT%", content)
                    .Replace("%VERSION%", version)
                    .Replace("%DATE_GENERATED%", dateGenerated.ToString("dd-MMM-yyyy"));
        }

        public string GetRedirectPage(string url)
        {
            return _templateRedirectPage
                .Replace("%URL%", url);
        }
    }
}
