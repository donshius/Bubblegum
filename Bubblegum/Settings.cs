using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Bubblegum
{
	public class SettingsApi
	{
		public string Token { get; set; }
	}

	public class SettingsResponse
	{
		public List<string> Keywords { get; set; }
		public string Response { get; set; }

		public SettingsResponse()
		{
			Keywords = new List<string>();
		}
	}

	[XmlInclude(typeof(MessageHandlers.NewsConfig))]
	[XmlInclude(typeof(MessageHandlers.NativeDbConfig))]
	[XmlInclude(typeof(MessageHandlers.BuildUpdateConfig))]
	public class SettingsPlugin
	{
		public string ClassName { get; set; }
		public object Config { get; set; }
	}

	[Serializable]
	public class Settings
	{
		private string m_filename;

		public SettingsApi API { get; set; }

		public bool UseShortUrls { get; set; }
		public string ShortUrlApiKey { get; set; }

		public List<ulong> Admins { get; set; }

		public List<SettingsPlugin> Handlers { get; set; }
		public List<SettingsResponse> Responses { get; set; }

		public Settings()
		{
			API = new SettingsApi();
			API.Token = "put a token here";
		}

		public void Save()
		{
			var serializer = new XmlSerializer(typeof(Settings));
			using (var fs = File.OpenWrite(m_filename)) {
				serializer.Serialize(fs, this);
			}
		}

		public void Save(string filename)
		{
			m_filename = filename;
			Save();
		}

		public static Settings FromFile(string filename)
		{
			var serializer = new XmlSerializer(typeof(Settings));
			using (var fs = File.OpenRead(filename)) {
				var ret = (Settings)serializer.Deserialize(fs);
				ret.m_filename = filename;
				return ret;
			}
		}
	}
}
