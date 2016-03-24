using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.PlatformAbstractions;

namespace Mindfor.AspNetCore
{
	/// <summary>
	/// Prerenders HTML for request.
	/// This is required for bots that does not support JS execution.
	/// </summary>
	public class PrerenderMiddleware : IDisposable
	{
		static readonly string[] PrerenderQueryKeys = new[] { "_escaped_fragment_", "prerender" };
		readonly RequestDelegate _next;
		PhantomJS _phantomJs;

		/// <summary>
		/// Creates new middleware instance.
		/// </summary>
		public PrerenderMiddleware(RequestDelegate next, IApplicationEnvironment env, string phantomJsPath)
		{
			if (next == null)
				throw new ArgumentNullException(nameof(next));
			_next = next;

			string path = Path.Combine(env.ApplicationBasePath, phantomJsPath);
			_phantomJs = new PhantomJS(path);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (_phantomJs != null)
			{
				_phantomJs.Dispose();
				_phantomJs = null;
			}
		}

		/// <summary>
		/// Invokes middleware.
		/// </summary>
		public async Task Invoke(HttpContext context)
		{
			if (ShouldPrerender(context.Request))
				await PrerenderAsync(context);
			else
				await _next(context);
		}

		/// <summary>
		/// Determines if request should be prerendered.
		/// </summary>
		/// <returns><c>True</c> if request should be prerenderer; otherwise <c>false</c>.</returns>
		protected virtual bool ShouldPrerender(HttpRequest request)
		{
			return string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
				PrerenderQueryKeys.Any(key => request.Query[key] == string.Empty);
		}

		/// <summary>
		/// Prerenders page and sends to response.
		/// </summary>
		async Task PrerenderAsync(HttpContext context)
		{
			var request = context.Request;
			var query = QueryString.Create(request.Query.Where(r => !PrerenderQueryKeys.Contains(r.Key)));
			string url = string.Concat(request.Scheme, "://", request.Host, request.Path, query);

			var result = await _phantomJs.PrerenderAsync(url);
			await SendResultAsync(context, result);
		}

		/// <summary>
		/// Sends prerender result to the response.
		/// </summary>
		async Task SendResultAsync(HttpContext context, PrerenderResult result)
		{
			var response = context.Response;
			response.StatusCode = result.StatusCode;
			CopyHeader(result, response, "Content-Type");
			CopyHeader(result, response, "Location");
			if (result.Content != null)
				await response.WriteAsync(result.Content, context.RequestAborted);
		}

		/// <summary>
		/// Copies header value from <paramref name="result"/> to <paramref name="response"/>.
		/// </summary>
		/// <param name="result">Source prerender result.</param>
		/// <param name="response">Destination response.</param>
		/// <param name="name">Header name.</param>
		void CopyHeader(PrerenderResult result, HttpResponse response, string name)
		{
			var header = result.Headers.FirstOrDefault(h => h.Name == name);
			if (header != null)
				response.Headers[header.Name] = header.Value;
		}
	}
}