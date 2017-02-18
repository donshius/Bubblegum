using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Menees.Diffs;

namespace Bubblegum.MessageHandlers
{
	public class NativeDbConfig
	{
		public List<ulong> SubscribedChannels { get; set; } = new List<ulong>();
	}

	public class NativeDb : BotPlugin
	{
		private DateTime m_tmLastCheck = DateTime.Now;
		private int m_checkInterval = 3600;

		public new NativeDbConfig Config
		{
			get {
				if (base.Config == null) {
					base.Config = new NativeDbConfig();
				}
				return (NativeDbConfig)base.Config;
			}
		}

		public override bool MessagesRequiresAdmin() { return true; }

		public override string HandleMessage(Message msg)
		{
			var parse = msg.RawText.SplitCommandline();

			if (parse[0] == ".subscribeNativeDb") {
				Config.SubscribedChannels.Add(msg.Channel.Id);
				Program.SaveConfig();

				if (!File.Exists("natives.h")) {
					File.WriteAllText("natives.h", DownloadHeader());
					return "**Subscribed to NativeDb updates and downloaded initial file.**";
				}

				return "**Subscribed to NativeDb updates.**";

			} else if (parse[0] == ".unsubscribeNativeDb") {
				if (!Config.SubscribedChannels.Contains(msg.Channel.Id)) {
					return "**This channel is not subscribed to NativeDb updates.**";
				}

				Config.SubscribedChannels.Remove(msg.Channel.Id);
				Program.SaveConfig();

				return "**Unsubscribed from NativeDb updates.**";

			} else if (parse[0] == ".checkNativeDb") {
				CheckNativeDb();
			}

			return null;
		}

		private string DownloadHeader()
		{
			var wc = new WebClient();
			wc.Proxy = null;
			wc.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:51.0) Gecko/20100101 Firefox/51.0";
			wc.Headers[HttpRequestHeader.Accept] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
			wc.Headers[HttpRequestHeader.AcceptEncoding] = "deflate";

			var sb = new StringBuilder();

			using (var ms = new MemoryStream(wc.DownloadData("http://www.dev-c.com/nativedb/natives.h"))) {
				ms.Seek(2, SeekOrigin.Begin);

				using (var ds = new DeflateStream(ms, CompressionMode.Decompress)) {
					byte[] buffer = new byte[1024];
					while (true) {
						int len = ds.Read(buffer, 0, 1024);

						if (len > 0) {
							sb.Append(Encoding.UTF8.GetString(buffer, 0, len));
						}

						if (len < 1024) {
							break;
						}
					}
				}
			}

			return sb.ToString();
		}

		private string CleanLine(string line)
		{
			var match = Regex.Match(line, "^\tstatic ([^{]+).*$");
			if (match.Success) {
				return match.Groups[1].Value.Trim();
			}

			return line;
		}

		private void CheckNativeDb()
		{
			if (Config.SubscribedChannels.Count == 0) {
				return;
			}

			var headerFile = DownloadHeader();

			if (!File.Exists("natives.h")) {
				File.WriteAllText("natives.h", headerFile);
				return;
			}

			var existingFileLines = new List<string>(File.ReadAllLines("natives.h"));

			File.WriteAllText("natives.h", headerFile);

			var headerFileLines = new List<string>(headerFile.Split('\n'));

			var diff = new TextDiff(HashType.Crc32, false, true);
			var changes = diff.Execute(existingFileLines, headerFileLines);

			int changeCount = 0;

			var outputText = "```diff\n";
			var lastEditType = EditType.None;

			foreach (var change in changes) {
				if (change.EditType == EditType.Change) {
					if (existingFileLines[change.StartA].StartsWith("// Generated ")) {
						continue;
					}

					if (lastEditType != EditType.Change) {
						outputText += "*** Changed:\n";
						lastEditType = EditType.Change;
					}

					changeCount += change.Length;

					for (int i = 0; i < change.Length; i++) {
						outputText += "- " + CleanLine(existingFileLines[change.StartA + i]) + "\n";
						outputText += "+ " + CleanLine(headerFileLines[change.StartB + i]) + "\n\n";
					}
				}

				if (change.EditType == EditType.Insert) {
					for (int i = 0; i < change.Length; i++) {
						var line = headerFileLines[change.StartA + i];
						if (line == "") {
							continue;
						}

						if (lastEditType != EditType.Insert) {
							outputText += "\n*** Added:\n";
							lastEditType = EditType.Insert;
						}

						changeCount++;
						outputText += "+ " + CleanLine(line) + "\n";
					}
				}

				if (change.EditType == EditType.Delete) {
					for (int i = 0; i < change.Length; i++) {
						var line = existingFileLines[change.StartA + i];
						if (line == "") {
							continue;
						}

						if (lastEditType != EditType.Delete) {
							outputText += "\n*** Removed:\n";
							lastEditType = EditType.Delete;
						}

						changeCount++;
						outputText += "- " + CleanLine(line) + "\n";
					}
				}
			}

			outputText += "```";

			Logging.Info("NativeDb had {0} changes.", changeCount);

			if (changeCount > 0) {
				foreach (var id in Config.SubscribedChannels) {
					var channel = Discord.GetChannel(id);
					if (channel == null) {
						continue;
					}

					channel.SendMessage("`[" + DateTime.Now.ToString("HH:mm:ss") + "] [NATIVES]` " + changeCount + " change(s):\n" + outputText);
				}
			}
		}

		public override void OnInitialize()
		{
			CheckNativeDb();
		}

		public override void OnUpdate(TimeSpan dt)
		{
			var tmLastUpdate = (DateTime.Now - m_tmLastCheck);
			if (tmLastUpdate.TotalSeconds > m_checkInterval) {
				CheckNativeDb();
				m_tmLastCheck = DateTime.Now;
			}
		}
	}
}
