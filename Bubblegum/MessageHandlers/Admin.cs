using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Bubblegum.CommandHandlers
{
	public class Admin : BotPlugin
	{
		public override bool MessagesRequiresAdmin() { return true; }

		public override string HandleMessage(Message msg)
		{
			var parse = msg.RawText.SplitCommandline();

			if (parse[0] == ".quit") {
				Program.QuitRequested = true;

			} else if (parse[0] == ".addAdmin" && parse.Length > 1) {
				foreach (var user in msg.MentionedUsers) {
					if (Program.Config.Admins.Contains(user.Id)) {
						continue;
					}
					Program.Config.Admins.Add(user.Id);
					Program.SaveConfig();
				}

				return "*Admins added.*";

			} else if (parse[0] == ".removeAdmin" && parse.Length > 1) {
				foreach (var user in msg.MentionedUsers) {
					if (Program.Config.Admins.Contains(user.Id)) {
						continue;
					}
					Program.Config.Admins.Add(user.Id);
					Program.SaveConfig();
				}

				return "*Admins removed.*";

			} else if (parse[0] == ".addResponse" && parse.Length == 3) {
				var newResponse = new SettingsResponse();
				newResponse.Keywords.Add(parse[1]);
				newResponse.Response = parse[2];
				Program.Config.Responses.Add(newResponse);

				return "*New response added.*";

			} else if (parse[0] == ".removeResponse" && parse.Length == 2) {
				var response = Program.Config.Responses.Find(r => r.Keywords.Contains(parse[1]));
				if (response == null) {
					return "*No such response keyword found.*";
				}

				Program.Config.Responses.Remove(response);
				Program.SaveConfig();

				return "*Response removed.*";

			} else if (parse[0] == ".addResponseKeyword" && parse.Length == 3) {
				var response = Program.Config.Responses.Find(r => r.Keywords.Contains(parse[1]));
				if (response == null) {
					return "*No such keyword found.*";
				}
				response.Keywords.Add(parse[2]);

				Program.SaveConfig();

				return "*New keyword added.*";

			} else if (parse[0] == ".removeResponseKeyword" && parse.Length == 2) {
				var response = Program.Config.Responses.Find(r => r.Keywords.Contains(parse[1]));
				if (response == null) {
					return "*No such response keyword found.*";
				}

				response.Keywords.Remove(parse[1]);

				if (response.Keywords.Count == 0) {
					Program.Config.Responses.Remove(response);
					Program.SaveConfig();
					return "*Response removed.*";
				} else {
					Program.SaveConfig();
					return "*Keyword removed.*";
				}

			} else if (parse[0] == ".useShortUrl") {
				bool val = false;
				if (bool.TryParse(parse[1], out val)) {
					Program.Config.UseShortUrls = val;
					return "*Set short urls.*";
				}

				return "*I don't know what that means.*";

			}

			return null;
		}
	}
}
