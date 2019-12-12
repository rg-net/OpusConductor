using System;

namespace OpusConductor
{
    /// <summary>
    /// Provides audio encoding with Opus.
    /// </summary>
    public class OpusEncoder : IDisposable
    {
        private readonly SafeEncoderHandle _handle;

        private int _bitrate;

        /// <summary>
        /// Initializes a new <see cref="OpusEncoder"/> instance as 48k stereo.
        /// </summary>
        /// <param name="optimized">The codec usage tuning.</param>
        public OpusEncoder(OpusOptimizer optimized) : this(optimized, 48000, 2)
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
            if (!Enum.IsDefined(typeof(OpusOptimizer), optimized))
            {
                throw new ArgumentException("Value is not defined in the enumeration.", nameof(optimized));
            }

            if (!Enum.IsDefined(typeof(OpusSampleRate), sampleRate))
	    {
                throw new ArgumentException("Value is not defined in the enumeration.", nameof(sampleRate));
            }

            if (!Enum.IsDefined(typeof(OpusAudioCHannels), audioChannels))
	    {
                throw new ArgumentException("Value is not defined in the enumeration.", nameof(audioChannels));
            }

            OpusOptimizer = optimized;
            SampleRate = sampleRate;
            AudioChannels = audioChannels;
            Bitrate = 128000;

            _handle = API.opus_encoder_create((int)sampleRate, (int)audioChannels, (int)optimized, out int error);
            API.ThrowIfError(error);

            // Setting to -1 (OPUS_BITRATE_MAX) enables bitrate to be regulated by the output buffer length.
            int result = API.opus_encoder_ctl(_handle, (int)OpusControl.SetBitrate, -1);
            API.ThrowIfError(result);
        }

        /// <summary>
        /// Gets the codec usage tuning.
        /// </summary>
        public OpusOptimizer OpusOptimizer { get; }

        /// <summary>
        /// Gets the sample rate, 48000, 24000, 16000, 12000 or 8000 Hz.
        /// </summary>
        public OpusSampeRate SampleRate { get; }

        /// <summary>
        /// Gets the audio channels, mono or stereo.
        /// </summary>
        public OpusAudioChannels AudioChannels { get; }

