namespace OpusConductor
{
	internal enum OpusControl
	{
		SetBitrate = 4002,
		GetBitrate = 4003,

		SetQuality = 4004,
		GetQuality = 4005,

		SetVariableBitrate = 4006,
		GetVariableBitrate = 4007,

		SetComplexity = 4010,
		GetComplexity = 4011,

		SetInbandFEC = 4012,
		GetInbandFEC = 4013,

		SetPacketLossPerc = 4014,
		GetPacketLossPerc = 4015,

		SetDTX = 4016,
		GetDTX = 4017,

		SetAudioChannels = 4022,
		GetAudioChannels = 4023
	}
}
