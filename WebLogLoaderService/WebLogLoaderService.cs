using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using System.IO;
using System.Text;
using System.Xml;
using System.Net;



namespace WebLogLoaderService
{

	public class WebLogLoaderService : System.ServiceProcess.ServiceBase
	{
		/// <summary> 
		/// Loads  WebLogLoaderService.xml 
		/// Loads parameters about the Innovator Server + DB 
		/// wakes up every few minutes,  connects to  server and loads data from the web logs
		/// 
		/// </summary>
		private System.ComponentModel.Container components = null;
		static bool _runAsService = true;
		public static String Version = "1.1";
		public static Int32 TIMER_INTERVAL = 1;
		public static Int32 LOGGING_LEVEL  = 1;    //  0 or >= 1		
		CookieContainer sessionCookies = new CookieContainer();

		System.Diagnostics.EventLog MyEventLog = new System.Diagnostics.EventLog();
		
		Timer _timer = null; // the one minute timer by default
		ServerInfo Job = null; // the Job definition

		/// This is the timer event handler
		/// Log the event if logging = 1 and then check if there are nay jobs to run
		public void OnNextMinute(object state)
		{
			if (Job==null)
			{
				MyEventLog.WriteEntry("Terminating - no jobs to process",EventLogEntryType.Error);
				ServiceController serviceMonitor = new ServiceController("WebLogLoaderService");
				serviceMonitor.Stop();
				return;
			}
			DateTime currentTime = DateTime.Now;
			string[] DaysEnum = {"Sunday","Monday","Tuesday","Wednesday","Thursday","Friday","Saturday"};
			int DaysInMonth = DateTime.DaysInMonth(currentTime.Year, currentTime.Month);

			int DayInt = 0;
			for (DayInt=0; DayInt<7 && DaysEnum[DayInt] !=currentTime.DayOfWeek.ToString(); DayInt++);
			if (DayInt==7) DayInt=0;

			if (LOGGING_LEVEL > 1) MyEventLog.WriteEntry("Service Wakeup at " + currentTime + "\n  HOUR="+currentTime.Hour + "\n  MINUTE=" + currentTime.Minute + "\n  DAY=" + DayInt.ToString() );

			RunJob(Job.Server,Job.Database,Job.Username,Job.Password,sessionCookies);
		}

