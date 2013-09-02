using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicMorg.Net {
	public static class AdvancedWebClient {
		public enum RequestMethod {
			GET,
			POST
		}
		#region User Functions
		#region Synchronous
		public static string DownloadString( string url, int enc, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
			return DownloadString( url, Encoding.GetEncoding(enc), cookies , headers, method, post, timeout );}
		public static string DownloadString( string url, Encoding enc = null, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
			var rst = _processRequest( url, cookies, headers, method, post, timeout).GetResponseStream();
			rst.ReadTimeout = timeout;
			return ( enc == null ? new StreamReader( rst ) : new StreamReader( rst, enc ) ).ReadToEnd();
		}

		public static byte[] DownloadData( string url, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
			return _downloadData( _processRequest( url, cookies, headers, method, post,timeout ) );}
		
		public static void DownloadFile( string url, string fileName, bool prealloc, WebHeaderCollection headers, CookieContainer cookies = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
			Stream s = null;
			try { _downloadStream( _processRequest( url, cookies, headers, method, post, timeout ), ( s = new FileStream( fileName, FileMode.Create, FileAccess.Write ) ), prealloc, timeout ); }
			finally { try { s.Close(); } catch { } } }
		#endregion
		#region Asynchronous
		public static async Task<string> DownloadStringAsync( string url, Encoding enc = null, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ){
			var rst = (await _processRequestAsync(url, cookies, headers, method, post, timeout)).GetResponseStream();
			rst.ReadTimeout = timeout;
			return await ( enc == null ? new StreamReader( rst ) : new StreamReader( rst, enc ) ).ReadToEndAsync();}

		public static async Task<byte[]> DownloadDataAsync( string url, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
			return await _downloadDataAsync(await _processRequestAsync( url, cookies, headers, method, post, timeout ) );}
		
		public static async Task DownloadFileAsync( string url, string fileName, bool prealloc, WebHeaderCollection headers, CookieContainer cookies = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
			Stream s = null;
			try { await _downloadStreamAsync(await _processRequestAsync( url, cookies, headers, method, post, timeout ), ( s = new FileStream( fileName, FileMode.Create, FileAccess.Write ) ), prealloc, timeout ); }
			finally { try { s.Close(); } catch { } }
		}
		#endregion
		#endregion
		#region Engine
		private static WebResponse _processRequest( string url, CookieContainer cookies=null, WebHeaderCollection headers=null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ){
			var r = _prepareRequest(url, cookies, headers, method, post, timeout);
			if ( method == RequestMethod.POST && !String.IsNullOrEmpty( post ) ) {
				r.ContentType = "application/x-www-form-urlencoded";
				Stream stream = r.GetRequestStream();
				byte[] data = new System.Text.UTF8Encoding().GetBytes( post );
				stream.Write( data, 0, data.Length );
				stream.Close();
				stream.Dispose();
			}
			return r.GetResponse();}
		private static async Task<WebResponse> _processRequestAsync( string url, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
			var r = _prepareRequest( url, cookies, headers, method, post, timeout );
			if ( method == RequestMethod.POST && !String.IsNullOrEmpty( post ) ) {
				r.ContentType = "application/x-www-form-urlencoded";
				Stream stream = await r.GetRequestStreamAsync();
				byte[] data = new System.Text.UTF8Encoding().GetBytes( post );
				await stream.WriteAsync( data, 0, data.Length );
				stream.Close();
				stream.Dispose();
			}
			return ( await r.GetResponseAsync() );
		}
		private static HttpWebRequest _prepareRequest( string url, CookieContainer cookies = null, WebHeaderCollection headers = null, RequestMethod method = RequestMethod.GET, string post = null, int timeout = 5000 ) {
			var r = ( HttpWebRequest ) WebRequest.CreateHttp( url );
			r.Timeout = timeout;
			if ( cookies != null ) r.CookieContainer = cookies;
			if ( headers != null ) foreach ( var h in headers.AllKeys ) try {r.Headers.Add( h, headers[ h ] );}catch { }
			r.Method = method.ToString().ToUpper();
			if ( r.UserAgent == null ) r.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.4 (KHTML, like Gecko)";
			r.Headers.Add( HttpRequestHeader.AcceptEncoding, "none" );
			return r;
		}
		
		private static void _downloadStream( WebResponse resp, Stream write, bool prealloc, int timeout = 5000 ) {
			Stream read = resp.GetResponseStream();
			read.ReadTimeout = timeout;
			long length = resp.ContentLength, startlength = write.Length, ready = 0, curwlen=startlength;
			int buflength = 65536, count = 0;
			byte[] buf = new byte[ buflength ];
			if ( prealloc && length>0 )
				write.SetLength( write.Length + length );
			while ( ( count = read.Read( buf, 0, buflength ) ) != 0 ) {
				ready += count;
				write.Write( buf, 0, count );
				if (length < 0 && startlength + ready + buflength > (curwlen = write.Length))
					write.SetLength(curwlen + buflength*4);
			}
			if ( prealloc ) write.SetLength( startlength + ready );
			read.Flush();
			read.Close();
		}
		private static async Task _downloadStreamAsync( WebResponse resp, Stream write, bool prealloc, int timeout = 5000 ) {
			Stream read = resp.GetResponseStream();
			read.ReadTimeout = timeout;
			long length = resp.ContentLength, startlength = write.Length, ready = 0, curwlen = startlength;
			int buflength = 65536, count = 0;
			byte[] buf = new byte[ buflength ];
			if ( prealloc && length > 0 )
				write.SetLength( write.Length + length );
			while ( ( count = await read.ReadAsync( buf, 0, buflength ) ) != 0 ) {
				ready += count;
				await write.WriteAsync( buf, 0, count );
				if ( length < 0 && startlength + ready + buflength > ( curwlen = write.Length ) )
					write.SetLength( curwlen + buflength * 4 );
			}
			if ( prealloc ) write.SetLength( startlength + ready );
			await read.FlushAsync();
			read.Close();
		}
		
		private static byte[] _downloadData( WebResponse resp, int timeout = 5000 ) {
			Stream read;
			List<byte[]> buffer = new List<byte[]>();
			int buflength = 65536, count = 0;
			byte[] buf = new byte[ buflength ];
			read = resp.GetResponseStream();
			read.ReadTimeout = timeout;
			while ( ( count = read.Read( buf, 0, buflength ) ) != 0 )
				buffer.Add(buf.Take( count ).ToArray());
			read.Close();
			return buffer.SelectMany(a=>a).ToArray();
		}
		private static async Task<byte[]> _downloadDataAsync( WebResponse resp, int timeout = 5000 ) {
			Stream read;
			List<byte[]> buffer = new List<byte[]>();
			int buflength = 65536, count = 0;
			byte[] buf = new byte[ buflength ];
			read = resp.GetResponseStream();
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
		#endregion
	}
}
