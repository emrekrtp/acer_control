using AcerKontrol.Battery;

namespace AcerKontrol.Tests;

public class PowercfgReportTests
{
    private const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <BatteryReport xmlns="http://schemas.microsoft.com/battery/2012">
          <Batteries><Battery><Id>AP18E8M</Id><DesignCapacity>57488</DesignCapacity><FullChargeCapacity>46108</FullChargeCapacity><CycleCount>0</CycleCount></Battery></Batteries>
        </BatteryReport>
        """;

    [Fact] public void ParseDesignCapacity_ReadsValue() => Assert.Equal(57488u, PowercfgReport.ParseDesignCapacity(Xml));
    [Fact] public void ParseDesignCapacity_Garbage_ReturnsNull() => Assert.Null(PowercfgReport.ParseDesignCapacity("<bozuk"));
    [Fact] public void ParseDesignCapacity_Zero_ReturnsNull()
        => Assert.Null(PowercfgReport.ParseDesignCapacity(Xml.Replace("57488", "0")));
}
