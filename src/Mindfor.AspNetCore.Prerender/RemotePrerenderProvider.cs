using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Mindfor.AspNetCore
{
	/// <summary>
	/// Prerender URLs via remote server.
	/// </summary>
	public class RemotePrerenderProvider : IPrerenderProvider, IDisposable
	{
		const string JsonMediaType = "text/json";
		readonly HttpClient _client;
		readonly string _securityKey;

		/// <summary>
		/// Initializes new instance.
		/// </summary>
		/// <param name="url">Remote prerender server URL.</param>
		/// <param name="securityKey">
		/// Security key required for remote server.
		/// This key will be send in POST json data as "key" field.
		/// </param>
		public RemotePrerenderProvider(string url, string securityKey)
		{
			_client = new HttpClient
			{
				BaseAddress = new Uri(url)
			};
			_securityKey = securityKey;
		}

		/// <inheritdoc/>
		public virtual void Dispose()
		{
			_client.Dispose();
		}

		/// <summary>
		/// Prerenders specified <paramref name="url"/> via remove server.
		/// </summary>
		/// <param name="url">URL to prerender which will be send in POST json data as "url" field.</param>
		public async Task<PrerenderResult> PrerenderAsync(string url)
		{
			var args = new
			{
				url,
				key = _securityKey
			};
			var content = new StringContent(JsonConvert.SerializeObject(args), Encoding.UTF8, JsonMediaType);
			var response = await _client.PostAsync("/", content);
			response.EnsureSuccessStatusCode();

			string responseString = await response.Content.ReadAsStringAsync();
			bool responseIsJson = response.Content.Headers.ContentType.MediaType == JsonMediaType;
			if (!responseIsJson)
				throw new ArgumentException($"Remove server responded with non-JSON string. This cannot be converted to the type: {typeof(PrerenderResult).FullName}");
			return JsonConvert.DeserializeObject<PrerenderResult>(responseString);
		}
	}
}