using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mindfor.AspNetCore.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Mindfor.AspNetCore
{
	/// <summary>
	/// Starts PhantomJS in another process and provides access to the prerender function via HTTP.
	/// </summary>
	public class PhantomJS : IDisposable
	{
		const string OutputInit = "[Initialized]";
		static readonly Regex PortMessageRegex = new Regex(@"^\[Port:(?<port>\d+)\]$");
		static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
		{
			ContractResolver = new CamelCasePropertyNamesContractResolver()
		};

		readonly string _phantomJsPath;
		readonly StringAsTempFile _script;
		object _childProcessLauncherLock = new object();
		bool _disposed;
		bool _initialized;
		Process _phantomProcess;
		TaskCompletionSource<bool> _phantomProcessReady;
		int _port;

		/// <summary>
		/// Initializes new PhantomJS proxy.
		/// </summary>
		/// <param name="phantomJsPath">Path to the PhantomJS executable.</param>
		public PhantomJS(string phantomJsPath)
		{
			_phantomJsPath = phantomJsPath;
			_script = new StringAsTempFile(EmbeddedResourceReader.Read(typeof(PhantomJS), "Content/app.js"));
		}

		/// <summary>
		/// Starts PhantomJS if it is not running. Ensures that one started successful.
		/// If PhantomJS is already running then do nothing.
		/// </summary>
		/// <remarks>
		/// Every public method runs current one so you are not obliged to invoke this method on your own.
		/// </remarks>
		public async Task EnsureReadyAsync()
		{
			lock (_childProcessLauncherLock)
			{
				if (_phantomProcess == null || _phantomProcess.HasExited)
					StartPhantomProcess();
			}

			var task = _phantomProcessReady.Task;
			if (!await task)
				throw new InvalidOperationException("The PhantomJS process failed to initialize", task.Exception);
		}

		/// <summary>
		/// Prerenders specified <paramref name="url"/> via PhantomJS.
		/// </summary>
		/// <param name="url">URL to prerender.</param>
		public async Task<PrerenderResult> PrerenderAsync(string url)
		{
			await EnsureReadyAsync();
			return await InvokePhantom(url);
		}

		/// <summary>
		/// Invokes PhantomJS prerender method.
		/// </summary>
		/// <param name="url">URL to prerender.</param>
		protected async Task<PrerenderResult> InvokePhantom(string url)
		{
			using (var client = new HttpClient())
			{
				var response = await client.GetAsync($"http://localhost:{_port}/{url}");
				response.EnsureSuccessStatusCode();

				string responseString = await response.Content.ReadAsStringAsync();
				bool responseIsJson = response.Content.Headers.ContentType.MediaType == "text/json";
				if (!responseIsJson)
					throw new ArgumentException($"PhantomJS responded with non-JSON string. This cannot be converted to the type: {typeof(PrerenderResult).FullName}");
				return JsonConvert.DeserializeObject<PrerenderResult>(responseString);
			}
		}

		/// <summary>
		/// Starts new PhantomJS process.
		/// </summary>
		void StartPhantomProcess()
		{
			_phantomProcessReady = new TaskCompletionSource<bool>();
			_initialized = false;

			// start process
			var startInfo = new ProcessStartInfo()
			{
				FileName = _phantomJsPath,
				Arguments = $"\"{_script.FileName}\" {GetAvailableNetworkPort()}",
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			_phantomProcess = Process.Start(startInfo);

			// connect to IO streams
			_phantomProcess.OutputDataReceived += Process_OutputDataReceived;
			_phantomProcess.ErrorDataReceived += Process_ErrorDataReceived;
			_phantomProcess.BeginOutputReadLine();
			_phantomProcess.BeginErrorReadLine();
		}

		void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
				return;

			if (!_initialized)
			{
				// initialized message
				if (e.Data == OutputInit)
				{
					_phantomProcessReady.SetResult(true);
					_initialized = true;
				}

				// port message
				var match = PortMessageRegex.Match(e.Data);
				if (match.Success && _port == 0)
					_port = int.Parse(match.Groups["port"].Value);
			}
		}

		void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data == null)
				return;
			if (!_initialized)
			{
				_phantomProcessReady.SetResult(false);
				_initialized = true;
			}
		}

		/// <summary>
		/// Retruns random available network port.
		/// </summary>
		int GetAvailableNetworkPort()
		{
			var rnd = new Random();
			int port;
			do
			{
				port = rnd.Next(1000, 65535);
			}
			while (!IsPortAvailable(port));
			return port;
		}

		/// <summary>
		/// Determines if <paramref name="port"/> is available.
		/// </summary>
		bool IsPortAvailable(int port)
		{
			// Evaluate current system tcp connections. This is the same information provided
			// by the netstat command line application, just in .Net strongly-typed object
			// form.  We will look through the list, and if our port we would like to use
			// in our TcpClient is occupied, we will set isAvailable to false.
			var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			var connections = ipGlobalProperties.GetActiveTcpConnections();

			foreach (var connection in connections)
			{
				if (connection.LocalEndPoint.Port == port)
					return false;
			}
			return true;
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
					_script.Dispose();
				if (_phantomProcess != null && !_phantomProcess.HasExited)
					_phantomProcess.Kill();

				_disposed = true;
			}
		}

		~PhantomJS()
		{
			Dispose(false);
		}
	}
}