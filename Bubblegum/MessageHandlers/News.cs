using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Discord;
using HtmlAgilityPack;

namespace Bubblegum.MessageHandlers
{
	public class NewsConfig
	{
		public class Feed
		{
			public ulong Channel { get; set; }
			public string Name { get; set; }
			public string URL { get; set; }
			public DateTime LastPoll { get; set; }
		}

		public List<Feed> Feeds { get; set; } = new List<Feed>();
	}

	public class News : BotPlugin
	{
		private DateTime m_tmLastCheck = DateTime.Now;
		private int m_checkInterval = 300;

		public override bool MessagesRequiresAdmin() { return true; }

		public new NewsConfig Config
		{
			get {
				if (base.Config == null) {
					base.Config = new NewsConfig();
				}
				return (NewsConfig)base.Config;
			}
		}

		private string RemoveUnwantedTags(string data)
		{
			if (string.IsNullOrEmpty(data)) return string.Empty;

			var document = new HtmlDocument();
			document.LoadHtml(data);

			var nodes = new Queue<HtmlNode>(document.DocumentNode.SelectNodes("./*|./text()"));
			while (nodes.Count > 0) {
				var node = nodes.Dequeue();
				var parentNode = node.ParentNode;

				if (node.Name != "#text") {
					var childNodes = node.SelectNodes("./*|./text()");

					if (childNodes != null) {
						foreach (var child in childNodes) {
							nodes.Enqueue(child);
							parentNode.InsertBefore(child, node);
						}
					}
					parentNode.RemoveChild(node);
				}
			}

			return document.DocumentNode.InnerHtml;
		}

		private void CheckFeeds()
		{
			foreach (var f in Config.Feeds) {
				var newItems = new List<SyndicationItem>();

				try {
					using (var reader = XmlReader.Create(f.URL)) {
						var feed = SyndicationFeed.Load(reader);
						foreach (var item in feed.Items) {
							if (item.LastUpdatedTime.UtcDateTime < f.LastPoll && item.PublishDate.UtcDateTime < f.LastPoll) {
								continue;
							}
							newItems.Add(item);
						}
					}
				} catch (Exception ex) {
					Logging.Error("Failed downloading syndication feed for {0}: {1}", f.Name, ex.ToString());
					continue;
				}

				if (newItems.Count == 0) {
					continue;
				}

				var channel = Discord.GetChannel(f.Channel);
				if (channel == null) {
					Logging.Error("Couldn't find channel to send syndication update to for {0}", f.Name);
					continue;
				}

				var verbUpdates = "update";
				if (newItems.Count > 1) {
					verbUpdates += "s";
				}

				var message = ":two_hearts: **" + f.Name + "** " + verbUpdates + ":\n\n";
				foreach (var item in newItems) {
					if (message.Length >= 1800) {
						channel.SendMessage(message + "(cont)");
						message = "";
					}

					var latestTime = item.LastUpdatedTime.UtcDateTime;
					if (item.PublishDate.UtcDateTime > latestTime) {
						latestTime = item.PublishDate.UtcDateTime;
					}
					message += "- " + latestTime.ToString("HH:mm:ss") + ": `" + item.Title.Text + "`";
					if (item.Authors.Count > 0 && item.Authors[0].Name != null && item.Authors[0].Name.Length > 0) {
						message += " by *" + item.Authors[0].Name + "*";
					}
					if (item.Links != null && item.Links.Count > 0) {
						message += " " + Program.ShortUrl(item.Links[0].Uri.ToString());
					}
					message += "\n";
				}

				f.LastPoll = DateTime.UtcNow;

				channel.SendMessage(message);
			}

			Program.SaveConfig();
		}

		public override string HandleMessage(Message msg)
		{
			var parse = msg.RawText.SplitCommandline();

			if (parse[0] == ".subscribeNews" && parse.Length == 3) {
				if (parse[1].StartsWith("http")) {
					return "**First parameter must be the feed name, not URL.**";
				}

				Config.Feeds.Add(new NewsConfig.Feed() {
					Channel = msg.Channel.Id,
					LastPoll = DateTime.UtcNow,
					Name = parse[1],
					URL = parse[2]
				});
				Program.SaveConfig();

				return "**Subscribed to feed " + parse[1] + "**.";

			} else if (parse[0] == ".unsubscribeNews" && parse.Length == 2) {
				var feed = Config.Feeds.Find(f => f.Name == parse[1]);
				if (feed == null) {
					return "**No feed found with that name.**";
				}

				Config.Feeds.Remove(feed);
				Program.SaveConfig();

				return "**Unsubscribed from feed " + parse[1] + "**.";

			} else if (parse[0] == ".checkNews") {
				CheckFeeds();
			}

			return null;
		}

		public override void OnInitialize()
		{
			CheckFeeds();
		}

		public override void OnUpdate(TimeSpan dt)
		{
			var tmLastUpdate = (DateTime.Now - m_tmLastCheck);
			if (tmLastUpdate.TotalSeconds > m_checkInterval) {
				CheckFeeds();
				Logging.Info("News feeds checked!");
				m_tmLastCheck = DateTime.Now;
			}
		}
	}
}
