using System;
using System.Net;

namespace SteamBot
{
	public static class OtherWebClients
	{
		private class MyWebClient : WebClient
		{
			protected override WebRequest GetWebRequest(Uri address)
			{
				WebRequest w = base.GetWebRequest(address);
				w.Timeout = 10000;
				return w;
			}
		}

		public static bool IsMarked(string steamId32)
		{
			string url = string.Format("http://steamrep.com/id2rep.php?steamID32={0}", steamId32);
			WebClient client = new WebClient();
			string result = client.DownloadString(url);
			return result.IndexOf("SCAMMER", StringComparison.Ordinal) > -1;
		}
	}
}
