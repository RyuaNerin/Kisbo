using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Kisbo.Utilities
{
    public static class BypassHttp
    {        
        private const string UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36";

        private readonly static LockByKey<string> Lock = new LockByKey<string>();
        private readonly static CookieContainer CookieContainer = new CookieContainer();
        private readonly static Jint.Engine JSEngine = new Jint.Engine();

        public static bool GetResponse(Uri uri, Stream stream, CancellationToken token, bool passException = false)
        {
            if (!passException)
                Lock.Wait(uri.Host);

            var req = WebRequest.Create(uri) as HttpWebRequest;
            req.Referer = new Uri(uri, "/").AbsoluteUri;
            req.UserAgent = UserAgent;

            lock (CookieContainer)
                req.Headers.Set("cookie", CookieContainer.GetCookieHeader(uri));

            HttpWebResponse res;
            try
            {
                using (res = req.GetResponse() as HttpWebResponse)
                {
                    if (res.ContentType.IndexOf("image", StringComparison.OrdinalIgnoreCase) == -1)
                        return false;

                    return GetResponseBytes(res, stream, token);
                }
            }
            catch (WebException e)
            {
                if (passException)
                {
                    e.Response.Close();
                    return false;
                }

                string body;
                string cookies;
                using (res = e.Response as HttpWebResponse)
                {
                    if (res.Server.IndexOf("cloudflare", StringComparison.OrdinalIgnoreCase) == -1)
                        return false;

                    using (var mem = new MemoryStream(10240))
                    {
                        if (!GetResponseBytes(res, mem, token))
                            return false;

                        mem.Position = 0;
                        body = new StreamReader(mem).ReadToEnd();
                    }

                    cookies = res.Headers["set-cookie"];
                }

                return BypassCloudFlare(body, cookies, uri, stream, token);
            }
            catch
            {
            }

            return false;
        }

        private static bool GetResponseBytes(HttpWebResponse res, Stream stream, CancellationToken token)
        {
            using (var http = res.GetResponseStream())
            {
                int rd;
                var buff = new byte[40960]; // 40k

                while (!token.IsCancellationRequested && (rd = http.Read(buff, 0, 40960)) > 0)
                    stream.Write(buff, 0, rd);
            }

            stream.Flush();

            return !token.IsCancellationRequested;
        }

        private readonly static RegexOptions RegexDefaultOption = RegexOptions.Compiled | RegexOptions.IgnoreCase;
        private readonly static Regex regCF_challenge       = new Regex("name=\"jschl_vc\" value=\"(\\w+)\"", RegexDefaultOption);
        private readonly static Regex regCF_challengepass   = new Regex("name=\"pass\" value=\"(.+?)\"", RegexDefaultOption);
        private readonly static Regex regCG_script          = new Regex(@"setTimeout\(function\(\){\s+(var t,r,a,f.+?\r?\n[\s\S]+?a\.value =.+?)\r?\n", RegexDefaultOption);
        private readonly static Regex regCG_scriptReplace0  = new Regex(@"a\.value =(.+?) \+ .+?;", RegexDefaultOption);
        private readonly static Regex regCG_scriptReplace1  = new Regex(@"\s{3,}[a-z](?: = |\.).+", RegexDefaultOption);
        private readonly static Regex regCG_scriptReplace2  = new Regex(@"[\n\\']", RegexDefaultOption);
        private static bool BypassCloudFlare(string body, string setCookieHeader, Uri uri, Stream stream, CancellationToken token)
        {
            // http://stackoverflow.com/questions/32425973/how-can-i-get-html-from-page-with-cloudflare-ddos-portection#2

            using (Lock.GetLock(uri.Host))
            {
                // 기존 쿠키 추가
                lock (CookieContainer)
                    CookieContainer.Add(StringToCookies(setCookieHeader, uri.Host));

                Uri newUri;

                var challenge = regCF_challenge.Match(body).Groups[1].Value;
                var challenge_pass = regCF_challengepass.Match(body).Groups[1].Value;

                var builder = regCG_script.Match(body).Groups[1].Value;
                builder = regCG_scriptReplace0.Replace(builder, "$1");
                builder = regCG_scriptReplace1.Replace(builder, "");
                builder = regCG_scriptReplace2.Replace(builder, "");

                var solved = Convert.ToInt64(JSEngine.Execute(builder).GetCompletionValue().ToObject());
                solved += uri.Host.Length;

                if (!Sleep(3000, token))
                    return false;

                var cookie_url = string.Format("{0}://{1}/cdn-cgi/l/chk_jschl", uri.Scheme, uri.Host);
                var uri_builder = new UriBuilder(cookie_url);

                var query = new Dictionary<string, object>();
                query["jschl_vc"] = challenge;
                query["pass"] = challenge_pass;
                query["jschl_answer"] = solved;
                uri_builder.Query = ToString(query);

                newUri = uri_builder.Uri;

                var req = WebRequest.Create(newUri) as HttpWebRequest;
                req.AllowAutoRedirect = false;
                lock (CookieContainer)
                    req.Headers.Set("cookie", CookieContainer.GetCookieHeader(uri));
                req.Referer = uri.AbsoluteUri;
                req.UserAgent = UserAgent;

                try
                {
                    using (var res = req.GetResponse() as HttpWebResponse)
                    {
                        setCookieHeader = res.Headers["Set-Cookie"];
                        if (string.IsNullOrWhiteSpace(setCookieHeader))
                            return false;

                        lock (CookieContainer)
                            CookieContainer.Add(StringToCookies(res.Headers["Set-Cookie"], uri.Host));
                    }
                }
                catch (WebException e)
                {
                    e.Response.Close();
                    return false;
                }
                catch
                {
                    return false;
                }

                return GetResponse(uri, stream, token, true);
            }
        }

        private static bool Sleep(int milliseconds, CancellationToken token)
        {
            var endTime = DateTime.Now.AddMilliseconds(milliseconds);
            do 
            {
                Thread.Sleep(50);
            } while (DateTime.Now < endTime && !token.IsCancellationRequested);

            return !token.IsCancellationRequested;
        }

        private static CookieCollection StringToCookies(string setCookies, string domain)
        {
            var cc = new CookieCollection();
            if (string.IsNullOrWhiteSpace(setCookies))
                return cc;

            int i, k;
            string[] kvs;
            string key, val;

            foreach (var str in SplitEachCookie(setCookies))
            {
                var cookie = new Cookie();
                cookie.Domain = domain;
                cookie.Path = "/";

                kvs = str.Split(';');
                for (i = 0; i < kvs.Length; ++i)
                {
                    k   = kvs[i].IndexOf('=');
                    if (k == -1)
                    {
                        key = kvs[i];
                        val = null;
                    }
                    else
                    {
                        key = kvs[i].Substring(0, k);
                        val = kvs[i].Substring(k + 1);
                    }

                    if (i == 0)
                    {
                        cookie.Name = key;
                        cookie.Value = val;
                    }
                    else
                    {
                        key = key.ToUpperInvariant();
                        if (string.IsNullOrWhiteSpace(val))
                        {
                            switch (key)
                            {
                            case "HTTPONLY":
                                cookie.HttpOnly = true;
                                break;
                            case "DISCARD":
                                cookie.Discard = true;
                                break;
                            case "SECURE":
                                cookie.Secure = true;
                                break;
                            }
                        }
                        else
                        {
                            switch (key)
                            {
                            case "EXPIRES":
                                if (cookie.Expires == DateTime.MinValue)
                                    cookie.Expires = ParseCookieExpires(val);
                                break;

                            case "MAX-AGE":
                                if (cookie.Expires == DateTime.MinValue)
                                    cookie.Expires = cookie.TimeStamp.AddSeconds(int.Parse(val));
                                break;

                            case "PATH":
                                cookie.Path = val;
                                break;

                            case "DOMAIN":
                                cookie.Domain = val;
                                break;

                            case "PORT":
                                cookie.Port = val;
                                break;

                            case "COMMENT":
                                cookie.Comment = val;
                                break;

                            case "COMMENTURL":
                                try
                                {
                                    cookie.CommentUri = new Uri(val);
                                }
                                catch { }
                                break;

                            case "VERSION":
                                try
                                {
                                    cookie.Version = int.Parse(val);
                                }
                                catch { }
                                break;

                            }
                        }
                    }
                }

                cc.Add(cookie);
            }
            return cc;
        }

        private readonly static string[] cookieExpiresFormats =
        {
            "r",
            "ddd, dd'-'MMM'-'yyyy HH':'mm':'ss 'GMT'",
            "ddd, dd'-'MMM'-'yy HH':'mm':'ss 'GMT'",
            "ddd, dd'-'MMM'-'yyyy HH':'mm':'ss 'UTC'",
            "ddd, dd'-'MMM'-'yy HH':'mm':'ss 'UTC'"
        };

        private static IEnumerable<string> SplitEachCookie(string setCookies)
        {
            var lst = new List<string>();

            int read = 0;
            int find = 0;

            while (read < setCookies.Length)
            {
                find = setCookies.IndexOf(',', read + 1);
                if (find != -1 && setCookies.IndexOf("expires=", read, find - read, StringComparison.OrdinalIgnoreCase) >= 0)
                    find = setCookies.IndexOf(',', find + 1);

                if (find == -1)
                    find = setCookies.Length;

                yield return setCookies.Substring(read, find - read);

                read = find + 1;
            }
        }
        private static DateTime ParseCookieExpires(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return DateTime.MinValue;

            DateTime date;

            for (int i = 0; i < cookieExpiresFormats.Length; i++)
            {
                if (DateTime.TryParseExact(value, cookieExpiresFormats[i], CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    return TimeZone.CurrentTimeZone.ToLocalTime(date);
                }
            }

            return DateTime.MinValue;
        }

        private static string ToString(IDictionary<string, object> dic)
        {
            if (dic == null) return null;

            var sb = new StringBuilder();

            if (dic.Count > 0)
            {
                foreach (var st in dic)
                    if (st.Value is bool)
                        sb.AppendFormat("{0}={1}&", st.Key, (bool)st.Value ? "true" : "false");
                    else
                        sb.AppendFormat("{0}={1}&", st.Key, Uri.EscapeUriString(Convert.ToString(st.Value)));

                if (sb.Length > 0)
                    sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }
    }
}
