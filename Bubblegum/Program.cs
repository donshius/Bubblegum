using Discord;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bubblegum
{
	public class Program
	{
		public static bool QuitRequested = false;
		public static bool KeepRunnig = true;

		public static Settings Config;
		public static DiscordClient Client;

		public static List<BotPlugin> Plugins = new List<BotPlugin>();

		public static string ShortUrl(string longUrl)
		{
			if (!Config.UseShortUrls) {
				return longUrl;
			}

			try {
				var wc = new WebClient();
				wc.Proxy = null;
				wc.Headers[HttpRequestHeader.ContentType] = "application/json";
				var ret = wc.UploadString("https://www.googleapis.com/urlshortener/v1/url?key=" + Config.ShortUrlApiKey, "{\"longUrl\":\"" + longUrl + "\"}");
				dynamic data = JsonConvert.DeserializeObject(ret);
				return data["id"];
			} catch {
				return longUrl;
			}
		}

		static void Main(string[] args)
		{
			Logging.Info("Bubblegum 2.0");

			if (!File.Exists("settings.xml")) {
				Logging.Error("No settings.xml found! An empty file was created. Edit it and run the bot again.");

				Config = new Settings();
				Config.Save("settings.xml");

				return;
			} else {
				Config = Settings.FromFile("settings.xml");
			}

			Logging.Info("Connecting to Discord...");

			Client = new DiscordClient();
			Client.Connect(Config.API.Token, TokenType.Bot).Wait();

			Logging.Info("Connected: {0}", Client.CurrentUser.Name);

			Logging.Info("Loading handlers...");

			Plugins = new List<BotPlugin>();
			foreach (var h in Config.Handlers) {
				var type = Type.GetType(h.ClassName);
				if (type == null) {
					Logging.Error("  * Couldn't find plugin class '{0}'", h.ClassName);
					continue;
				}

				var plugin = (BotPlugin)Activator.CreateInstance(type);
				plugin.Discord = Client;
				plugin.Config = h.Config;
				Plugins.Add(plugin);

				Logging.Info("  * {0}", h.ClassName);

				plugin.OnInitialize();
			}

			Client.MessageReceived += Client_MessageReceived;

			Logging.Info("Ready!");

			DateTime tmLastUpdate = DateTime.Now;
			while (KeepRunnig) {
				Thread.Sleep(17);

				foreach (var plugin in Plugins) {
					plugin.OnUpdate(DateTime.Now - tmLastUpdate);
				}
				tmLastUpdate = DateTime.Now;
			}
		}

		public static void SaveConfig()
		{
			foreach (var h in Config.Handlers) {
				var plugin = Plugins.Find(p => p.GetType().FullName == h.ClassName);
				if (plugin == null) {
					continue;
				}
				h.Config = plugin.Config;
			}
			Config.Save();
		}

		private static void Client_MessageReceived(object sender, MessageEventArgs e)
		{
			if (e.User.Id == Client.CurrentUser.Id) {
				return;
			}

			string responseMessage = "";

			bool handled = false;
			foreach (var h in Plugins) {
				if (h.MessagesRequiresAdmin() && !Config.Admins.Contains(e.User.Id)) {
					continue;
				}

				string output = h.HandleMessage(e.Message);
				if (output != null) {
					handled = h.BlocksResponses();
					responseMessage += output + "\n";
					break;
				}
			}

			if (!handled) {
				var parse = e.Message.Text.Split(' ');
				foreach (var r in Config.Responses) {
					foreach (var k in r.Keywords) {
						if (!parse.Contains(k)) {
							continue;
						}
						responseMessage += r.Response + "\n";
						break;
					}
				}
			}

			responseMessage = responseMessage.Trim();

			if (responseMessage != "") {
				Logging.Info("Request from user {0}: \"{1}\"", e.User.Name, e.Message.Text);

				string sendPre = "";
				foreach (var user in e.Message.MentionedUsers) {
					sendPre += user.Mention + " ";
				}

				e.Message.Channel.SendMessage(sendPre + responseMessage);
			}

			if (QuitRequested) {
				SaveConfig();
				KeepRunnig = false;
			}
		}
	}
}
