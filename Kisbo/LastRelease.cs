using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;

namespace Kisbo
{
    internal static class LastRelease
    {
        [DataContract]
        public class LastestRealease
        {
            public static LastestRealease Parse(Stream stream)
            {
                var serializer = new DataContractJsonSerializer(typeof(LastestRealease));
                return (LastestRealease)serializer.ReadObject(stream);
            }

            [DataMember(Name = "tag_name")]
            public string TagName { get; set; }

            [DataMember(Name = "html_url")]
            public string HtmlUrl { get; set; }

            [DataMember(Name = "assets")]
            public Asset[] Assets { get; set; }

            [DataContract]
            public class Asset
            {
                [DataMember(Name = "browser_download_url")]
                public string BrowserDownloadUrl { get; set; }
            }
        }
        public static LastestRealease CheckNewVersion()
        {
            try
            {
                LastestRealease last;

                var req = HttpWebRequest.Create("https://api.github.com/repos/RyuaNerin/Kisbo/releases/latest") as HttpWebRequest;
                req.Timeout = 5000;
                req.UserAgent = "Kisbo";
                using (var res = req.GetResponse())
                using (var stream = res.GetResponseStream())
                    last = LastestRealease.Parse(stream);

                return new Version(last.TagName) > new Version(Application.ProductVersion) ? last : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