        /// <summary>
        /// Gets or sets the bitrate, 8000 - 512000 bps.
        /// </summary>
        [Obsolete("This property was used for the old encode method and is deprecated, please use the new encode method instead.")]
        public int Bitrate
        {
            get => _bitrate;
            set
            {
                if (value < 8000 || value > 512000)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 8000 and 512000.");
                }

                _bitrate = value;
            }
        }

        /// <summary>
        /// Gets or sets whether VBR (variable bitrate) is enabled.
        /// </summary>
        public bool VBR
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.GetVBR, out int value);
                API.ThrowIfError(result);

                return value == 1;
            }
            set
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.SetVBR, value ? 1 : 0);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets Opus line clarity.
        /// </summary>
        public OpusClarity Clarity
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.GetClarity, out int value);
                API.ThrowIfError(result);

                return (OpusClarity)value;
            }
            set
            {
                if (!Enum.IsDefined(typeof(OpusClarity), value))
                {
                    throw new ArgumentException("Value is not defined in the enumeration.", nameof(value));
                }

                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.SetClarity, (int)value);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets the computational complexity, 0 - 10. Decreasing this will decrease CPU time, at the expense of quality.
        /// </summary>
        public int Complexity
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.GetComplexity, out int value);
                API.ThrowIfError(result);

                return value;
            }
            set
            {
                if (value < 0 || value > 10)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 10.");
                }

                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.SetComplexity, value);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets whether to use FEC (forward error correction). You need to adjust <see cref="ExpectedPacketLoss"/>
        /// before FEC takes effect.
        /// </summary>
        public bool FEC
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.GetInbandFEC, out int value);
                API.ThrowIfError(result);

                return value == 1;
            }
            set
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.SetInbandFEC, value ? 1 : 0);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets the expected packet loss percentage when using FEC (forward error correction). Increasing this will
        /// improve quality under loss, at the expense of quality in the absence of packet loss.
        /// </summary>
        public int ExpectedPacketLoss
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.GetPacketLossPerc, out int value);
                API.ThrowIfError(result);

                return value;
            }
            set
            {
                if (value < 0 || value > 100)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 100.");
                }

                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.SetPacketLossPerc, value);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets whether to use DTX (discontinuous transmission). When enabled the encoder will produce
        /// packets with a length of 2 bytes or less during periods of no voice activity.
        /// </summary>
        public bool DTX
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.GetDTX, out int value);
                API.ThrowIfError(result);

                return value == 1;
            }
            set
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.SetDTX, value ? 1 : 0);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Gets or sets the forced mono/stereo mode.
        /// </summary>
        public OpusAudioChannels AudioChannels
        {
            get
            {
                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.GetAudioChannels, out int value);
                API.ThrowIfError(result);

                return (OpusAudioChannels)value;
            }
            set
            {
                if (!Enum.IsDefined(typeof(OpusAudioChannels), value))
                {
                    throw new ArgumentException("Value is not defined in the enumeration.", nameof(value));
                }

                ThrowIfDisposed();
                int result = API.opus_encoder_ctl(_handle, (int)OpusControl.SetAudioChannels, (int)value);
                API.ThrowIfError(result);
            }
        }

        /// <summary>
        /// Encodes an Opus frame, the frame size must be one of the following: 2.5, 5, 10, 20, 40 or 60 ms.
        /// </summary>
        /// <param name="pcmBytes">The Opus frame.</param>
        /// <param name="length">The maximum number of bytes to use from <paramref name="pcmBytes"/>.</param>
        /// <param name="encodedLength">The length of the encoded audio.</param>
        /// <returns>A byte array containing the encoded audio.</returns>
        [Obsolete("This method is deprecated, please use the new encode method instead.")]
        public unsafe byte[] Encode(byte[] pcmBytes, int length, out int encodedLength)
        {
            if (pcmBytes == null)
            {
                throw new ArgumentNullException(nameof(pcmBytes));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Value cannot be negative.");
            }

            if (pcmBytes.Length < length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"Value cannot be greater than the length of {nameof(pcmBytes)}.");
            }

            double frameSize = API.GetFrameSize(length, SampleRate, AudioChannels);

            switch (frameSize)
            {
                case 2.5:
                case 5:
                case 10:
                case 20:
                case 40:
                case 60:
                    break;
                default:
                    throw new ArgumentException("The frame size must be one of the following: 2.5, 5, 10, 20, 40 or 60.", nameof(length));
            }

            ThrowIfDisposed();

            byte[] opusBytes = new byte[(int)(frameSize * Bitrate / 8 / 1000)];
            int result;

            int samples = API.GetSampleCount(frameSize, SampleRate);

            fixed (byte* input = pcmBytes)
            fixed (byte* output = opusBytes)
            {
                var inputPtr = (IntPtr)input;
                var outputPtr = (IntPtr)output;
                result = API.opus_encode(_handle, inputPtr, samples, outputPtr, opusBytes.Length);
            }

            API.ThrowIfError(result);

            encodedLength = result;
            return opusBytes;
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
            if (pcmBytes == null)
            {
                throw new ArgumentNullException(nameof(pcmBytes));
            }

            if (pcmLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pcmLength), "Value cannot be negative.");
            }

            if (pcmBytes.Length < pcmLength)
            {
                throw new ArgumentOutOfRangeException(nameof(pcmLength), $"Value cannot be greater than the length of {nameof(pcmBytes)}.");
            }

            if (opusBytes == null)
            {
                throw new ArgumentNullException(nameof(opusBytes));
            }

            if (opusLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(opusLength), "Value cannot be negative.");
            }

            if (opusBytes.Length < opusLength)
            {
                throw new ArgumentOutOfRangeException(nameof(opusLength), $"Value cannot be greater than the length of {nameof(opusBytes)}.");
            }

            double frameSize = API.GetFrameSize(pcmLength, SampleRate, AudioChannels);

            switch (frameSize)
            {
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
            int samples = API.GetSampleCount(frameSize, SampleRate);

            fixed (byte* input = pcmBytes)
            fixed (byte* output = opusBytes)
            {
                var inputPtr = (IntPtr)input;
                var outputPtr = (IntPtr)output;
                result = API.opus_encode(_handle, inputPtr, samples, outputPtr, opusLength);
            }

            API.ThrowIfError(result);
            return result;
        }

        /// <summary>
        /// Releases all resources used by the current instance.
        /// </summary>
        public void Dispose()
        {
            _handle?.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_handle.IsClosed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
