using System;

namespace OpusConductor
{
	/// <summary>
	/// Provides audio decoding with Opus.
	/// </summary>
	public class OpusDecoder : OpusCommon
	{
		private readonly SafeDecoderHandle _handle;
		// Number of samples in the frame size, per channel.
		private readonly int _samples;
		private readonly int _pcmLength;

		private bool _fec;

		/// <summary>
		/// Initializes a new <see cref="OpusDecoder"/> instance 48k stereo.
		/// </summary>
		public OpusDecoder() : this(60, OpusSampleRate._48k, OpusAudioChannels.Stereo, false)
		{
		}

		/// <summary>
		/// Initializes a new <see cref="OpusDecoder"/> instance, with the specified frame size, 48000 Hz sample rate and 2 channels.
		/// </summary>
		/// <param name="frameSize">The frame size used when encoding, 2.5, 5, 10, 20, 40 or 60 ms.</param>
		[Obsolete("This constructor was used for the old decode method and is deprecated, please use the new decode method instead.")]
		public OpusDecoder(double frameSize) : this(frameSize, OpusSampleRate._48k, OpusAudioChannels.Stereo, true)
		{
		}

		/// <summary>
		/// Initializes a new <see cref="OpusDecoder"/> instance, with the specified sample rate and channels.
		/// </summary>
		/// <param name="sampleRate">The sample rate to decode to, 48000, 24000, 16000, 12000 or 8000 Hz.</param>
		/// <param name="audioChannels">The channels to decode to, mono or stereo.</param>
		public OpusDecoder(OpusSampleRate sampleRate, OpusAudioChannels audioChannels) : this(60, sampleRate, audioChannels, false)
		{
		}

		/// <summary>
		/// Initializes a new <see cref="OpusDecoder"/> instance, with the specified frame size, sample rate and channels.
		/// </summary>
		/// <param name="frameSize">The frame size used when encoding, 2.5, 5, 10, 20, 40 or 60 ms.</param>
		/// <param name="sampleRate">The sample rate to decode to, 48000, 24000, 16000, 12000 or 8000 Hz.</param>
		/// <param name="audioChannels">The channels to decode to, mono or stereo.</param>
		public OpusDecoder(double frameSize, int sampleRate, int audioChannels) : this(frameSize, (OpusSampleRate)sampleRate, (OpusAudioChannel)audioChannels, true)
		{
		}

		private OpusDecoder(double frameSize, OpusSampleRate sampleRate, OpusAudioChannels audioChannels, bool frameSizeWasSpecified)
		{
			switch (frameSize) {
				case 2.5:
				case 5:
				case 10:
				case 20:
				case 40:
				case 60:
					break;
				default:
					throw new ArgumentException("Value must be one of the following: 2.5, 5, 10, 20, 40 or 60.", nameof(frameSize));
			}

			if (!Enum.IsDefined(typeof(OpusSampleRate), sampleRate)) {
				throw new ArgumentException("Value is not defined in the enumeration.", nameof(sampleRate));
			}

			if (!Enum.IsDefined(typeof(OpusAudioCHannels), audioChannels)) {
				throw new ArgumentException("Value is not defined in the enumeration.", nameof(audioChannels));
			}

			if (frameSizeWasSpecified) {
				FrameSize = frameSize;
			}

			SampleRate = sampleRate;
			AudioChannels = audioChannels;

			_samples = GetSampleCount(frameSize);
			_pcmLength = GetPCMLength(_samples);
			_handle = FFI.opus_decoder_create((int)sampleRate, (int)audioChannels, out int error);

			ThrowIfError(error);
		}

		/// <summary>
		/// Gets the frame size, or null if not specified when constructing the current instance.
		/// </summary>
		[Obsolete("This property was used for the old decode method and is deprecated, please use the new decode method instead.")]
		public double? FrameSize { get; }

		/// <summary>
		/// Gets or sets whether to use Forward Error Correction. NOTE: This can only be set if <see cref="FrameSize"/> is set,
		/// and only works if the encoder also uses Forward Error Correction. You also need to indicate when a packet has been lost
		/// (by calling <see cref="Decode(byte[], int, out int)"/> with null and -1 as the arguments).
		/// </summary>
		[Obsolete("This property was used for the old decode method and is deprecated, please use the new decode method instead.")]
		public bool ForwardErrorCorrection
		{
			get => _fec;
			set {
				if (FrameSize == null) {
					throw new InvalidOperationException("A frame size has to be specified in the constructor for Forward Error Correction to work.");
				}

				_fec = value;
			}
		}

