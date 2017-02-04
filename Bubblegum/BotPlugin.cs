using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Bubblegum
{
	public abstract class BotPlugin
	{
		public DiscordClient Discord { get; set; }

		public object Config { get; set; }

		public virtual bool MessagesRequiresAdmin() { return false; }
		public virtual bool BlocksResponses() { return true; }
		public virtual string HandleMessage(Message msg) { return null; }

		public virtual void OnInitialize() { }
		public virtual void OnUpdate(TimeSpan dt) { }
	}
}
