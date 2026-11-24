namespace AcerKontrol.Fan;

public interface IFanController
{
    void SetMode(FanMode mode);
    void SetCustomSpeeds(int cpuPercent, int gpuPercent);
}
