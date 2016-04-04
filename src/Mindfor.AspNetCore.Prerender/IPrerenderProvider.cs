using System.Threading.Tasks;

namespace Mindfor.AspNetCore
{
	/// <summary>
	/// Provider that can prerender URLs.
	/// </summary>
	public interface IPrerenderProvider
	{
		/// <summary>
		/// Prerenders specified <paramref name="url"/>.
		/// </summary>
		/// <param name="url">URL to prerender.</param>
		Task<PrerenderResult> PrerenderAsync(string url);
	}
}