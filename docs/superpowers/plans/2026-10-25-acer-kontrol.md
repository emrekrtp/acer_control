# AcerKontrol Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Acer Nitro 5 AN515-58 için %80 pil sınırını hatırlayan, sürekli yeniden uygulayan ve pil durumunu gösteren Türkçe sistem tepsisi uygulaması.

**Architecture:** Saf mantık (ayarlar, durum kodlama, bekçi, simge stili) arayüzler arkasında ve birim testli; WMI erişimi iki ince sınıfta (`AcerWmiChargeLimitDevice`, `WmiBatteryInfoSource`) ve elle doğrulanır. `TrayAppContext` hepsini bir `NotifyIcon`, 60 sn zamanlayıcı ve güç olaylarıyla birbirine bağlar.

**Tech Stack:** C# / .NET 9 (`net9.0-windows`, WinForms), `System.Management` NuGet, xUnit, PowerShell (kurulum).

**Spec:** `docs/superpowers/specs/2026-10-25-acer-kontrol-design.md`

## Global Constraints

- Hedef çatı: `net9.0-windows`, `<UseWindowsForms>true</UseWindowsForms>`, nullable açık.
- Uygulama manifesti `requireAdministrator`.
- Kullanıcıya görünen tüm metinler Türkçe ve yalnızca `Strings.cs` içinde.
- WMI: `root\wmi`, sınıf `BatteryControl`; pil no `1`, sağlık modu fonksiyonu `1`.
- Sınır sabit %80; başka eşik seçeneği yok.
- Varsayılan ayar: sınır açık.
- Bekçi aralığı 60 sn; döngü başına 3 deneme, aralarında 2 sn.
- Ayarlar: `%APPDATA%\AcerKontrol\settings.json`.
- Kurulum: `%LOCALAPPDATA%\Programs\AcerKontrol`, zamanlanmış görev `AcerKontrol` (oturum açılışında, en yüksek yetki).
- Fan, mod, klavye ışığı, kalibrasyon yok.

## Review Focus

