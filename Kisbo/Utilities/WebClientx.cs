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
            this.m_reader.Close();
            this.m_buffer.Dispose();
            GC.SuppressFinalize(this);
        }

        //private static readonly CookieContainer CookieContainer = new CookieContainer();
        private readonly CancellationToken m_token;

        private readonly MemoryStream m_buffer;
        private readonly StreamReader m_reader;

        public Uri ResponseUri { get; private set; }

        public string DownloadString(Uri uri, string referer = null)
        {
            if (!Request(uri, this.m_buffer, null, null, referer))
                return null;

            return m_reader.ReadToEnd();
        }
        public bool DownloadData(Uri uri, Stream stream, string referer = null)
        {
            return Request(uri, stream, null, null, referer);
        }
        public string UploadData(Uri uri, Stream data, string contentType, string referer = null)
        {
            if (!Request(uri, this.m_buffer, data, contentType, referer))
                return null;

            return m_reader.ReadToEnd();
        }

        private bool Request(Uri uri, Stream buffer, Stream data, string contentType, string referer)
        {
            this.m_buffer.SetLength(0);

            var req = WebRequest.Create(uri) as HttpWebRequest;
            //req.CookieContainer = CookieContainer;
            req.UserAgent = UserAgent;
            req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            if (referer != null)
                req.Referer = referer;
            else if (this.ResponseUri != null)
                req.Referer = this.ResponseUri.AbsoluteUri;

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
                    this.ResponseUri = res.ResponseUri;

                    using (stream = res.GetResponseStream())
                    {
                        while (!this.m_token.IsCancellationRequested && (read = stream.Read(buff, 0, 4096)) > 0)
                            buffer.Write(buff, 0, read);

                        if (this.m_token.IsCancellationRequested)
                            return false;
                    }
                }

                buffer.Position = 0;
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
