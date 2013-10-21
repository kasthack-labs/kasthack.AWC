using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using EpicMorg.Net;
using kasthack.Tools;

namespace Tests {
    class Program {
        static void Main( string[] args ) {
            var running = false;
            var domain = "2ch.hk";
            var brd = "b";
            var frmstr = "http://{0}/{1}";
            var url = String.Format( frmstr, domain, brd );
            var r = new Regex( "[0-9]+", RegexOptions.Compiled );
            //WebRequest.Proxy=null;
            WebRequest.DefaultWebProxy = null;
            do {
                try {
                    "Page loading".Dump();
                    var prs = AWC.DownloadString( url );
                    const string strt = "<!--<div class=\"speed\">-->[";
                    var speed = int.Parse( r.Match( prs.Substring( prs.IndexOf(strt, System.StringComparison.Ordinal) + strt.Length, 50 ) ).Value );
                    String.Format( "2ch speed is {0} at {1}", speed, DateTime.Now ).Dump();
                }
                catch ( Exception ex ) {
                    ex.Dump();
                }
                Thread.Sleep( 1000 * 60 );
            }
            while ( running );
        }
    }
}
