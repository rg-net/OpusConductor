using System;

namespace OpusConductor
{
	/// <summary>
	/// Provides audio encoding with Opus.
	/// </summary>
	public class OpusEncoder : OpusCommon
	{
		private readonly SafeEncoderHandle _handle;

		/// <summary>
		/// Initializes a new <see cref="OpusEncoder"/> instance as 48k stereo optimized for speed.
		/// </summary>
		public OpusEncoder() : this(OpusOptimizer.Speech, OpusSampleRate._48k, OpusAudioChannels.Stereo)
		{
		}

		/// <summary>
		/// Initializes a new <see cref="OpusEncoder"/> instance as 48k stereo.
		/// </summary>
		/// <param name="optimized">The codec usage tuning.</param>
		public OpusEncoder(OpusOptimizer optimized) : this(optimized, OpusSampleRate._48k, OpusAudioChannels.Stereo)
		{
		}

		/// <summary>
		/// Initializes a new <see cref="OpusEncoder"/> instance optimized for speed.
		/// </summary>
		/// <param name="sampleRate">The sample rate in the input audio.</param>
		/// <param name="audioChannels">The channels in the input audio - mono or stereo.</param>
		public OpusEncoder(OpusSampleRate sampleRate, OpusAudioChannels audioChannels) : this(OpusOptimizer.Speech, sampleRate, audioChannels)
		{
		}

		/// <summary>
		/// Initializes a new <see cref="OpusEncoder"/> instance, with the specified codec usage tuning, sample rate and channels.
		/// </summary>
		/// <param name="sampleRate">The sample rate in the input audio.</param>
		/// <param name="audioChannels">The channels in the input audio - mono or stereo.</param>
		public OpusEncoder(OpusOptimizer optimized, int sampleRate, int audioChannels) : this(optimized, (OpusSampleRate)sampleRate, (OpusAudioChannels)audioChannels)
		{
		}

		/// <summary>
		/// Initializes a new <see cref="OpusEncoder"/> instance, with the specified codec usage tuning, sample rate and channels.
		/// </summary>
		/// <param name="optimized">The codec usage tuning.</param>
		/// <param name="sampleRate">The sample rate in the input audio.</param>
		/// <param name="audioChannels">The channels in the input audio - mono or stereo.</param>
		public OpusEncoder(OpusOptimizer optimized, OpusSampleRate sampleRate, OpusAudioChannels audioChannels)
		{
			if (!Enum.IsDefined(typeof(OpusOptimizer), optimized)) {
				throw new ArgumentException("Value is not defined in the enumeration.", nameof(optimized));
			}

			if (!Enum.IsDefined(typeof(OpusSampleRate), sampleRate)) {
				throw new ArgumentException("Value is not defined in the enumeration.", nameof(sampleRate));
			}

			if (!Enum.IsDefined(typeof(OpusAudioCHannels), audioChannels)) {
				throw new ArgumentException("Value is not defined in the enumeration.", nameof(audioChannels));
			}

			_handle = FFI.opus_encoder_create((int)sampleRate, (int)audioChannels, (int)optimized, out int error);

			ThrowIfError(error);

			Optimized = optimized;
			SampleRate = sampleRate;
			AudioChannels = audioChannels;
			Bitrate = -1;
		}

		/// <summary>
		/// Gets the codec usage tuning.
		/// </summary>
		public OpusOptimizer Optimized { get; }

		/// <summary>
		/// Gets the sample rate, 48k, 24k, 16k, 12k or 8k.
		/// </summary>
		public OpusSampeRate SampleRate { get; }

		/// <summary>
		/// Gets the audio channels, mono or stereo.
		/// </summary>
		public OpusAudioChannels AudioChannels { get; }

		/// <summary>
		/// Gets or sets whether Variable Bitrate is enabled.
		/// </summary>
		public bool VariableBitrate
		{
			get {
				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.GetVariableBitrate, out int value);

				ThrowIfError(result);

				return value == 1;
			}
			set {
				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.SetVariableBitrate, value ? 1 : 0);

