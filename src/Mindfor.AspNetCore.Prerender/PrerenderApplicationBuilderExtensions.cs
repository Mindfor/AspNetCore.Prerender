using Mindfor.AspNetCore;

namespace Microsoft.AspNetCore.Builder
{
	/// <summary>
	/// Extension methods for <see cref="IApplicationBuilder"/> to add prerender to the request execution pipeline.
	/// </summary>
	public static class PrerenderApplicationBuilderExtensions
	{
		/// <summary>
		/// Adds <see cref="PrerenderMiddleware"/> to the request execution pileline.
		/// </summary>
		/// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
		/// <param name="phantomJsPath">Path to the PhantomJS executable.</param>
		public static IApplicationBuilder UsePrerender(this IApplicationBuilder app, string phantomJsPath)
		{
			return app.UseMiddleware<PrerenderMiddleware>(phantomJsPath);
		}
	}
}