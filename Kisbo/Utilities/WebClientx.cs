using System;
using System.IO;
using System.Net;
using System.Threading;

namespace Kisbo.Utilities
{
    internal class WebClientx : IDisposable
    {
        public const string UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0";

        public WebClientx(CancellationToken token)
        {
            this.m_token = token;
            
            this.m_buffer = new MemoryStream(40960);
            this.m_reader = new StreamReader(m_buffer);
        }
        public void Dispose()
        {
            this.m_buffer.Dispose();
            GC.SuppressFinalize(this);
        }

        //private static readonly CookieContainer CookieContainer = new CookieContainer();
        private readonly CancellationToken m_token;

        private readonly MemoryStream m_buffer;
        private readonly StreamReader m_reader;

        public Uri LastUrl { get; private set; }

        public string DownloadString(Uri uri)
        {
            if (!Request(uri, null, null))
                return null;

            return m_reader.ReadToEnd();
        }
        public string UploadData(Uri uri, Stream data, string contentType)
        {
            if (!Request(uri, data, contentType))
                return null;

            return m_reader.ReadToEnd();
        }

        private bool Request(Uri uri, Stream data, string contentType)
        {
            this.m_buffer.SetLength(0);

            var req = WebRequest.Create(uri) as HttpWebRequest;
            //req.AllowAutoRedirect = false;
            //req.CookieContainer = CookieContainer;
            req.UserAgent = UserAgent;
            req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            if (this.LastUrl != null)
                req.Referer = this.LastUrl.AbsoluteUri;

            Stream stream;
            int read;
            var buff = new byte[4096];

            if (data != null)
            {
                req.Method = "POST";
                req.ContentType = contentType;

                stream = req.GetRequestStream();

                while (!this.m_token.IsCancellationRequested && (read = data.Read(buff, 0, 4096)) > 0)
                    stream.Write(buff, 0, read);

                if (this.m_token.IsCancellationRequested)
                    return false;
            }
            else
                req.Method = "GET";

            try
            {
                using (var res = req.GetResponse())
                {
                    this.LastUrl = res.ResponseUri;

                    using (stream = res.GetResponseStream())
                    {
                        while (!this.m_token.IsCancellationRequested && (read = stream.Read(buff, 0, 4096)) > 0)
                            this.m_buffer.Write(buff, 0, read);

                        if (this.m_token.IsCancellationRequested)
                            return false;
                    }
                }

                this.m_buffer.Position = 0;
                return true;
            }
            catch (WebException e)
            {
                if (e.Response != null)
                    e.Response.Close();
            }
            catch
            {
            }

            return false;
        }
    };
}
