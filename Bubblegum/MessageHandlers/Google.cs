using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Text.RegularExpressions;

namespace Bubblegum.MessageHandlers
{
	public class Google : BotPlugin
	{
		public override string HandleMessage(Message msg)
		{
			var match = Regex.Match(msg.Text, @"\.(?i)(google|lmgtfy) (.*)");

			if (!match.Success) {
				return null;
			}

			if (match.Groups[1].Value == "lmgtfy") {
				return "https://lmgtfy.com/?q=" + Uri.EscapeUriString(match.Groups[2].Value) + " :angry:";
			} else {
				return "https://google.com/search?q=" + Uri.EscapeUriString(match.Groups[2].Value) + " :mag:";
			}
		}
	}
}
