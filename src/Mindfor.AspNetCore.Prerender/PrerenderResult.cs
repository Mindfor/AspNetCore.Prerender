using Newtonsoft.Json;

namespace Mindfor.AspNetCore
{
	/// <summary>
	/// Provides PhantomJS prerender information.
	/// </summary>
	[JsonObject]
	public class PrerenderResult
	{
		/// <summary>
		/// Gets or sets response status code.
		/// </summary>
		[JsonProperty(Order = 0)]
		public int StatusCode { get; set; }

		/// <summary>
		/// Gets or sets response headers.
		/// </summary>
		[JsonProperty(Order = 1)]
		public PrerenderHeader[] Headers { get; set; }

		/// <summary>
		/// Gets or sets response content.
		/// </summary>
		[JsonProperty(Order = 2)]
		public string Content { get; set; }
	}

	/// <summary>
	/// Represents single prerender header.
	/// </summary>
	[JsonObject]
	public class PrerenderHeader
	{
		/// <summary>
		/// Gets or sets header name.
		/// </summary>
		[JsonProperty(Order = 0)]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets header value.
		/// </summary>
		[JsonProperty(Order = 1)]
		public string Value { get; set; }
	}
}