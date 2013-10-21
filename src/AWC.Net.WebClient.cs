using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicMorg.Net {
    public static class AWC {
        public enum RequestMethod {
            GET,
            POST
        }
        #region User Functions
        #region Synchronous
        public static string DownloadString( string url, int enc, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
            return DownloadString( url, Encoding.GetEncoding( enc ), cookies, headers, method, post, timeout );
        }
        public static string DownloadString( string url, Encoding enc = null, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
            return SyncTask(DownloadStringAsync(url, enc, cookies, headers, method, post, timeout));
        }
        public static byte[] DownloadData( string url, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
            return SyncTask( DownloadDataAsync( url, cookies, headers, method, post, timeout ) );
        }

        public static void DownloadFile( string url, string fileName, bool prealloc = true, WebHeaderCollection headers = null, CookieContainer cookies = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
            SyncTask(DownloadFileAsync(url, fileName, prealloc, headers, cookies, method, post, timeout));
        }
        #endregion
        #region Asynchronous
        public static async Task<string> DownloadStringAsync( string url, Encoding enc = null, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
            using ( var rst = ( await _processRequestAsync( url, cookies, headers, method, post, timeout ) ).GetResponseStream() ) {
                rst.ReadTimeout = timeout;
                using ( var br = new BufferedStream(rst) )
                    using ( var sr = enc == null ? new StreamReader(br) : new StreamReader(br, enc) )
                        return await ( sr ).ReadToEndAsync();
            }
        }

        public static async Task<byte[]> DownloadDataAsync( string url, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
            return await _downloadDataAsync( await _processRequestAsync( url, cookies, headers, method, post, timeout ) );
        }

        public static async Task DownloadFileAsync( string url, string fileName, bool prealloc = true, WebHeaderCollection headers = null, CookieContainer cookies = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
            Stream s = null;
            try { await _downloadStreamAsync( await _processRequestAsync( url, cookies, headers, method, post, timeout ), ( s = new FileStream( fileName, FileMode.Create, FileAccess.Write ) ), prealloc, timeout ); }
            finally { try { s.Close(); } catch { } }
        }
        #endregion
        #endregion
        #region Engine
        private static async Task<WebResponse> _processRequestAsync( string url, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
            var r = _prepareRequest( url, cookies, headers, method, timeout );
            if ( method != RequestMethod.POST || String.IsNullOrEmpty( post ) ) return ( await r.GetResponseAsync() );
            r.ContentType = "application/x-www-form-urlencoded";
            var stream = await r.GetRequestStreamAsync();
            var data = new UTF8Encoding().GetBytes( post );
            await stream.WriteAsync( data, 0, data.Length );
            await stream.FlushAsync();
            stream.Close();
            stream.Dispose();
            return ( await r.GetResponseAsync() );
        }
        private static HttpWebRequest _prepareRequest( string url, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, int timeout = 5000 ) {
            var r = WebRequest.CreateHttp( url );
            r.Timeout = timeout;
            if ( cookies != null ) r.CookieContainer = cookies;
            if ( headers != null ) foreach ( var h in headers.AllKeys ) try { r.Headers.Add( h, headers[ h ] ); }
                    catch { }
            r.Method = method.ToString().ToUpper();
            if ( r.UserAgent == null ) r.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.4 (KHTML, like Gecko)";
            r.Headers.Add( HttpRequestHeader.AcceptEncoding, "none" );
            return r;
        }
        private static async Task _downloadStreamAsync( WebResponse resp, Stream write, bool prealloc, int timeout = 5000 ) {
            const int buflength = 65536;
            var buf = new byte[ buflength ];
            int count;
            long length = resp.ContentLength, startlength = write.Length, ready = 0, curwlen = startlength;
            var read = resp.GetResponseStream();
            read.ReadTimeout = timeout;
            if ( prealloc && length > 0 )
                write.SetLength( write.Length + length );
            while ( ( count = await read.ReadAsync( buf, 0, buflength ) ) != 0 ) {
                ready += count;
                await write.WriteAsync( buf, 0, count );
                if ( length < 0 && startlength + ready + buflength > ( curwlen = write.Length ) )
                    write.SetLength( curwlen + buflength * 16 ); //reduce fragmentation for files with no content-length specified
            }
            if ( prealloc ) write.SetLength( startlength + ready );
            await read.FlushAsync();
            read.Close();
        }
        private static async Task<byte[]> _downloadDataAsync( WebResponse resp, int timeout = 5000 ) {
            var buffer = new List<byte[]>();
            const int buflength = 65536;
            int count;
            var buf = new byte[ buflength ];
            var read = resp.GetResponseStream();
            read.ReadTimeout = timeout;
            while ( ( count = await read.ReadAsync( buf, 0, buflength ) ) != 0 )
                buffer.Add( buf.Take( count ).ToArray() );
            read.Close();
            return buffer.SelectMany( a => a ).ToArray();
        }
        public static CookieContainer CCollectoion2Container( CookieCollection cookies ) {
            var c = new CookieContainer();
            c.Add( cookies );
            return c;
        }
        private static T SyncTask<T>( Task<T> task ) {
            task.Start();
            task.Wait();
            return task.Result;
        }
        private static void SyncTask( Task task ) {
            task.Start();
            task.Wait();
        }

        #endregion
    }
}
