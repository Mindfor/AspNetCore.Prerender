using System;
using System.IO;
using System.Reflection;

namespace Mindfor.AspNetCore.Util
{
	/// <summary>
	/// Provides reading from embedded resources.
	/// </summary>
	public static class EmbeddedResourceReader
	{
		/// <summary>
		/// Reads content of the embedded resource.
		/// </summary>
		/// <param name="assemblyContainingType">Assemby type where resource is located.</param>
		/// <param name="path">Resource path inside assembly.</param>
		/// <returns>Resource content.</returns>
		public static string Read(Type assemblyContainingType, string path)
		{
			var asm = assemblyContainingType.GetTypeInfo().Assembly;
			var resourceName = asm.GetName().Name + "." + path.TrimStart('/').Replace("/", ".");

			using (var stream = asm.GetManifestResourceStream(resourceName))
			using (var sr = new StreamReader(stream))
			{
				return sr.ReadToEnd();
			}
		}
	}
}