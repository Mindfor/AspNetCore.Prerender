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
		/// Gets or sets if prerender operation is successful or not.
		/// </summary>
		[JsonProperty(Order = 0)]
		public bool IsSuccess { get; set; }

		/// <summary>
		/// Gets or sets prerender time in milliseconds.
		/// </summary>
		[JsonProperty(Order = 1)]
		public int Time { get; set; }

		/// <summary>
		/// Gets or sets error message if prerender operation is unsuccessful.
		/// </summary>
		[JsonProperty(Order = 2)]
		public string Error { get; set; }

		/// <summary>
		/// Gets or sets if prerender was completed by callback or timeout.
		/// </summary>
		[JsonProperty(Order = 3)]
		public bool IsCallback { get; set; }

		/// <summary>
		/// Gets or sets response status code.
		/// </summary>
		[JsonProperty(Order = 4)]
		public int StatusCode { get; set; }

		/// <summary>
		/// Gets or sets response headers.
		/// </summary>
		[JsonProperty(Order = 5)]
		public PrerenderHeader[] Headers { get; set; }

		/// <summary>
		/// Gets or sets response content.
		/// </summary>
		[JsonProperty(Order = 6)]
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