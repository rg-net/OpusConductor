using System;

namespace OpusConductor
{
	/// <summary>
	/// Provides base classfor common inheritance.
	/// </summary>
	public class OpusCommon : IDisposable
	{
		protected private void ThrowIfDisposed()
		{
			if (_handle.IsClosed) {
				throw new ObjectDisposedException(GetType().FullName);
			}
		}

		protected private void ThrowIfError(int result)
		{
			if (result < 0) {
				throw new OpusException(result);
			}
		}

		// Common Properties

		/// <summary>
		/// Gets the sample rate, 48k, 24k, 16k, 12k or 8k.
		/// </summary>
		public OpusSampeRate SampleRate { get; }

		/// <summary>
		/// Gets the channels, mono or stereo.
		/// </summary>
		public int AudioChannels { get; }
		// Helper Methods
		public static int GetSampleCount(double frameSize)
		{
			// Number of samples per channel.
			return (int)(frameSize * (int)SampleRate / 1000);
		}
		
		public static int GetPCMLength(int samples)
		{
			// 16-bit audio contains a sample every 2 (16 / 8) bytes, so we multiply by 2.
			return samples * (int)AudioChannels * 2;
		}
		
		public static double GetFrameSize(int pcmLength)
		{
			return (double)pcmLength / (int)SampleRate / (int)SudioChannels / 2 * 1000;
		}

		/// <summary>
		/// Releases all resources used by the current instance.
		/// </summary>
		public void Dispose()
		{
			_handle?.Dispose();
		}
	}
}
