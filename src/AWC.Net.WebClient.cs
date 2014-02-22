using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
namespace EpicMorg.Net {
    public static class AWC {
        private static object _locker = false;
        static AWC() {
            lock ( _locker ) {
                if (!(bool)_locker) ServicePointManager.DefaultConnectionLimit = Math.Max( ServicePointManager.DefaultConnectionLimit, 20 );
                _locker = true;
            }
        }

        public enum RequestMethod {
            Get,
            Post
        }
        #region User Functions
        #region Synchronous
        public static string DownloadString( string url, int enc, CookieContainer cookies = null, WebHeaderCollection headers = null,
            RequestMethod method = RequestMethod.Get, string post = null, int timeout = 5000, bool enableCompression = true ) {
            return DownloadString( url, Encoding.GetEncoding( enc ), cookies, headers, method, post, timeout, enableCompression );
        }
        public static string DownloadString( string url, Encoding enc = null, CookieContainer cookies = null, WebHeaderCollection headers = null,
            RequestMethod method = RequestMethod.Get, string post = null, int timeout = 5000, bool enableCompression = true ) {
            return Helper.SyncTask( DownloadStringAsync( url, enc, cookies, headers, method, post, timeout, enableCompression ) );
        }
        public static byte[] DownloadData( string url, CookieContainer cookies = null, WebHeaderCollection headers = null,
            RequestMethod method = RequestMethod.Get, string post = null, int timeout = 5000, bool enableCompression = true ) {
            return Helper.SyncTask( DownloadDataAsync( url, cookies, headers, method, post, timeout, enableCompression ) );
        }

        public static void DownloadFile( string url, string fileName, bool prealloc = true, WebHeaderCollection headers = null, CookieContainer cookies = null,
            RequestMethod method = RequestMethod.Get, string post = null, int timeout = 5000, bool enableCompression = false ) {
            Helper.VSyncTask( DownloadFileAsync( url, fileName, prealloc, headers, cookies, method, post, timeout, enableCompression ) );
        }
        #endregion
        #region Asynchronous
        public static async Task<string> DownloadStringAsync( string url, Encoding enc = null, CookieContainer cookies = null, WebHeaderCollection headers = null,
            RequestMethod method = RequestMethod.Get, string post = null, int timeout = 5000, bool enableCompression = true ) {
            using ( var rst = ( await _processRequestAsync( url, cookies, headers, method, post, timeout, enableCompression ) ).GetResponseStream() ) {
                if ( rst.CanTimeout ) rst.ReadTimeout = timeout;
                using ( var br = new BufferedStream( rst ) )
                using ( var sr = enc == null ? new StreamReader( br ) : new StreamReader( br, enc ) )
                    return await ( sr ).ReadToEndAsync();
            }
        }

        public static async Task<byte[]> DownloadDataAsync( string url, CookieContainer cookies = null, WebHeaderCollection headers = null,
            RequestMethod method = RequestMethod.Get, string post = null, int timeout = 5000, bool enableCompression = true ) {
            using ( var r = await _processRequestAsync( url, cookies, headers, method, post, timeout, enableCompression ) )
                return await _downloadDataAsync( r );
        }

