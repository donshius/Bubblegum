using Discord;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bubblegum.MessageHandlers
{
    class ScripthookVersion : BotPlugin
    {
        public const string ScripthookUrl = "http://www.dev-c.com/gtav/scripthookv/";

        public override string HandleMessage(Message msg)
        {
            var match = Regex.Match(msg.Text, @"\.(?i)([Ss]cripthook)");

            if (!match.Success)
            {
                return null;
            }

            var wc = new WebClient();
            wc.Proxy = null;
            wc.Headers[HttpRequestHeader.UserAgent] = "Bubblegum / 2.0";

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(wc.DownloadString(ScripthookUrl));

            var releasedDateNode = doc.DocumentNode.SelectSingleNode("//*[@id=\"tfhover\"]/tbody/tr[1]/td");
            var currentVersionNode = doc.DocumentNode.SelectSingleNode("//*[@id=\"tfhover\"]/tbody/tr[2]/td");

            if (releasedDateNode == null || currentVersionNode == null) return null;

            return $"Current scripthook version is {currentVersionNode.InnerText} released at {releasedDateNode.InnerText}";
        }
    }
}
