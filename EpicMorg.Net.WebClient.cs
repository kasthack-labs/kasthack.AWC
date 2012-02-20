using System;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
namespace EpicMorg.Net
{
    public static class AdvancedWebClient
    {
        #region User Functions
        public static string DownloadString(string URL)
        {
            return DownloadString(URL, null, null, null);
        }
        public static string DownloadString(string URL, Encoding enc)
        {
            return DownloadString(URL, enc, null, null);
        }
        public static string DownloadString(string URL, CookieCollection cookies)
        {
            return DownloadString(URL, null, gcc(cookies), null);
        }
        public static string DownloadString(string URL, CookieContainer cookies)
        {
            return DownloadString(URL, null, cookies, null);
        }
        public static string DownloadString(string URL, Encoding enc, CookieCollection cookies)
        {
            return DownloadString(URL, enc, gcc(cookies), null);
        }
        public static string DownloadString(string URL, Encoding enc, CookieContainer cookies)
        {
            return DownloadString(URL, enc, cookies, null);
        }
        public static string DownloadString(string URL, int enc, CookieContainer cookies)
        {
            return DownloadString(URL, Encoding.GetEncoding(enc), cookies, null);
        }
        public static string DownloadString(string URL, int enc, CookieCollection cookies)
        {
            return DownloadString(URL, Encoding.GetEncoding(enc), gcc(cookies), null);
        }
        public static string DownloadString(string URL, Encoding enc, CookieContainer cookies, WebHeaderCollection headers)
        {
            HttpWebRequest r = (HttpWebRequest) WebRequest.Create(URL);
            if (cookies != null)
                r.CookieContainer = cookies;
            if (headers != null)
                r.Headers = headers;
            r.Headers.Add(HttpRequestHeader.AcceptEncoding, "none");
            if (enc!=null)
                return new StreamReader(((HttpWebResponse) r.GetResponse()).GetResponseStream(),enc).ReadToEnd();
            else
                return new StreamReader(((HttpWebResponse) r.GetResponse()).GetResponseStream()).ReadToEnd();
        }
        
        public static byte[] DownloadData(string URL)
        {
            return DownloadData(URL, null, null);
        }
        public static byte[] DownloadData(string URL,CookieContainer cookies)
        {
            return DownloadData(URL, cookies, null);
        }
        public static byte[] DownloadData(string URL, WebHeaderCollection headers)
        {
            return DownloadData(URL, null, headers);
        }
        public static byte[] DownloadData(string URL,CookieContainer cookies,WebHeaderCollection headers)
        {
            HttpWebRequest r = (HttpWebRequest) WebRequest.Create(URL);
            if (cookies != null)
                r.CookieContainer = cookies;
            if (headers != null)
                r.Headers = headers;
            r.Headers.Add(HttpRequestHeader.AcceptEncoding, "none");
            return _downloaddata(r);
        }
        public static void DownloadFile(string URL, string FileName)
        {
            DownloadFile(URL, null,null, FileName,true);
        }
        public static void DownloadFile(string URL,CookieContainer cookies, string FileName)
        {
            DownloadFile(URL, cookies, null, FileName,true);
        }
        public static void DownloadFile(string URL, WebHeaderCollection headers, string FileName)
        {
            DownloadFile(URL, null, headers, FileName,true);
        }
        public static void DownloadFile(string URL, CookieContainer cookies, WebHeaderCollection headers,string FileName)
        {
            DownloadFile(URL, cookies, headers, FileName, true);
        }
        public static void DownloadFile(string URL, CookieContainer cookies, WebHeaderCollection headers, string FileName, bool prealloc)
        {
            HttpWebRequest r = (HttpWebRequest) WebRequest.Create(URL);
            if (cookies != null)
                r.CookieContainer = cookies;
            if (headers != null)
                r.Headers = headers;
            r.Headers.Add(HttpRequestHeader.AcceptEncoding, "none");
            _downloadstream(r, new FileStream(FileName, FileMode.Create, FileAccess.Write), prealloc);
        }
        #endregion
        #region Egine
        private static void _downloadstream(HttpWebRequest httpWebRequest, Stream write,bool prealloc)
        {
            var resp = httpWebRequest.GetResponse();
            Stream read = resp.GetResponseStream();
            long length = resp.ContentLength, startlength = write.Length, ready = 0;
            int buflength = 65536, count = 0;
            if (prealloc)
                write.SetLength(write.Length + length);
            byte[] buf = new byte[buflength];
            while ((count = read.Read(buf, 0, buflength)) != 0)
            {
                ready += count;
                write.Write(buf, 0,count);
                #if DEBUG
                     Thread.Sleep(1000);
                #endif
            }
            if (prealloc)
                write.SetLength(startlength + ready);
            read.Close();
        }
        private static byte[] _downloaddata(HttpWebRequest httpWebRequest)
        {

            Stream read = httpWebRequest.GetResponse().GetResponseStream();
            long length = read.Length, ready = 0;
            byte[] output = new byte[0];
            int buflength = 65536, count = 0;
            byte[] buf = new byte[buflength];
            while ((count = read.Read(buf, 0, buflength)) != 0)
            {
                ready += count;
                output = output.Concat(buf.Take(count)).ToArray();
                #if DEBUG
                     Thread.Sleep(1000);
                #endif
            }
            read.Close();
            return output;
        }
        private static CookieContainer gcc(CookieCollection cookies)
        {
            CookieContainer c = new CookieContainer();
            c.Add(cookies);
            return c;
        }
        #endregion
    }
}