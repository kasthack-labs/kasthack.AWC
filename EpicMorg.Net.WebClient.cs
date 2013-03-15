using System;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
namespace EpicMorg.Net {
    public static class AdvancedWebClient {
        public enum RequestMethod {
            GET,
            POST
        }
        #region User Functions
        public static string DownloadString( string URL ) {
            return DownloadString(URL, null, null, null);
        }
        public static string DownloadString( string URL, Encoding enc ) {
            return DownloadString(URL, enc, null, null);
        }
        public static string DownloadString( string URL, CookieCollection cookies ) {
            return DownloadString(URL, null, gcc(cookies), null);
        }
        public static string DownloadString( string URL, CookieContainer cookies ) {
            return DownloadString(URL, null, cookies, null);
        }
        public static string DownloadString( string URL, Encoding enc, CookieCollection cookies ) {
            return DownloadString(URL, enc, gcc(cookies), null);
        }
        public static string DownloadString( string URL, Encoding enc, CookieContainer cookies ) {
            return DownloadString(URL, enc, cookies, null);
        }
        public static string DownloadString( string URL, int enc, CookieContainer cookies ) {
            return DownloadString(URL, Encoding.GetEncoding(enc), cookies, null);
        }
        public static string DownloadString( string URL, int enc, CookieCollection cookies ) {
            return DownloadString(URL, Encoding.GetEncoding(enc), gcc(cookies), null);
        }
        public static string DownloadString( string URL, Encoding enc, CookieContainer cookies, WebHeaderCollection headers, RequestMethod Method = RequestMethod.GET, string Post = null, int Timeout = 5000 ) {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(URL);
            if ( cookies != null )
                r.CookieContainer = cookies;
            if ( headers != null )
                foreach ( var h in headers.AllKeys ) {
                    try {
                        r.Headers.Add(h, headers[h]);
                    }
                    catch {
                        try {
                            var info = typeof(HttpWebRequest).GetProperty(h.Replace("-", ""));
                            info.SetValue(r, headers[h], null);
                        }
                        catch { }
                    }
                }
            r.Headers.Add(HttpRequestHeader.AcceptEncoding, "none");
            if ( r.UserAgent == null )
                r.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.4 (KHTML, like Gecko)";
            r.Method = Method.ToString().ToUpper();
            if ( Method == RequestMethod.POST && !String.IsNullOrEmpty(Post) ) {
                Stream stream = r.GetRequestStream();
                byte[] data = new System.Text.UTF8Encoding().GetBytes(Post);
                stream.Write(data, 0, data.Length);
                stream.Close();
                stream.Dispose();
            }
            r.Timeout = Timeout;
            var rst = ( (HttpWebResponse)r.GetResponse() ).GetResponseStream();
            rst.ReadTimeout = Timeout;
            if ( enc != null )
                return new StreamReader(rst, enc).ReadToEnd();
            else
                return new StreamReader(rst).ReadToEnd();
        }
        public static byte[] DownloadData( string URL ) {
            return DownloadData(URL, (CookieContainer)null, null);
        }
        public static byte[] DownloadData( string URL, CookieContainer cookies ) {
            return DownloadData(URL, cookies, null);
        }
        public static byte[] DownloadData( string URL, CookieCollection cookies ) {
            return DownloadData(URL, gcc(cookies), null);
        }
        public static byte[] DownloadData( string URL, WebHeaderCollection headers ) {
            return DownloadData(URL, (CookieContainer)null, headers);
        }
        public static byte[] DownloadData( string URL, CookieCollection cookies, WebHeaderCollection headers ) {
            return DownloadData(URL, gcc(cookies), headers);
        }
        public static byte[] DownloadData( string URL, CookieContainer cookies, WebHeaderCollection headers, RequestMethod Method = RequestMethod.GET, string Post = null ) {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(URL);
            if ( cookies != null )
                r.CookieContainer = cookies;
            if ( headers != null )
                foreach ( var h in headers.AllKeys ) {
                    try {
                        r.Headers.Add(h, headers[h]);
                    }
                    catch { }
                }
            r.Method = Method.ToString().ToUpper();
            if ( Method == RequestMethod.POST && !String.IsNullOrEmpty(Post) ) {
                Stream stream = r.GetRequestStream();
                byte[] data = new System.Text.UTF8Encoding().GetBytes(Post);
                stream.Write(data, 0, data.Length);
                stream.Close();
                stream.Dispose();
            }
            r.Headers.Add(HttpRequestHeader.AcceptEncoding, "none");
            return _downloaddata(r);
        }
        public static void DownloadFile( string URL, string FileName ) {
            DownloadFile(URL, (CookieContainer)null, null, FileName, true);
        }
        public static void DownloadFile( string URL, CookieContainer cookies, string FileName ) {
            DownloadFile(URL, cookies, null, FileName, true);
        }
        public static void DownloadFile( string URL, CookieCollection cookies, string FileName ) {
            DownloadFile(URL, gcc(cookies), null, FileName, true);
        }
        public static void DownloadFile( string URL, WebHeaderCollection headers, string FileName ) {
            DownloadFile(URL, (CookieContainer)null, headers, FileName, true);
        }
        public static void DownloadFile( string URL, CookieContainer cookies, WebHeaderCollection headers, string FileName ) {
            DownloadFile(URL, cookies, headers, FileName, true);
        }
        public static void DownloadFile( string URL, CookieCollection cookies, WebHeaderCollection headers, string FileName ) {
            DownloadFile(URL, gcc(cookies), headers, FileName, true);
        }
        public static void DownloadFile( string URL, CookieCollection cookies, WebHeaderCollection headers, string FileName, bool prealloc ) {
            DownloadFile(URL, gcc(cookies), headers, FileName, prealloc);
        }
        public static void DownloadFile( string URL, CookieContainer cookies, WebHeaderCollection headers, string FileName, bool prealloc, RequestMethod Method = RequestMethod.GET, string Post = null ) {
            HttpWebRequest r = (HttpWebRequest)WebRequest.Create(URL);
            if ( cookies != null )
                r.CookieContainer = cookies;
            if ( headers != null )
                foreach ( var h in headers.AllKeys ) {
                    try {
                        r.Headers.Add(h, headers[h]);
                    }
                    catch { }
                }
            r.Method = Method.ToString().ToUpper();
            if ( Method == RequestMethod.POST && !String.IsNullOrEmpty(Post) ) {
                Stream stream = r.GetRequestStream();
                byte[] data = new System.Text.UTF8Encoding().GetBytes(Post);
                stream.Write(data, 0, data.Length);
                stream.Close();
                stream.Dispose();
            }
            r.Headers.Add(HttpRequestHeader.AcceptEncoding, "none");
            Stream s = null;
            try {
                s = new FileStream(FileName, FileMode.Create, FileAccess.Write);
                _downloadstream(r, s, prealloc);
            }
            finally {
                try { s.Close(); }
                catch { }
            }
        }
        #endregion
        #region Egine
        private static void _downloadstream( HttpWebRequest httpWebRequest, Stream write, bool prealloc, int Timeout = 5000 ) {

            var resp = httpWebRequest.GetResponse();
            Stream read = resp.GetResponseStream();
            read.ReadTimeout = Timeout;
            long length = resp.ContentLength, startlength = write.Length, ready = 0;
            int buflength = 65536, count = 0;
            if ( prealloc )
                write.SetLength(write.Length + length);
            byte[] buf = new byte[buflength];
            while ( ( count = read.Read(buf, 0, buflength) ) != 0 ) {
                ready += count;
                write.Write(buf, 0, count);
            }
            if ( prealloc )
                write.SetLength(startlength + ready);
            read.Close();
        }
        private static byte[] _downloaddata( HttpWebRequest httpWebRequest, int Timeout = 5000 ) {

            httpWebRequest.Timeout = Timeout;
            var resp = httpWebRequest.GetResponse();
            Stream read = resp.GetResponseStream();
            read.ReadTimeout = Timeout;
            long length = resp.ContentLength, ready = 0;
            byte[] output = new byte[0];
            int buflength = 65536, count = 0;
            byte[] buf = new byte[buflength];
            while ( ( count = read.Read(buf, 0, buflength) ) != 0 ) {
                ready += count;
                output = output.Concat(buf.Take(count)).ToArray();
            }
            read.Close();
            return output;
        }
        private static CookieContainer gcc( CookieCollection cookies ) {
            CookieContainer c = new CookieContainer();
            c.Add(cookies);
            return c;
        }
        #endregion
    }
}
