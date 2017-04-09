using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Net;

namespace Bubblegum.MessageHandlers
{
	public class BuildUpdateConfig
	{
		public class Branch
		{
			public ulong Channel { get; set; }
			public string ID { get; set; }
			public string LastVersion { get; set; }
		}

		public List<Branch> Branches { get; set; } = new List<Branch>();
	}

	public class BuildUpdate : BotPlugin
	{
		private DateTime m_tmLastCheck = DateTime.Now;
		private int m_checkInterval = 300;

		public override bool MessagesRequiresAdmin() { return true; }

		public new BuildUpdateConfig Config
		{
			get {
				if (base.Config == null) {
					base.Config = new BuildUpdateConfig();
				}
				return (BuildUpdateConfig)base.Config;
			}
		}

		private string GetVersion(string branch)
		{
			try {
				var wc = new WebClient();
				wc.Proxy = null;
				wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:51.0) Gecko/20100101 Firefox/51.0";
				return wc.DownloadString("https://master.gtanet.work/update/" + branch + "/version");
			} catch {
				Logging.Error("Failed to fetch build update version for '" + branch + "'");
				return null;
			}
		}

		private void CheckBuilds()
		{
			foreach (var b in Config.Branches) {
				string version = GetVersion(b.ID);
				if (version == b.LastVersion || version == null || version == "") {
					continue;
				}

				var channel = Program.Client.GetChannel(b.Channel);
				if (channel == null) {
					Logging.Warn("Tried notifying of new build update in channel " + b.Channel + " but it couldn't be found");
					continue;
				}

				b.LastVersion = version;
				//channel.SendMessage(":warning: @here **New update** on the `" + b.ID + "` branch: `" + version + "`");
				channel.SendMessage(":warning: @here **New update**: `" + version + "`");
			}
		}

		public override string HandleMessage(Message msg)
		{
			var parse = msg.RawText.SplitCommandline();

			if (parse[0] == ".subscribeBuildUpdate" && parse.Length == 2) {
				var currentVersion = GetVersion(parse[1]);
				if (currentVersion == null) {
					return "**That looks like an invalid branch name!**";
				}

				Config.Branches.Add(new BuildUpdateConfig.Branch() {
					Channel = msg.Channel.Id,
					ID = parse[1],
					LastVersion = currentVersion
				});
				Program.SaveConfig();

				return "**Subscribed to build updates for branch '" + parse[1] + "'**. (Current: " + currentVersion + ")";

			} else if (parse[0] == ".unsubscribeBuildUpdate" && parse.Length == 2) {
				var branch = Config.Branches.Find(b => b.ID == parse[1] && b.Channel == msg.Channel.Id);
				if (branch == null) {
					return "**No subscribed branch with that name.**";
				}

				Config.Branches.Remove(branch);
				Program.SaveConfig();

				return "**Unsubscribed from branch updates '" + parse[1] + "'**.";

			} else if (parse[0] == ".checkBuilds") {
				CheckBuilds();
			}

			return null;
		}

		public override void OnInitialize()
		{
			CheckBuilds();
		}

		public override void OnUpdate(TimeSpan dt)
		{
			var tmLastUpdate = (DateTime.Now - m_tmLastCheck);
			if (tmLastUpdate.TotalSeconds > m_checkInterval) {
				CheckBuilds();
				Logging.Info("Build updates checked!");
				m_tmLastCheck = DateTime.Now;
			}
		}
	}
}