        public static async Task DownloadFileAsync( string url, string fileName, bool prealloc = true, WebHeaderCollection headers = null, CookieContainer cookies = null,
            RequestMethod method = RequestMethod.Get, string post = null, int timeout = 5000, bool enableCompression = false ) {
            using ( var s = new FileStream( fileName, FileMode.Create, FileAccess.Write ) )
            using ( var r = await _processRequestAsync( url, cookies, headers, method, post, timeout, enableCompression ) )
                await _downloadStreamAsync( r, s, prealloc, timeout );
        }
        #endregion
        #endregion
        #region Engine
        private static async Task<WebResponse> _processRequestAsync( string url, CookieContainer cookies = null, WebHeaderCollection headers = null,
            RequestMethod method = RequestMethod.Get, string post = null, int timeout = 5000, bool enableCompression = true ) {
            try {
                var r = _prepareRequest( url, cookies, headers, method, timeout, enableCompression );
                if ( method != RequestMethod.Post || String.IsNullOrEmpty( post ) ) return ( await r.GetResponseAsync() );
                r.ContentType = "application/x-www-form-urlencoded";
                var stream = await r.GetRequestStreamAsync();
                var data = new UTF8Encoding().GetBytes( post );
                await stream.WriteAsync( data, 0, data.Length );
                await stream.FlushAsync();
                stream.Close();
                stream.Dispose();
                return ( await r.GetResponseAsync() );
            }
            catch ( WebException ex ) {
                if ( ex.Response != null )
                    return ex.Response;
                throw;
            }
        }
        private static HttpWebRequest _prepareRequest( string url, CookieContainer cookies = null, WebHeaderCollection headers = null,
            RequestMethod method = RequestMethod.Get, int timeout = 5000, bool enableCompression = true ) {
            var r = WebRequest.CreateHttp( url );
            r.Timeout = timeout;
            r.KeepAlive = false;//fuck keep-alive
            if ( cookies != null )
                r.CookieContainer = cookies;
            if ( headers != null )
                foreach ( var h in headers.AllKeys )
                    try { r.Headers.Add( h, headers[ h ] ); }
                    catch { }
            r.Method = method.ToString().ToUpperInvariant();
            r.AutomaticDecompression = enableCompression ?
                DecompressionMethods.Deflate | DecompressionMethods.GZip :
                DecompressionMethods.None;
            if ( r.UserAgent == null ) r.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.4 (KHTML, like Gecko)";
            return r;
        }
        private static async Task _downloadStreamAsync( WebResponse resp, Stream write, bool prealloc, int timeout = 5000 ) {
            const int buflength = 65536;
            const int grow = buflength * 128;
            //reduce fragmentation for files with no content-length specified
            var buf = new byte[ buflength ];
            var length = resp.ContentLength;
            var startlength = write.Length;
            var ready = 0L;

            using ( var read = resp.GetResponseStream() ) {
                if ( read.CanTimeout ) read.ReadTimeout = timeout;
                if ( prealloc && length > 0L ) write.SetLength( write.Length + length );
                int count;
                while ( ( count = await read.ReadAsync( buf, 0, buflength ) ) != 0 ) {
                    ready += count;
                    long curwlen;
                    if ( length <= 0L && startlength + ready + buflength > ( curwlen = write.Length ) )
                        write.SetLength( curwlen + grow );
                    await write.WriteAsync( buf, 0, count );
                }
                if ( prealloc ) write.SetLength( startlength + ready );
                await read.FlushAsync();
            }
        }
        private static async Task<byte[]> _downloadDataAsync( WebResponse resp, int timeout = 5000 ) {
            var buffer = new List<byte[]>();
            const int buflength = 65536;
            int count;
            var buf = new byte[ buflength ];
            var read = resp.GetResponseStream();
            if ( read.CanTimeout )
                read.ReadTimeout = timeout;
            while ( ( count = await read.ReadAsync( buf, 0, buflength ) ) != 0 )
                buffer.Add( buf.Take( count ).ToArray() );
            read.Close();
            return buffer.SelectMany( a => a ).ToArray();
        }

        #endregion
    }
    public static class Helper {
        //copypasta: http://msdn.microsoft.com/en-us/library/bb408523(v=exchg.140).aspx
        public static bool SSLValidatorCallback(
         object sender,
         X509Certificate certificate,
         X509Chain chain,
         SslPolicyErrors sslPolicyErrors ) {
            // If the certificate is a valid, signed certificate, return true.
            if ( sslPolicyErrors == SslPolicyErrors.None ) return true;
            // If there are errors in the certificate chain, look at each error to determine the cause.
            if ( ( sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors ) == 0 )
                return false;
            if ( chain == null || chain.ChainStatus == null ) return true;
            // When processing reaches this line, the only errors in the certificate chain are
            // untrusted root errors for self-signed certificates. These certificates are valid
            // for default Exchange Server installations, so return true.
            // In all other cases, return false.
            return chain.ChainStatus.Where( status => certificate.Subject != certificate.Issuer || ( status.Status != X509ChainStatusFlags.UntrustedRoot ) ).All( status => status.Status == X509ChainStatusFlags.NoError );
        }
        /// <summary>
        /// Convert CookieCollection to CookieContainer
        /// </summary>
        /// <param name="cookies"></param>
        /// <returns></returns>
        public static CookieContainer CCollectoion2Container( CookieCollection cookies ) {
            var c = new CookieContainer();
            c.Add( cookies );
            return c;
        }
        /// <summary>
        /// Wait for async task and return execution result
        /// </summary>
        /// <param name="task"></param>
        public static T SyncTask<T>( Task<T> task ) {
            VSyncTask( task );
            return task.Result;
        }
        /// <summary>
        /// Wait for async task
        /// </summary>
        /// <param name="task"></param>
        public static void VSyncTask( Task task ) {
            //not started
            if ( task.Status == TaskStatus.Created )
                task.Start();
            //not finished
            if ( task.Status != TaskStatus.Canceled && task.Status != TaskStatus.Faulted && task.Status != TaskStatus.RanToCompletion )
                task.Wait();
        }
    }
}
