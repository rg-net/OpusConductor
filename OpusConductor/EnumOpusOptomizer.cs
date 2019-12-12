namespace OpusConductor
{
    /// <summary>
    /// Specifies the intended applications.
    /// </summary>
    public enum OpusOptimizer
    {
        /// <summary>
        /// Process signal for improved speech intelligibility.
        /// </summary>
        Speech = 2048,
        /// <summary>
        /// Favor faithfulness to the original input.
        /// </summary>
        Clarity = 2049,
        /// <summary>
        /// Configure the minimum possible coding delay by disabling certain modes of operation.
        /// </summary>
        Speed = 2051
    }
}
