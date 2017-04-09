using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bubblegum.MessageHandlers
{
	public class RestrictConfig
	{
		public class Restriction
		{
			public ulong Channel { get; set; }
			public int Mode { get; set; }
		}

		public List<Restriction> Restrictions { get; set; } = new List<Restriction>();
	}

	public class Restrict : BotPlugin
	{
		public new RestrictConfig Config
		{
			get {
				if (base.Config == null) {
					base.Config = new RestrictConfig();
				}
				return (RestrictConfig)base.Config;
			}
		}

		private void DeleteMessage(Message msg)
		{
			Logging.Info("Deleted restricted message from {0} in channel {1}: \"{2}\"", msg.User.Name, msg.Channel.Name, msg.RawText);
			msg.Delete();
		}

		public override string HandleMessage(Message msg)
		{
			if (Program.Config.Admins.Contains(msg.User.Id)) {
				var parse = msg.RawText.SplitCommandline();
				if (parse.Length > 0) {
					if (parse[0] == ".restrict" && parse.Length == 2) {
						int mode = 0;
						if (!int.TryParse(parse[1], out mode)) {
							return "Wrong restriction!";
						}
						Config.Restrictions.Add(new RestrictConfig.Restriction() {
							Channel = msg.Channel.Id,
							Mode = mode
						});
						Program.SaveConfig();
						return "Restriction set.";

					} else if (parse[0] == ".unrestrict" && parse.Length == 2) {
						int mode = 0;
						if (!int.TryParse(parse[1], out mode)) {
							return "Wrong restriction!";
						}
						var restriction = Config.Restrictions.Find(r => r.Channel == msg.Channel.Id && r.Mode == mode);
						if (restriction == null) {
							return "No such restriction!";
						}
						Config.Restrictions.Remove(restriction);
						Program.SaveConfig();
						return "Restriction removed.";
					}
				}
				// Admins have no restrictions
				//return null;
			}

			foreach (var restriction in Config.Restrictions) {
				if (msg.Channel.Id != restriction.Channel) {
					continue;
				}

				// 1 = remove non-media
				if (restriction.Mode == 1) {
					if (msg.Embeds.Length > 0 || msg.Attachments.Length > 0) {
						Logging.Info("Message is fine - embed/attachment found");
						return null;
					}
					if (Regex.IsMatch(msg.RawText, @"https?:\/\/.+\/[A-Za-z0-9]+\.(png|jpg|gif)")) {
						Logging.Info("Message is fine - image link found");
						return null;
					}
					if (Regex.IsMatch(msg.RawText, @"https?:\/\/www\.youtube\.com\/watch\?v=[A-Za-z0-9\-_]{11}")) {
						Logging.Info("Message is fine - Youtube link found");
						return null;
					}
					if (Regex.IsMatch(msg.RawText, @"https?:\/\/youtu\.be/[A-Za-z0-9\-_]{11}")) {
						Logging.Info("Message is fine - Youtube short link found");
						return null;
					}
					DeleteMessage(msg);
				}
			}

			return null;
		}
	}
}