		// This method sends a request to the Innovator Server
		public bool SendRequestToInnovator( string AML, 
											string SOAPAction, //Examples :"ApplyItem", "Logoff"
											XmlDocument outDom, 
											string innovatorServerURL,
											string userName,
											string userPassword,
											string databaseName,
											CookieContainer sessionCookies)
		{
			StreamWriter lWriter=null;
			StreamReader lReader=null;
			Stream reqstream=null;
			Stream resstream=null;
			HttpWebResponse wResp=null;
			string lResult="";
			bool ErrorExists=false;
			HttpWebRequest wReq;
					
			XmlDocument inDom = new XmlDocument();
			// Store the AML in an XML Document
			inDom.LoadXml(AML);
			// Create the HTTP Request to the Innovator Server
			try
			{
				wReq = (HttpWebRequest) WebRequest.Create(innovatorServerURL+"/server/innovatorServer.aspx");
				wReq.CookieContainer = sessionCookies;
				wReq.Method = "POST";
				wReq.ContentLength = inDom.DocumentElement.OuterXml.Length;
				wReq.ContentType = "text/xml";
				wReq.Headers.Add("SOAPAction", SOAPAction);
				wReq.Headers.Add("AUTHUSER", userName);
				wReq.Headers.Add("AUTHPASSWORD", userPassword);
				wReq.Headers.Add("DATABASE", databaseName);
			}
			catch (Exception e) 
			{
				MyEventLog.WriteEntry("Error occurred initializing header request to Innovator. Error Code: " + e.Message);
				ErrorExists = true;
				return(ErrorExists);
			}

			try
			{
				reqstream = wReq.GetRequestStream();
				lWriter = new StreamWriter(reqstream);
				// Send the request to Innovator
				lWriter.Write(inDom.DocumentElement.OuterXml);
			}
			catch (Exception e) 
			{
				if (LOGGING_LEVEL > 0) MyEventLog.WriteEntry("Error occurred sending Item Request to Innovator. Error Code: " + e.Message);
				ErrorExists = true;
				return(ErrorExists);
			}
			finally
			{
				lWriter.Close();
				reqstream.Close();
			}
			
			try
			{
				// Get the Response message from Innovator
				wResp = (HttpWebResponse) wReq.GetResponse();
				// Check the response message from Innovator
				if (wReq.HaveResponse)
				{
					resstream = wResp.GetResponseStream();
					lReader = new StreamReader(resstream);
					lResult = lReader.ReadToEnd(); 
					if (LOGGING_LEVEL > 0) MyEventLog.WriteEntry("Innovator Response:  " + lResult); 
				}
				else
				{
					if (LOGGING_LEVEL > 0) MyEventLog.WriteEntry("No response sent from Innovator Server for Item Request."); 
					ErrorExists = true;
					return(ErrorExists);
				}
			}
			catch (Exception e)
			{
				if (LOGGING_LEVEL > 0) MyEventLog.WriteEntry("Error occurred reading Item Response from Innovator. Error Code: " + e.Message);
				ErrorExists = true;
				return(ErrorExists);
			}
			finally
			{
				lReader.Close();
				resstream.Close();
				wResp.Close();
			}

			// Now load the XML response in the outDom argument
			try
			{
				outDom.LoadXml(lResult);
			}
			catch (XmlException e)
			{
				if (LOGGING_LEVEL > 0) MyEventLog.WriteEntry("Invalid response from Innovator Server. Error Code: " + e.Message);
				ErrorExists = true;
				return(ErrorExists);
			}

			return(ErrorExists);
		} // end of send-to-innovator function

		private void RunJob(String innovatorServerURL,String database,String loginName,String password,CookieContainer sessionCookies)
		{
			string AML="";
			bool errorInnovatorReq=false;
			XmlDocument responseDom = new XmlDocument();

			DateTime currentTime = DateTime.Now;
			String timestamp = currentTime.Year + "." + currentTime.Month + "." + currentTime.Day + "." + currentTime.Hour + "." + currentTime.Minute;
			String PATH1= "c:\\Inetpub\\logs\\Visitor.log";
			String PATH2= "c:\\Inetpub\\logs\\Old-Logs\\Visitor." + timestamp + ".log";

	        if ( !File.Exists(PATH1) )
			{
				if (LOGGING_LEVEL > 1) MyEventLog.WriteEntry("Cannot find the Web Log File: " + PATH1);
				return;
			}


			if ( File.Exists(PATH2) )
			{
				if (LOGGING_LEVEL > 1) MyEventLog.WriteEntry("Renamed Log File already Exists: " + PATH2);
				return;
			}

			// rename the file first - then open the new file
			try
			{
				File.Move(PATH1,PATH2);
			}
			catch (Exception  ex)
			{
				MyEventLog.WriteEntry("Log File Rename Error: " + ex.ToString() );
				return;
			}

			// FILE was moved to the Old-Logs folder,  now open the file and loop through it.
			StreamReader sr = File.OpenText(PATH2);
			string input = null;

			string delimStr  = ",";
			char[] delimiter= delimStr.ToCharArray();
			string[] splitter;
			string cookie = null;

			while ((input = sr.ReadLine()) != null)
			{
				splitter=input.Split(delimiter,4);
				if (cookie ==null || cookie !=splitter[0]) 
				{
					if (cookie != null)
					{
						// write the previous transaction to the server
						AML = AML + "</Relationships></Item>"; 
						if (LOGGING_LEVEL > 1) MyEventLog.WriteEntry("Sending " + AML + " to Innovator");

						errorInnovatorReq = SendRequestToInnovator(AML, 
							"ApplyItem",
							responseDom, 
							innovatorServerURL,										
							loginName,
							password,
							database,
							sessionCookies);
					}

					AML = "<Item type='WebCookie' action='merge' id='" + splitter[0] + "'><cookie>" + splitter[0] + "</cookie><Relationships>";
					AML = AML + "<Item type='WebCookie Visit' action='add'><webpage><![CDATA[" + splitter[1] + "]]></webpage><visitdate><![CDATA[" + splitter[2] + "]]></visitdate></Item>"; 
				}
				else 
				{
					// append this cookie visit to a growing transaction
					AML = AML + "<Item type='WebCookie Visit' action='add'><webpage><![CDATA[" + splitter[1] + "]]></webpage><visitdate><![CDATA[" + splitter[2] + "]]></visitdate></Item>"; 
				}
				cookie = splitter[0];
			}
			// when we fallout of the loop there may be a partial AML ready to send

			if (AML !="" )
			{
				// write the previous transaction to the server
				AML = AML + "</Relationships></Item>"; 
				if (LOGGING_LEVEL > 0) MyEventLog.WriteEntry("Sending " + AML + " to Innovator");

				errorInnovatorReq = SendRequestToInnovator(AML, 
					"ApplyItem",
					responseDom, 
					innovatorServerURL,										
					loginName,
					password,
					database,
					sessionCookies);
			}
			sr.Close();
		}

	
		private void StartTimer()
		{
			_timer.Change(0, (30000 * TIMER_INTERVAL) );
		}

