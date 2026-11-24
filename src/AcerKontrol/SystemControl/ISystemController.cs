namespace AcerKontrol.SystemControl;

public interface ISystemController
{
    void SetPerformanceProfile(PerformanceProfile profile);
    void SetCoolBoost(bool enabled);
    void SetKeyboardTimeout(bool enabled);
    void SetLcdOverdrive(bool enabled);
    void SetUsbCharging(bool enabled);
    void SetWindowsPowerPlan(WindowsPowerPlan plan);
    int? GetDisplayBrightness();
    void SetDisplayBrightness(int percent);
}