		/// <summary>
		/// Decodes an Opus packet, or indicates packet loss (if <see cref="ForwardErrorCorrection"/> is enabled).
		/// </summary>
		/// <param name="opusBytes">The Opus packet, or null to indicate packet loss (if <see cref="ForwardErrorCorrection"/> is enabled).</param>
		/// <param name="length">The maximum number of bytes to use from <paramref name="opusBytes"/>, or -1 to indicate packet loss
		/// (if <see cref="ForwardErrorCorrection"/> is enabled).</param>
		/// <param name="decodedLength">The length of the decoded audio.</param>
		/// <returns>A byte array containing the decoded audio.</returns>
		[Obsolete("This method is deprecated, please use the new decode method instead.")]
		public unsafe byte[] Decode(byte[] opusBytes, int length, out int decodedLength)
		{
			if (opusBytes == null && !ForwardErrorCorrection) {
				throw new ArgumentNullException(nameof(opusBytes), "Value cannot be null when Forward Error Correction is disabled.");
			}

			if (length < 0 && (!ForwardErrorCorrection || opusBytes != null)) {
				throw new ArgumentOutOfRangeException(nameof(length), $"Value cannot be negative when {nameof(opusBytes)} is not null or Forward Error CorrectioForward Error Correction is disabled.");
			}

			if (opusBytes != null && opusBytes.Length < length) {
				throw new ArgumentOutOfRangeException(nameof(length), $"Value cannot be greater than the length of {nameof(opusBytes)}.");
			}

			ThrowIfDisposed();

			byte[] pcmBytes = new byte[_pcmLength];
			int result;

			fixed (byte* input = opusBytes)
			fixed (byte* output = pcmBytes)
			{
				var inputPtr = (IntPtr)input;
				var outputPtr = (IntPtr)output;

				if (opusBytes != null) {
					result = FFI.opus_decode(_handle, inputPtr, length, outputPtr, _samples, 0);
				} else {
					// If forward error correction is enabled, this will indicate a packet loss.
					result = FFI.opus_decode(_handle, IntPtr.Zero, 0, outputPtr, _samples, ForwardErrorCorrection ? 1 : 0);
				}
			}

			ThrowIfError(result);

			decodedLength = result * AudioChannels * 2;

			return pcmBytes;
		}

		/// <summary>
		/// Decodes an Opus packet or any Forward Error Correction data.
		/// </summary>
		/// <param name="opusBytes">The Opus packet, or null to indicate packet loss.</param>
		/// <param name="opusLength">The maximum number of bytes to read from <paramref name="opusBytes"/>, or -1 to indicate packet loss.</param>
		/// <param name="pcmBytes">The buffer that the decoded audio will be stored in.</param>
		/// <param name="pcmLength">The maximum number of bytes to write to <paramref name="pcmBytes"/>.
		/// When using Forward Error Correction this must be a valid frame size that matches the duration of the missing audio.</param>
		/// <returns>The number of bytes written to <paramref name="pcmBytes"/>.</returns>
		public unsafe int Decode(byte[] opusBytes, int opusLength, byte[] pcmBytes, int pcmLength)
		{
			if (opusLength < 0 && opusBytes != null) {
				throw new ArgumentOutOfRangeException(nameof(opusLength), $"Value cannot be negative when {nameof(opusBytes)} is not null.");
			}

			if (opusBytes != null && opusBytes.Length < opusLength) {
				throw new ArgumentOutOfRangeException(nameof(opusLength), $"Value cannot be greater than the length of {nameof(opusBytes)}.");
			}

			if (pcmBytes == null) {
				throw new ArgumentNullException(nameof(pcmBytes));
			}

			if (pcmLength < 0) {
				throw new ArgumentOutOfRangeException(nameof(pcmLength), "Value cannot be negative.");
			}

			if (pcmBytes.Length < pcmLength) {
				throw new ArgumentOutOfRangeException(nameof(pcmLength), $"Value cannot be greater than the length of {nameof(pcmBytes)}.");
			}

			double frameSize = GetFrameSize(pcmLength);

			if (opusBytes == null) {
				switch (frameSize) {
					case 2.5:
					case 5:
					case 10:
					case 20:
					case 40:
					case 60:
						break;
					default:
						throw new ArgumentException("When using Forward Error Correction the frame size must be one of the following: 2.5, 5, 10, 20, 40 or 60.", nameof(pcmLength));
				}
			}

			ThrowIfDisposed();

			int result;
			int samples = GetSampleCount(frameSize);

			fixed (byte* input = opusBytes)
			fixed (byte* output = pcmBytes)
			{
				var inputPtr = (IntPtr)input;
				var outputPtr = (IntPtr)output;

				if (opusBytes != null) {
					result = FFI.opus_decode(_handle, inputPtr, opusLength, outputPtr, samples, 0);
				} else {
					// If forward error correction is enabled, this will indicate a packet loss.
					result = FFI.opus_decode(_handle, IntPtr.Zero, 0, outputPtr, samples, 1);
				}
			}

			ThrowIfError(result);

			return GetPCMLength(result, AudioChannels);
		}
	}
}