				ThrowIfError(result);
			}
		}

		/// <summary>
		/// Gets or sets Compression Bitrate.
		/// </summary>
		public int Bitrate
		{
			get {
				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.GetBitrate, out int value);

				ThrowIfError(result);

				return (OpusBitrate)value;
			}
			set {
				if (!Enum.IsDefined(typeof(OpusBitrate), value)) {
					throw new ArgumentException("Value is not defined in the enumeration.", nameof(value));
				}

				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.SetBitrate, (int)value);

				ThrowIfError(result);
			}
		}

		/// <summary>
		/// Gets or sets Compression Quality.
		/// </summary>
		public OpusQuality Quality
		{
			get {
				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.GetQuality, out int value);

				ThrowIfError(result);

				return (OpusQuality)value;
			}
			set {
				if (!Enum.IsDefined(typeof(OpusQuality), value)) {
					throw new ArgumentException("Value is not defined in the enumeration.", nameof(value));
				}

				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.SetQuality, (int)value);

				ThrowIfError(result);
			}
		}

		/// <summary>
		/// Gets or sets the computational complexity, 0 - 10. Decreasing this will decrease CPU time, at the expense of quality.
		/// </summary>
		public int Complexity
		{
			get {
				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.GetComplexity, out int value);

				ThrowIfError(result);

				return value;
			}
			set {
				if (value < 0 || value > 10) {
					throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 10.");
				}

				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.SetComplexity, value);

				ThrowIfError(result);
			}
		}

		/// <summary>
		/// Gets or sets whether to use Forward Error Correction. You need to adjust <see cref="ExpectedPacketLoss"/>
		/// before FEC takes effect.
		/// </summary>
		public bool ForwardErrorCorrection
		{
			get {
				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.GetInbandFEC, out int value);

				ThrowIfError(result);

				return value == 1;
			}
			set {
				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.SetInbandFEC, value ? 1 : 0);

				ThrowIfError(result);
			}
		}

		/// <summary>
		/// Gets or sets the expected packet loss percentage when using ForwardErrorCorrection. Increasing this will
		/// improve quality under loss, at the expense of quality in the absence of packet loss.
		/// </summary>
		public int ExpectedPacketLoss
		{
			get {
				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.GetPacketLossPerc, out int value);

				ThrowIfError(result);

				return value;
			}
			set {
				if (value < 0 || value > 100) {
					throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 100.");
				}

				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.SetPacketLossPerc, value);

				ThrowIfError(result);
			}
		}

		/// <summary>
		/// Gets or sets whether to use DTX (discontinuous transmission). When enabled the encoder will produce
		/// packets with a length of 2 bytes or less during periods of no voice activity.
		/// </summary>
		public bool DTX
		{
			get {
				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.GetDTX, out int value);

				ThrowIfError(result);

				return value == 1;
			}
			set {
				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.SetDTX, value ? 1 : 0);

				ThrowIfError(result);
			}
		}

		/// <summary>
		/// Gets or sets the forced mono/stereo mode.
		/// </summary>
		public OpusAudioChannels AudioChannels
		{
			get {
				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.GetAudioChannels, out int value);

				ThrowIfError(result);

				return (OpusAudioChannels)value;
			}
			set {
				if (!Enum.IsDefined(typeof(OpusAudioChannels), value)) {
					throw new ArgumentException("Value is not defined in the enumeration.", nameof(value));
				}

				ThrowIfDisposed();

				int result = FFI.opus_encoder_ctl(_handle, (int)OpusControl.SetAudioChannels, (int)value);

				ThrowIfError(result);
			}
		}

		/// <summary>
		/// Encodes an Opus frame, the frame size must be one of the following: 2.5, 5, 10, 20, 40 or 60 ms.
		/// </summary>
		/// <param name="pcmBytes">The Opus frame.</param>
		/// <param name="pcmLength">The maximum number of bytes to read from <paramref name="pcmBytes"/>.</param>
		/// <param name="opusBytes">The buffer that the encoded audio will be stored in.</param>
		/// <param name="opusLength">The maximum number of bytes to write to <paramref name="opusBytes"/>.
		/// This will determine the bitrate in the encoded audio.</param>
		/// <returns>The number of bytes written to <paramref name="opusBytes"/>.</returns>
		public unsafe int Encode(byte[] pcmBytes, int pcmLength, byte[] opusBytes, int opusLength)
		{
			if (pcmBytes == null) {
				throw new ArgumentNullException(nameof(pcmBytes));
			}

			if (pcmLength < 0) {
				throw new ArgumentOutOfRangeException(nameof(pcmLength), "Value cannot be negative.");
			}

			if (pcmBytes.Length < pcmLength) {
				throw new ArgumentOutOfRangeException(nameof(pcmLength), $"Value cannot be greater than the length of {nameof(pcmBytes)}.");
			}

			if (opusBytes == null) {
				throw new ArgumentNullException(nameof(opusBytes));
			}

			if (opusLength < 0) {
				throw new ArgumentOutOfRangeException(nameof(opusLength), "Value cannot be negative.");
			}

			if (opusBytes.Length < opusLength) {
				throw new ArgumentOutOfRangeException(nameof(opusLength), $"Value cannot be greater than the length of {nameof(opusBytes)}.");
			}

			double frameSize = GetFrameSize(pcmLength);


			switch (frameSize) {
				case 2.5:
				case 5:
				case 10:
				case 20:
				case 40:
				case 60:
					break;
				default:
					throw new ArgumentException("The frame size must be one of the following: 2.5, 5, 10, 20, 40 or 60.", nameof(pcmLength));
			}

			ThrowIfDisposed();

			int result;
			int samples = GetSampleCount(frameSize);

			fixed (byte* input = pcmBytes)
			fixed (byte* output = opusBytes)
			{
				var inputPtr = (IntPtr)input;
				var outputPtr = (IntPtr)output;

				result = FFI.opus_encode(_handle, inputPtr, samples, outputPtr, opusLength);
			}

			ThrowIfError(result);

			return result;
		}
	}
}
