using AcerKontrol.ChargeLimit;

namespace AcerKontrol.Tests;

public class ChargeLimitCodecTests
{
    [Fact] public void Decode_On() => Assert.Equal(ChargeLimitState.On, ChargeLimitCodec.Decode(1, [0, 0], [1, 0, 0, 0, 0]));
    [Fact] public void Decode_Off() => Assert.Equal(ChargeLimitState.Off, ChargeLimitCodec.Decode(1, [0, 0], [0, 0, 0, 0, 0]));
    [Fact] public void Decode_NotInFunctionList_Unsupported() => Assert.Equal(ChargeLimitState.Unsupported, ChargeLimitCodec.Decode(0, [0, 0], [1, 0, 0, 0, 0]));
    [Fact] public void Decode_ErrorReturn_Unsupported() => Assert.Equal(ChargeLimitState.Unsupported, ChargeLimitCodec.Decode(1, [1, 0], [1, 0, 0, 0, 0]));
    [Fact] public void Decode_EmptyStatus_Unsupported() => Assert.Equal(ChargeLimitState.Unsupported, ChargeLimitCodec.Decode(1, [0, 0], []));
    [Fact] public void IsWriteSuccess_Zero() => Assert.True(ChargeLimitCodec.IsWriteSuccess(0));
    [Fact] public void IsWriteSuccess_NonZero() => Assert.False(ChargeLimitCodec.IsWriteSuccess(1));
}
