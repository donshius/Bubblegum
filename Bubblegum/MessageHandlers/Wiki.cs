using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Text.RegularExpressions;
using System.Net;
using Newtonsoft.Json;

namespace Bubblegum.MessageHandlers
{
	public class Wiki : BotPlugin
	{
		private string HandleWikiMessage(Message msg, string regex)
		{
			var matches = Regex.Matches(msg.Text, regex);
			if (matches.Count == 0) {
				return null;
			}

			string results = "";

			var wc = new WebClient();
			wc.Proxy = null;
			wc.Headers[HttpRequestHeader.UserAgent] = "Bubblegum / 2.0";

			foreach (Match match in matches) {
				var query = match.Groups[1].Value;
				try {
					var jsonResponse = wc.DownloadString("https://wiki.gtanet.work/api.php?action=opensearch&limit=3&search=" + Uri.EscapeUriString(query));

					dynamic data = JsonConvert.DeserializeObject(jsonResponse);
					var arrTitles = data[1];
					var arrLinks = data[3];

					if (arrTitles.Count == 0) {
						results += "`" + query + "` was not found on the wiki.\n";
					} else {
						for (int i = 0; i < arrTitles.Count; i++) {
							results += "`" + arrTitles[i] + "` " + Program.ShortUrl(arrLinks[i].ToString()) + "\n";
						}
					}

				} catch (Exception ex) {
					Logging.Error("Error while fetching search results on Wiki: {0}", ex.Message);
					results += "*Failed to search the wiki for `" + query + "`.*\n";
				}
			}

			return results;
		}

		public override string HandleMessage(Message msg)
		{
			var result = "";

			var resultApi = HandleWikiMessage(msg, @"\.[Aa]pi ([^ $]+)");
			var resultWiki = HandleWikiMessage(msg, @"\.[Ww]iki (.+)");

			if (resultApi == null && resultWiki == null) {
				return null;
			}

			if (resultApi != null) {
				result += resultApi;
			}
			if (resultWiki != null) {
				result += resultWiki;
			}

			return result.Trim() + " :robot:";
		}
	}
}
