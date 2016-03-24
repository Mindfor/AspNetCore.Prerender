using System;
using System.IO;

namespace Mindfor.AspNetCore.Util
{
	/// <summary>
	/// Creates temp file for specified content.
	/// </summary>
	public sealed class StringAsTempFile : IDisposable
	{
		bool _disposed;

		/// <summary>
		/// Gets file full path.
		/// </summary>
		public string FileName { get; private set; }

		/// <summary>
		/// Initializes new temp file.
		/// </summary>
		/// <param name="content">File content.</param>
		public StringAsTempFile(string content)
		{
			FileName = Path.GetTempFileName();
			File.WriteAllText(FileName, content);
		}

		void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (File.Exists(FileName))
					File.Delete(FileName);
				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~StringAsTempFile()
		{
			Dispose(false);
		}
	}
}