using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Bubblegum.MessageHandlers
{
	public class Playing : BotPlugin
	{
		public override bool MessagesRequiresAdmin() { return true; }

		public override string HandleMessage(Message msg)
		{
			var parse = msg.RawText.SplitCommandline();

			if (parse[0] == ".playing" && parse.Length == 2) {
				Program.Client.SetGame(parse[1]);
			}

			return null;
		}
	}
}
