using System;
using System.Xml;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using Encoding = System.Text.Encoding;
using System.Text;

namespace WebLogLoaderService
{
	public class ServerInfo
	{
		public string  Server;
		public string  Database;
		public string  Username;
		public string  Password;

		public ServerInfo(string server, string database, string username, string password)
		{
			Server   = server;
			Database = database;
			Username = username;
			Password = password;
		}
	}

 	/// Reads the configuration information
	public class ConfigurationReader
	{
		static ConfigurationReader _configurationReader = new ConfigurationReader();
		static public ConfigurationReader Reader
		{
			get
			{
				return _configurationReader;
			}
		}
		public String ConvertPasswordToMD5 (String plainPasswd)
		{
			string MD5Passwd="";
			
			MD5 md5 = new MD5CryptoServiceProvider();
			ASCIIEncoding ascii = new ASCIIEncoding();
			byte[] data   = ascii.GetBytes(plainPasswd);
			byte[] result = md5.ComputeHash(data);
			// Convert the MD5 result to Hexadecimal string
			MD5Passwd = BinaryToHex(result);
			return (MD5Passwd.ToLower());
		}

		// Use this function to convert your MD5
		// hash 16 bytes array to 32 hexadecimals string.
		// Note: This code taken from www.gotdotnet.com - Topic: Function to convert your MD5 16 byte hash to 32 hexadecimals 
		public String BinaryToHex(byte[] BinaryArray) 
		{
			string result = "";
			long lowerByte;
			long upperByte;
			
			foreach(Byte singleByte in BinaryArray) 
			{
				lowerByte = singleByte & 15;
				upperByte = singleByte >> 4;
				
				result += NumberToHex(upperByte);
				result += NumberToHex(lowerByte);
			}
			return result;
		}

		// Convert the number to hexadecimal
		// Note: This code taken from www.gotdotnet.com - Topic: Function to convert your MD5 16 byte hash to 32 hexadecimals 
		private static char NumberToHex(long Number) 
		{
			if (Number>9) 
				return Convert.ToChar(65 + (Number-10));
			else
				return Convert.ToChar(48 + Number);
		}

		// This method generates a unique id.  This is used for AML sent to the vault server
		private static string NewID()
		{
			string genID="";
			genID = Guid.NewGuid().ToString("N").ToUpper();				
			return (genID);
		}
		public ServerInfo GetJob()
		{
			XmlDocument doc = new XmlDocument();
			
			doc.Load(@"WebLogLoaderService.xml");
			XmlNode nodeTest;
			XmlElement root = doc.DocumentElement;
			XmlNode rootNode = root.SelectSingleNode("/WebLogLoaderService");

			// test for the nodes,  for more graceful error handling.
			nodeTest = rootNode.SelectSingleNode("server");
			if (nodeTest==null)return null;
			nodeTest = rootNode.SelectSingleNode("database");
			if (nodeTest==null)return null;
			nodeTest = rootNode.SelectSingleNode("username");
			if (nodeTest==null)return null;
			nodeTest = rootNode.SelectSingleNode("password");
			if (nodeTest==null)return null;
			nodeTest = rootNode.SelectSingleNode("eventLoggingLevel");
			if (nodeTest==null)return null;
			nodeTest = rootNode.SelectSingleNode("intervalMinutes");
			if (nodeTest==null)return null;

			ServerInfo p = new ServerInfo(
				rootNode["server"].InnerText, 
				rootNode["database"].InnerText, 
				rootNode["username"].InnerText, 			 
				ConvertPasswordToMD5(rootNode["password"].InnerText));
			
			WebLogLoaderService.LOGGING_LEVEL = System.Convert.ToInt32(rootNode["eventLoggingLevel"].InnerText);
			WebLogLoaderService.TIMER_INTERVAL =System.Convert.ToInt32(rootNode["intervalMinutes"].InnerText);
			return p;
		}
	}
}