		public WebLogLoaderService()
		{
			// This call is required by the Windows.Forms Component Designer.
			InitializeComponent();

			// setup the event monitor logging
			if ( !System.Diagnostics.EventLog.SourceExists("WebLogLoaderService") ) 
				System.Diagnostics.EventLog.CreateEventSource("WebLogLoaderService", "Application");
			MyEventLog.Source="WebLogLoaderService";
		
			// Load the XML configuration file into an array
			Job = ConfigurationReader.Reader.GetJob();

			// build the message for the Event Monitor
			String MyStr="Innovator WebLogLoaderService Version " + Version + " Startup\nTime Interval: " + TIMER_INTERVAL + " minutes    Logging Level: " + LOGGING_LEVEL ;
			if (Job == null) 
			{
				MyStr += "\n\nError with configuration file.\nFile not found in Windows directory or missing tags";
				MyEventLog.WriteEntry(MyStr,EventLogEntryType.Error);
			}
			else
			{
				MyStr += "\nServer: " + Job.Server + "  username:" + Job.Username;
				MyEventLog.WriteEntry(MyStr);
			}
			// Subscribe to the timer
			_timer = new Timer(new TimerCallback(OnNextMinute), null, Timeout.Infinite, Timeout.Infinite);
		}

		static void Main(string[] args)
		{

			System.ServiceProcess.ServiceBase[] ServicesToRun;
			if (_runAsService)
			{
				ServicesToRun = new System.ServiceProcess.ServiceBase[] { new WebLogLoaderService() };
				System.ServiceProcess.ServiceBase.Run(ServicesToRun);
			}
			else
			{
				WebLogLoaderService s = new WebLogLoaderService();
			    s.OnStart(null);
				Console.WriteLine("Hit return to continue...");
				Console.ReadLine();
			}
		}

		/// Required method for Designer support - do not modify the contents of this method with the code editor.
		private void InitializeComponent()
		{
			// InnovatorService
			this.ServiceName = "WebLogLoaderService";
		}

		/// Clean up any resources being used.
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// Set things in motion so your service can do its work.
		protected override void OnStart(string[] args)
		{
			StartTimer(); // begin checking for Jobs to run
		}
 
		/// Stop this service.
		protected override void OnStop()
		{
			MyEventLog.WriteEntry("Service Shutdown");
		}
	}
}
