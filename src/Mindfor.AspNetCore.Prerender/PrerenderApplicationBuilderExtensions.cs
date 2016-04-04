using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
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
		/// Middleware prerenders pages via local PhantomJS.
		/// </summary>
		/// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
		/// <param name="phantomExecutablePath">Path to the PhantomJS executable.</param>
		public static IApplicationBuilder UsePrerender(this IApplicationBuilder app, string phantomExecutablePath)
		{
			var env = app.ApplicationServices.GetRequiredService<IApplicationEnvironment>();
			string path = Path.Combine(env.ApplicationBasePath, phantomExecutablePath);
			return app.UseMiddleware<PrerenderMiddleware>(new PhantomJS(path));
		}

		/// <summary>
		/// Adds <see cref="PrerenderMiddleware"/> to the request execution pileline.
		/// Middleware prerenders pages via remote prerender server.
		/// </summary>
		/// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
		/// <param name="serverUrl">Remote prerender server URL.</param>
		/// <param name="securityKey">
		/// Security key required for remote server.
		/// This key will be send in POST json data as "key" field.
		/// </param>
		public static IApplicationBuilder UsePrerender(this IApplicationBuilder app, string serverUrl, string securityKey)
		{
			return app.UseMiddleware<PrerenderMiddleware>(new RemotePrerenderProvider(serverUrl, securityKey));
		}
	}
}