1. **WMI arayüzü istisna atıyor veya "desteklenmiyor" diyor** (Care Center'da seçeneğin görünmediği durum) → çökme yok, 3 deneme, hâlâ olmazsa tek bildirim, simge "?" gösterir. Test: Task 3 `Enforce_DeviceThrowsEveryTime_ReturnsFailedAfterThreeAttempts`, `Enforce_Unsupported_ReturnsFailedWithoutSetting`.
2. **Ayar dosyası yok, boş veya bozuk JSON** → varsayılan (sınır açık), dosya üzerine düzgünce yazılır. Test: Task 1 `Load_CorruptJson_ReturnsDefaults`, `Load_MissingFile_ReturnsDefaults`.
3. **ACCSvc ayarı her dakika geri alıyor** → her döngüde sessizce yeniden uygulanır, bildirim gelmez. Test: Task 3 `Enforce_RevertedEveryCycle_ReappliesWithoutNotification`.
4. **Pil bilgisi eksik veya tasarım kapasitesi 0** → sağlık "—", sıfıra bölme yok. Test: Task 2 `HealthPercent_DesignCapacityZero_ReturnsNull`.
5. **İkinci örnek başlatılıyor** (zamanlanmış görev + elle açma) → ikincisi sessizce kapanır. Elle doğrulama: Task 5 Step 6.

---

### Task 1: Çözüm iskeleti, git ve ayar deposu

**Files:**
- Create: `AcerKontrol.sln`, `src/AcerKontrol/AcerKontrol.csproj`, `src/AcerKontrol/app.manifest`, `src/AcerKontrol/Strings.cs`
- Create: `src/AcerKontrol/Settings/AppSettings.cs`, `src/AcerKontrol/Settings/SettingsStore.cs`
- Create: `tests/AcerKontrol.Tests/AcerKontrol.Tests.csproj`, `tests/AcerKontrol.Tests/SettingsStoreTests.cs`
- Create: `.gitignore` (`bin/`, `obj/`, `publish/`)

**Interfaces:**
- Produces: `public sealed record AppSettings(bool ChargeLimitEnabled = true);`
- Produces: `public sealed class SettingsStore(string filePath)` with `AppSettings Load()` and `void Save(AppSettings settings)`; `public static string DefaultPath` = `%APPDATA%\AcerKontrol\settings.json`.
- Produces: `public static class Strings` (Türkçe sabitler; sonraki görevler buraya ekler).

- [ ] **Step 1: Git ve iskelet**

Run: `git init`, `dotnet new sln -n AcerKontrol`, `dotnet new winforms -n AcerKontrol -o src/AcerKontrol -f net9.0`, `dotnet new xunit -n AcerKontrol.Tests -o tests/AcerKontrol.Tests -f net9.0`, add both projects to the sln, add a project reference from tests to app, set the test project's TFM to `net9.0-windows`, add `System.Management` to the app project. Add `app.manifest` with `<requestedExecutionLevel level="requireAdministrator" uiAccess="false" />` and point `<ApplicationManifest>` at it. Delete the template `Form1`.
Expected: `dotnet build` succeeds.

- [ ] **Step 2: Write the failing tests** in `SettingsStoreTests.cs` (each test uses a fresh temp directory):

```csharp
[Fact] public void Load_MissingFile_ReturnsDefaults()
    => Assert.True(new SettingsStore(Path.Combine(tmp, "yok.json")).Load().ChargeLimitEnabled);

[Fact] public void Load_CorruptJson_ReturnsDefaults()
{ File.WriteAllText(path, "{bozuk"); Assert.Equal(new AppSettings(), new SettingsStore(path).Load()); }

[Fact] public void SaveThenLoad_RoundTrips()
{ var s = new SettingsStore(Path.Combine(tmp, "alt", "settings.json"));   // dizin yoksa oluşturulmalı
  s.Save(new AppSettings(false)); Assert.False(s.Load().ChargeLimitEnabled); }
```

- [ ] **Step 3: Run** `dotnet test` → Expected: FAIL (tipler yok).

- [ ] **Step 4: Implement** `AppSettings` and `SettingsStore` with `System.Text.Json`. `Load` catches `IOException`, `JsonException` and returns `new AppSettings()`. `Save` creates the directory and writes to `path + ".tmp"`, then `File.Move(..., overwrite: true)`.

- [ ] **Step 5: Run** `dotnet test` → Expected: 3 PASS.

- [ ] **Step 6: Commit** — `git add -A && git commit -m "feat: scaffold AcerKontrol and settings store"`

---

### Task 2: Pil durumu modeli ve okuyucu

**Files:**
- Create: `src/AcerKontrol/Battery/BatterySnapshot.cs`, `src/AcerKontrol/Battery/IBatteryInfoSource.cs`, `src/AcerKontrol/Battery/WmiBatteryInfoSource.cs`
- Test: `tests/AcerKontrol.Tests/BatterySnapshotTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public sealed record BatterySnapshot(int? ChargePercent, bool? IsCharging, bool? OnAcPower,
      uint? FullChargedCapacity, uint? DesignedCapacity, uint? CycleCount, double? TemperatureC)
  { public int? HealthPercent { get; } }   // round(100*Full/Designed); null if either null or Designed==0
  public interface IBatteryInfoSource { BatterySnapshot Read(); }
  ```
- Produces: `WmiBatteryInfoSource : IBatteryInfoSource`; `Read()` never throws (on any exception it returns the fields it got, the rest `null`).

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public void HealthPercent_Computes() => Assert.Equal(88, Snap(full: 44000, design: 50000).HealthPercent);
[Fact] public void HealthPercent_DesignCapacityZero_ReturnsNull() => Assert.Null(Snap(full: 44000, design: 0).HealthPercent);
[Fact] public void HealthPercent_MissingData_ReturnsNull() => Assert.Null(Snap(full: null, design: 50000).HealthPercent);
```

- [ ] **Step 2: Run** `dotnet test --filter BatterySnapshotTests` → Expected: FAIL.

- [ ] **Step 3: Implement** `BatterySnapshot` and `WmiBatteryInfoSource`. Sources: `root\cimv2 Win32_Battery.EstimatedChargeRemaining`; `root\wmi BatteryStatus` (`Charging`, `PowerOnline`); `BatteryFullChargedCapacity.FullChargedCapacity`; `BatteryStaticData.DesignedCapacity`; `BatteryCycleCount.CycleCount`; `BatteryTemperature.Temperature` (tenths of Kelvin → `t/10.0 - 273.15`; a value of 0 means `null`). Wrap each query in its own try/catch.

- [ ] **Step 4: Run** `dotnet test` → Expected: all PASS.

- [ ] **Step 5: Manual check (elevated)** — a temporary `--dump-battery` argument in `Program.cs` prints the snapshot to the console; run it from an admin terminal. Expected: the percentage matches Windows' battery indicator and health is between 1 and 100. Remove the argument afterwards.

- [ ] **Step 6: Commit** — `git commit -am "feat: battery snapshot via WMI"` (add new files first)

---

### Task 3: Pil sınırı cihazı ve bekçi

**Files:**
- Create: `src/AcerKontrol/ChargeLimit/IChargeLimitDevice.cs`, `ChargeLimitCodec.cs`, `AcerWmiChargeLimitDevice.cs`, `ChargeLimitGuard.cs` (all under `src/AcerKontrol/ChargeLimit/`)
- Test: `tests/AcerKontrol.Tests/ChargeLimitCodecTests.cs`, `tests/AcerKontrol.Tests/ChargeLimitGuardTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public enum ChargeLimitState { Off, On, Unsupported }
  public interface IChargeLimitDevice { ChargeLimitState Read(); void Write(bool enabled); }   // both may throw
  public static class ChargeLimitCodec {
      public static ChargeLimitState Decode(byte functionList, byte[] ret, byte[] status);
      public static bool IsWriteSuccess(ushort ret); }
  public enum GuardOutcome { InSync, Reapplied, Failed }
  public sealed record GuardResult(GuardOutcome Outcome, ChargeLimitState? Actual, bool ShouldNotify, string? Error);
  public sealed class ChargeLimitGuard(IChargeLimitDevice device, Action<TimeSpan> sleep, int attempts = 3, TimeSpan? delay = null /* 2 s */)
  { public GuardResult Enforce(bool desired); }
  ```
- `Decode` rules: `(functionList & 1) == 0` or any `ret` byte ≠ 0 → `Unsupported`; otherwise `status[0] == 1 ? On : Off`. `IsWriteSuccess(ret) => ret == 0`.

- [ ] **Step 1: Write the failing codec tests**

```csharp
[Fact] public void Decode_On()  => Assert.Equal(ChargeLimitState.On,  ChargeLimitCodec.Decode(1, [0,0], [1,0,0,0,0]));
[Fact] public void Decode_Off() => Assert.Equal(ChargeLimitState.Off, ChargeLimitCodec.Decode(1, [0,0], [0,0,0,0,0]));
[Fact] public void Decode_NotInFunctionList_Unsupported() => Assert.Equal(ChargeLimitState.Unsupported, ChargeLimitCodec.Decode(0, [0,0], [1,0,0,0,0]));
[Fact] public void Decode_ErrorReturn_Unsupported() => Assert.Equal(ChargeLimitState.Unsupported, ChargeLimitCodec.Decode(1, [1,0], [1,0,0,0,0]));
```

- [ ] **Step 2: Write the failing guard tests** with a `FakeDevice` (a scripted queue of `Read` results or exceptions; records `Write` calls) and a `sleep` that counts calls:

```csharp
Enforce_AlreadyMatches_InSyncNoWrite            // Read→On, desired true ⇒ InSync, Writes empty, ShouldNotify false
Enforce_Mismatch_WritesAndVerifies              // Read→Off, then Read→On ⇒ Reapplied, Writes == [true]
Enforce_DeviceThrowsEveryTime_ReturnsFailedAfterThreeAttempts  // ⇒ Failed, ShouldNotify true, sleep called 2 times
Enforce_Unsupported_ReturnsFailedWithoutSetting // Read→Unsupported ×3 ⇒ Failed, Writes empty
Enforce_TransientThenOk_Reapplied               // throws once, then Off, then On ⇒ Reapplied, ShouldNotify false
Enforce_SecondConsecutiveFailure_DoesNotNotifyAgain   // two Enforce calls that both fail ⇒ 1st ShouldNotify true, 2nd false
Enforce_FailureThenSuccessThenFailure_NotifiesAgain   // ⇒ notifies on 1st and 3rd
Enforce_RevertedEveryCycle_ReappliesWithoutNotification  // 3 cycles of Off→(write)→On ⇒ all Reapplied, ShouldNotify false
```

- [ ] **Step 3: Run** `dotnet test` → Expected: FAIL.

- [ ] **Step 4: Implement** `ChargeLimitCodec` and `ChargeLimitGuard`. One attempt = Read; if it equals desired → done; if `Unsupported` → attempt fails; otherwise Write(desired), then Read again and compare. Wait `delay` between attempts (not after the last). The guard keeps a private `_inFailureStreak`; `ShouldNotify = outcome == Failed && !wasInStreak`.

- [ ] **Step 5: Run** `dotnet test` → Expected: all PASS.

- [ ] **Step 6: Implement** `AcerWmiChargeLimitDevice` with `System.Management`: take the first instance of `BatteryControl` from `root\wmi`. `Read` → `GetBatteryHealthControlStatus(uBatteryNo=1, uFunctionQuery=1, uReserved=new byte[2])` → `ChargeLimitCodec.Decode`. `Write` → `SetBatteryHealthControl(uBatteryNo=1, uFunctionMask=1, uFunctionStatus=enabled?1:0, uReservedIn=new byte[5])`; if `!IsWriteSuccess(uReturn)`, throw `InvalidOperationException($"uReturn={ret}")`.

- [ ] **Step 7: Hardware check (elevated, user approval required — this is the first write to the hardware)** — add temporary `--limit on|off|read` arguments to `Program.cs` and run them from an admin terminal: `read`, `on`, `read`, `off`, `read`, then `on` again.
Expected: the reads return `On`/`Off` matching the writes; Acer Care Center shows the same state. **If the observed bytes contradict the assumptions in the spec** (for example `status[0]` doesn't change), stop, write down the bytes you saw, and update `ChargeLimitCodec` and its tests accordingly. Remove the arguments afterwards.

- [ ] **Step 8: Commit** — `git add -A && git commit -m "feat: charge limit device and guard"`

---

### Task 4: Tepsi simgesi çizimi

**Files:**
- Create: `src/AcerKontrol/Tray/TrayIconStyle.cs`, `src/AcerKontrol/Tray/TrayIconRenderer.cs`
- Test: `tests/AcerKontrol.Tests/TrayIconStyleTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public sealed record TrayIconStyle(string Text, Color Foreground)
  { public static TrayIconStyle For(int? percent, ChargeLimitState? limit); }
  public static class TrayIconRenderer { public static Icon Render(TrayIconStyle style, int size = 32); }  // caller disposes
  ```
- Rules for `For`: Text = `percent` as string (`100` stays `"100"`), `"?"` if `percent` is null; Foreground = `Color.LimeGreen` if `limit == On`, `Color.Gray` if `Off`, `Color.OrangeRed` if `Unsupported` or null.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public void For_LimitOn_Green() { var s = TrayIconStyle.For(76, ChargeLimitState.On); Assert.Equal("76", s.Text); Assert.Equal(Color.LimeGreen, s.Foreground); }
[Fact] public void For_LimitOff_Gray() => Assert.Equal(Color.Gray, TrayIconStyle.For(50, ChargeLimitState.Off).Foreground);
[Fact] public void For_Unknown_OrangeQuestion() { var s = TrayIconStyle.For(null, null); Assert.Equal("?", s.Text); Assert.Equal(Color.OrangeRed, s.Foreground); }
[Fact] public void Render_ReturnsIconOfRequestedSize() { using var i = TrayIconRenderer.Render(TrayIconStyle.For(100, ChargeLimitState.On)); Assert.Equal(32, i.Width); }
```

- [ ] **Step 2: Run** → Expected: FAIL.
- [ ] **Step 3: Implement.** Draw on a transparent `Bitmap` with bold Segoe UI; shrink the font so `"100"` fits. `Icon.FromHandle(bmp.GetHicon())`, then clone it and release the handle with `DestroyIcon` (P/Invoke) so handles don't leak on every 60 s refresh.
- [ ] **Step 4: Run** → Expected: PASS.
- [ ] **Step 5: Commit** — `git add -A && git commit -m "feat: tray icon renderer"`

---

### Task 5: Tepsi uygulaması, durum penceresi ve bağlantılar

**Files:**
- Create: `src/AcerKontrol/Tray/TrayAppContext.cs`, `src/AcerKontrol/Tray/StatusForm.cs`
- Modify: `src/AcerKontrol/Program.cs`, `src/AcerKontrol/Strings.cs`

**Interfaces:**
- Consumes: `SettingsStore`, `IBatteryInfoSource`, `IChargeLimitDevice`, `ChargeLimitGuard.Enforce`, `TrayIconStyle.For`, `TrayIconRenderer.Render`.
- Produces: `public sealed class TrayAppContext : ApplicationContext` (ctor takes the interfaces above plus `SettingsStore`).

- [ ] **Step 1: Strings** — add to `Strings.cs`: `AppName = "Acer Kontrol"`, `MenuLimit = "Pil sınırı (%80)"`, `MenuStatus = "Durumu göster"`, `MenuExit = "Çıkış"`, `LimitOn = "Açık"`, `LimitOff = "Kapalı"`, `LimitUnknown = "Bilinmiyor"`, `Unknown = "—"`, `Charging = "Şarj oluyor"`, `NotCharging = "Şarj olmuyor"`, `NotifyFailTitle = "Pil sınırı uygulanamadı"`, `NotifyFailBody = "Acer arayüzü yanıt vermedi. Bir dakika sonra tekrar denenecek."`, plus `StatusForm` labels: `"Pil"`, `"Durum"`, `"Sağlık"`, `"Döngü sayısı"`, `"Sıcaklık"`, `"Pil sınırı"`.

- [ ] **Step 2: Program.cs** — create a `Mutex` named `Global\AcerKontrol`; if it is already held, return immediately. Otherwise: `ApplicationConfiguration.Initialize()` and `Application.Run(new TrayAppContext(new SettingsStore(SettingsStore.DefaultPath), new WmiBatteryInfoSource(), new AcerWmiChargeLimitDevice()))`.

- [ ] **Step 3: TrayAppContext** —
  - `NotifyIcon` with a context menu: checkable `MenuLimit`, `MenuStatus`, separator, `MenuExit`. Left click opens/focuses `StatusForm`.
  - `Tick()`: `guard.Enforce(settings.ChargeLimitEnabled)` + `battery.Read()` on a background thread (`Task.Run`), then on the UI thread: update the icon (dispose the old one), tooltip `"%{p} · Sınır: {Açık|Kapalı|Bilinmiyor}"`, the menu checkmark and the open `StatusForm`; if `ShouldNotify`, call `ShowBalloonTip(NotifyFailTitle, NotifyFailBody)`. Re-entrancy guard: skip if a Tick is already running.
  - Triggers: one `Tick` at startup, a `System.Windows.Forms.Timer` every 60 s, and `SystemEvents.PowerModeChanged` (`Resume` and `StatusChange`). Unsubscribe from `SystemEvents` on exit.
  - Menu toggle: `settings = settings with { ChargeLimitEnabled = !… }`, `store.Save`, `Tick()`.

- [ ] **Step 4: StatusForm** — small fixed-size window (`FormBorderStyle.FixedToolWindow`, positioned bottom-right) with a two-column label table (the labels from Step 1, values from `BatterySnapshot` and the actual state; null → `Strings.Unknown`) and a `CheckBox` "Pil sınırı (%80)" that triggers the same toggle as the menu. `UpdateView(BatterySnapshot, ChargeLimitState?, bool desired)`. Closing the window only hides it.

- [ ] **Step 5: Run** `dotnet build` and `dotnet test` → Expected: build succeeds, all tests PASS.

- [ ] **Step 6: Manual check (admin terminal, `dotnet run --project src/AcerKontrol`)** —
  1. The tray icon shows the percentage and is green.
  2. Turning the limit off from the menu → icon turns gray within ~2 s, Care Center shows "off".
  3. Turning the limit back on from Care Center → within 60 s the app puts it back to the stored setting (off), with no notification.
  4. Launching a second time → no second icon appears.
  5. Sleep/wake → the limit stays correct after waking.
  6. Exit removes the icon.

- [ ] **Step 7: Commit** — `git add -A && git commit -m "feat: tray app with watchdog and status window"`

---

### Task 6: Kurulum ve otomatik başlama

**Files:**
- Create: `scripts/install.ps1`, `scripts/uninstall.ps1`, `README.md`

**Interfaces:**
- Consumes: the `src/AcerKontrol` project.

- [ ] **Step 1: install.ps1** — if not running as admin, restart itself with `-Verb RunAs`. Steps: `dotnet publish src/AcerKontrol -c Release -r win-x64 --self-contained false -o "$env:LOCALAPPDATA\Programs\AcerKontrol"`; stop any running `AcerKontrol` process before copying; register a task named `AcerKontrol` (or update it if it exists) with `Register-ScheduledTask`: trigger `-AtLogOn -User $env:USERNAME`, principal `-RunLevel Highest -LogonType Interactive`, settings `-AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit 0`; then `Start-ScheduledTask AcerKontrol`.

- [ ] **Step 2: uninstall.ps1** — elevate itself the same way; stop the process, `Unregister-ScheduledTask AcerKontrol -Confirm:$false`, delete the install folder. Leave the settings file alone.

- [ ] **Step 3: README.md** — 10–15 lines in Turkish: what it does, the %80 constraint, install/uninstall commands, settings file location, a note that Care Center can stay installed.

- [ ] **Step 4: Manual check** — run `scripts/install.ps1` → the icon appears without a UAC prompt; after signing out and back in, the icon is there again without UAC; `Get-ScheduledTask AcerKontrol` shows `State=Running`/`Ready`; `scripts/uninstall.ps1` removes the task and the folder.

- [ ] **Step 5: Commit** — `git add -A && git commit -m "feat: install/uninstall with logon autostart"`
