using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReNative3D36S;

public partial class MainWindow : Window
{
    private readonly string appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReNative3D36S");
    private readonly string kitDir;
    private readonly string sessionLog;
    private CancellationTokenSource? work;
    private string? keysPath;
    private PocketNativeUpdates updater = null!;
    private EnginePreferences enginePrefs = new();
    private bool settingEngine;
    private string SelectedEngine => enginePrefs.Selected;
    private string ActiveKit => SelectedEngine == "local" ? enginePrefs.LocalForkDirectory : kitDir;

    private string? latestOutput;
    private bool settingLanguage;
    private readonly Stopwatch elapsedStopwatch = new();
    private readonly DispatcherTimer elapsedTimer = new() { Interval = TimeSpan.FromSeconds(1) };


    public MainWindow()
    {
        InitializeComponent();
        elapsedTimer.Tick += (_, _) => UpdateElapsedTime();
        kitDir = Path.Combine(appDir, "kit");
        LanguageService.Initialize();
        updater = new PocketNativeUpdates(kitDir, Log);
        enginePrefs = EnginePreferences.Load();
        if (enginePrefs.Selected is not ("pocket" or "renative" or "local" or "zakuro")) enginePrefs.Selected = "pocket";
        settingEngine = true;
        foreach (var combo in new[] { EngineSelector, EngineSelectorPage })
        {
            combo.SelectedItem = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == SelectedEngine) ?? combo.Items[0];
        }
        settingEngine = false;

        settingLanguage = true;
        foreach (var combo in new[] { LanguageSelector, LanguageSelectorSettings })
        {
            foreach (var item in combo.Items.OfType<ComboBoxItem>())
                if ((string)item.Tag == LanguageService.Selection) combo.SelectedItem = item;
            if (combo.SelectedIndex < 0) combo.SelectedIndex = 0;
        }
        settingLanguage = false;
        ApplyTranslations();
        UpdateInstalledLabels();
        RefreshEngineUi();
        SetPage("recompile");
        UpdateOutputPreview();
        Loaded += async (_, _) => await CheckReleaseInBackgroundAsync();
        var logs = Path.Combine(appDir, "logs");
        Directory.CreateDirectory(logs);
        sessionLog = Path.Combine(logs, "recompilacao-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
        OutPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ReNative3D36S");
        Log("ReNative3D36S 0.2.0 started.");
        Log("Arquivo de log desta sessão: " + sessionLog);
    }

    private void SelectRom_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Title = "Selecione sua ROM", Filter = "ROM Nintendo 3DS|*.3ds;*.cci;*.cia", CheckFileExists = true };
        if (dlg.ShowDialog(this) != true) return;
        RomPath.Text = dlg.FileName;
        keysPath = null;
        RomHint.Text = LanguageService.Text("romHint");
        FolderNameBox.Text = ""; // prefer official game profile name, not dump filename
        UpdateOutputPreview();
    }

    private void SelectOut_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Escolha a pasta de destino", Multiselect = false };
        if (dlg.ShowDialog(this) == true)
        {
            OutPath.Text = dlg.FolderName;
            UpdateOutputPreview();
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var path = ReferenceEquals(sender, QuickOutputBtn) ? OutPath.Text
            : latestOutput is not null && Directory.Exists(latestOutput) ? latestOutput : OutPath.Text;
        if (!Directory.Exists(path)) { MessageBox.Show("Ainda não existe uma recompilação nessa pasta."); return; }
        OpenDirectory(path);
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e) => OpenDirectory(Path.GetDirectoryName(sessionLog)!);
    private void About_Click(object sender, RoutedEventArgs e) => new AboutWindow { Owner = this }.ShowDialog();
    private void Cancel_Click(object sender, RoutedEventArgs e) => work?.Cancel();
    private async void Prepare_Click(object sender, RoutedEventArgs e) => await ExecuteAsync(PrepareOnlyAsync);
    private async void Build_Click(object sender, RoutedEventArgs e) => await ExecuteAsync(BuildGameAsync);

    private static void OpenDirectory(string path)
    {
        var start = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
        start.ArgumentList.Add(path);
        Process.Start(start);
    }

    private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ApplyLanguageChoice(LanguageSelector);

    private void LanguageSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ApplyLanguageChoice(LanguageSelectorSettings);

    private void ApplyLanguageChoice(ComboBox source)
    {
        if (settingLanguage || source.SelectedItem is not ComboBoxItem item) return;
        settingLanguage = true;
        var selected = (string)item.Tag;
        LanguageService.Set(selected);
        foreach (var combo in new[] { LanguageSelector, LanguageSelectorSettings })
        {
            var target = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(x => (string)x.Tag == selected);
            if (target is not null && !ReferenceEquals(combo.SelectedItem,target)) combo.SelectedItem = target;
        }
        settingLanguage = false;
        ApplyTranslations();
    }

    private void NavRecompile_Click(object sender, RoutedEventArgs e) => SetPage("recompile");
    private void NavUpdates_Click(object sender, RoutedEventArgs e) => SetPage("updates");
    private void NavLibrary_Click(object sender, RoutedEventArgs e)
    {
        RefreshLibrary();
        SetPage("library");
    }
    private void NavSettings_Click(object sender, RoutedEventArgs e) => SetPage("settings");
    private void NavEngines_Click(object sender, RoutedEventArgs e) => SetPage("engines");

    private void Engine_SelectionChanged(object sender, SelectionChangedEventArgs e) => ChooseEngine(EngineSelector);
    private void EnginePage_SelectionChanged(object sender, SelectionChangedEventArgs e) => ChooseEngine(EngineSelectorPage);

    private void ChooseEngine(ComboBox source)
    {
        if (settingEngine || source.SelectedItem is not ComboBoxItem item || work is not null) return;
        settingEngine = true;
        enginePrefs.Selected = (string)item.Tag;
        enginePrefs.Save();
        foreach (var combo in new[] { EngineSelector, EngineSelectorPage })
        {
            var chosen = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == SelectedEngine);
            if (chosen is not null && !ReferenceEquals(combo.SelectedItem, chosen)) combo.SelectedItem = chosen;
        }
        settingEngine = false;
        RefreshEngineUi();
        UpdateOutputPreview();
        Log("Motor selecionado: " + SelectedEngine);
    }

    private void RefreshEngineUi()
    {
        if (EngineStatusPage is null || ActiveEngineStatus is null || BuildBtn is null) return;
        var key = SelectedEngine;
        var configured = key switch
        {
            "pocket" => true,
            "local" => enginePrefs.LocalForkValid(),
            "zakuro" => enginePrefs.RecompilerValid(),
            _ => false
        };
        var state = key switch
        {
            "pocket" => "engineStatusOfficial",
            "local" => configured ? "engineStatusForkReady" : "engineStatusForkMissing",
            "zakuro" => configured ? "engineStatusZakuroReady" : "engineStatusZakuroMissing",
            _ => "engineStatusOptimized"
        };
        var desc = key switch
        {
            "pocket" => "engineDetailOfficial",
            "local" => "engineDetailFork",
            "zakuro" => "engineDetailZakuro",
            _ => "engineDetailOptimized"
        };
        var target = key == "zakuro" ? "engineTargetPc" : "engineTargetR36S";
        EngineStatusPage.Text = ActiveEngineStatus.Text = LanguageService.Text(state);
        EngineDetailsPage.Text = ActiveEngineDetails.Text = LanguageService.Text(desc);
        EngineTargetPage.Text = LanguageService.Text(target);
        var stateBrush = configured ? new SolidColorBrush(Color.FromRgb(123, 226, 178)) : new SolidColorBrush(Color.FromRgb(255, 194, 115));
        EngineStatusPage.Foreground = ActiveEngineStatus.Foreground = stateBrush;
        BuildBtn.IsEnabled = configured && work is null;
        PrepareBtn.IsEnabled = configured && work is null;
        OpenEngineSourceBtn.IsEnabled = key != "renative";
        ForkPathLabel.Text = LanguageService.Text("engineForkPath") + ": " + (string.IsNullOrWhiteSpace(enginePrefs.LocalForkDirectory) ? "—" : enginePrefs.LocalForkDirectory);
        RecompilerPathLabel.Text = "3dsrecomp: " + (string.IsNullOrWhiteSpace(enginePrefs.RecompilerExecutable) ? "—" : enginePrefs.RecompilerExecutable);
        SelectForkBtn.IsEnabled = work is null;
        SelectRecompilerBtn.IsEnabled = work is null;
    }

    private void SelectFork_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = LanguageService.Text("engineChooseFork"), Multiselect = false };
        if (dlg.ShowDialog(this) != true) return;
        var check = new EnginePreferences { LocalForkDirectory = dlg.FolderName };
        if (!check.LocalForkValid())
        {
            MessageBox.Show(this, LanguageService.Text("engineInvalidFork"), "ReNative3D36S", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        enginePrefs.LocalForkDirectory = dlg.FolderName;
        enginePrefs.Save();
        RefreshEngineUi();
    }
    private void SelectRecompiler_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Title = LanguageService.Text("engineChooseRecompiler"), Filter = "Windows executables (*.exe)|*.exe", CheckFileExists = true };
        if (dlg.ShowDialog(this) != true) return;
        var check = new EnginePreferences { RecompilerExecutable = dlg.FileName };
        if (!check.RecompilerValid())
        {
            MessageBox.Show(this, LanguageService.Text("engineInvalidRecompiler"), "ReNative3D36S", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        enginePrefs.RecompilerExecutable = dlg.FileName;
        enginePrefs.Save();
        RefreshEngineUi();
    }
    private void OpenEngineSource_Click(object sender, RoutedEventArgs e)
    {
        var url = SelectedEngine switch
        {
            "zakuro" => "https://github.com/fearkov/3dsrecomp",
            "local" => "https://github.com/whovian64/pocket-native",
            _ => "https://github.com/whovian64/pocket-native"
        };
        OpenWebsite(url);
    }
    private void OpenScarlet_Click(object sender, RoutedEventArgs e) => OpenWebsite("https://github.com/scarletnx/ScarletNX");
    private void OpenAzahar_Click(object sender, RoutedEventArgs e) => OpenWebsite("https://github.com/azahar-emu/azahar");


    private void SetPage(string page)
    {
        RecompilePage.Visibility = page == "recompile" ? Visibility.Visible : Visibility.Collapsed;
        UpdatesPage.Visibility = page == "updates" ? Visibility.Visible : Visibility.Collapsed;
        LibraryPage.Visibility = page == "library" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = page == "settings" ? Visibility.Visible : Visibility.Collapsed;
        EnginesPage.Visibility = page == "engines" ? Visibility.Visible : Visibility.Collapsed;
        foreach (var nav in new[] { NavRecompile, NavUpdates, NavLibrary, NavSettings, NavEngines })
            nav.Background = Brushes.Transparent;
        var selected = page switch
        {
            "updates" => NavUpdates,
            "library" => NavLibrary,
            "engines" => NavEngines,
            "settings" => NavSettings,
            _ => NavRecompile
        };
        selected.Background = new SolidColorBrush(Color.FromRgb(23, 78, 124));
    }

    private void Destination_TextChanged(object sender, TextChangedEventArgs e) => UpdateOutputPreview();
    private void FolderName_TextChanged(object sender, TextChangedEventArgs e) => UpdateOutputPreview();
    private void Naming_Checked(object sender, RoutedEventArgs e) => UpdateOutputPreview();

    private void UpdateOutputPreview()
    {
        // These may fire during InitializeComponent, before all named controls exist.
        if (RomStateText is null || RomPath is null || FolderPreviewRoot is null ||
            FolderPreviewName is null || FolderPreviewLauncher is null || NameFolderCheck is null ||
            FolderNameBox is null || OutPath is null) return;
        var rom = RomPath.Text;
        var valid = File.Exists(rom);
        var name = valid ? Path.GetFileNameWithoutExtension(rom) : "Jogo";
        if (name.Length > 64) name = name[..64];
        if (NameFolderCheck.IsChecked != true) name = "Game-ID";
        else if (!string.IsNullOrWhiteSpace(FolderNameBox.Text)) name = FolderNameBox.Text;
        name = SafeFolderName(name);
        if (SelectedEngine == "zakuro")
        {
            FolderPreviewRoot.Text = "Zakuro/3dsrecomp (Windows)";
            FolderPreviewName.Text = LanguageService.Text("engineZakuroOutput");
            FolderPreviewLauncher.Text = "";
            OutHint.Text = LanguageService.Text("engineDetailZakuro");
        }
        else
        {
        OutHint.Text = LanguageService.Text("copyInfo");
        FolderPreviewRoot.Text = "↳ " + (string.IsNullOrWhiteSpace(OutPath.Text) ? "Recomps" : OutPath.Text);
        FolderPreviewName.Text = "   ├── " + name + "/pocket-native/";
        FolderPreviewLauncher.Text = "   └── " + name + ".sh";
        }
        if (valid)
        {
            var info = new FileInfo(rom);
            RomStateText.Text = $"✓ {info.Length / (1024.0 * 1024.0):N1} MB";
            RomStateText.Foreground = Brushes.LightGreen;
        }
        else
        {
            RomStateText.Text = LanguageService.Text("noRomSelected");
            RomStateText.Foreground = new SolidColorBrush(Color.FromRgb(170,194,218));
        }
    }

    private void LibraryRefresh_Click(object sender, RoutedEventArgs e) => RefreshLibrary();
    private void RefreshLibrary()
    {
        if (LibraryList is null || OutPath is null) return;
        LibraryList.Items.Clear();
        if (!Directory.Exists(OutPath.Text)) return;
        foreach (var dir in Directory.EnumerateDirectories(OutPath.Text).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(Path.Combine(dir,"pocket-native")))
                LibraryList.Items.Add(Path.GetFileName(dir));
        }
    }
    private void LibraryOpen_Click(object sender, RoutedEventArgs e)
    {
        if (LibraryList.SelectedItem is not string name) { MessageBox.Show(LanguageService.Text("selectLibrary")); return; }
        var folder = Path.Combine(OutPath.Text, name);
        if (Directory.Exists(folder)) OpenDirectory(folder);
    }
    private void OpenKit_Click(object sender, RoutedEventArgs e)
    {
        if (Directory.Exists(kitDir)) OpenDirectory(kitDir);
        else MessageBox.Show(LanguageService.Text("kitNotReady"));
    }
    private void ClearLogs_Click(object sender, RoutedEventArgs e)
    {
        LogBox.Clear(); // Only the display is cleared; never erase diagnostic files.
        StatusLabel.Text = LanguageService.Text("logsCleared");
    }
    private async void CleanCache_Click(object sender, RoutedEventArgs e)
    {
        if (work is not null) return;
        if (MessageBox.Show(this, LanguageService.Text("confirmCleanCache"),
                "ReNative3D36S", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        await ExecuteAsync(async ct =>
        {
            var engine = await FindReadyEngineAsync(ct);
            if (engine is null) throw new InvalidOperationException(LanguageService.Text("dockerNotReady"));
            // Only the known, reusable compilation scratch volume. Never delete ROMs,
            // output folders, game saves, app settings, or the official source cache.
            var found = await RunProcessAsync(engine, new[] { "volume", "inspect", "pocket-native-work" }, kitDir, ct, quiet:true);
            if (found != 0) { Log(LanguageService.Text("cacheMissing")); Progress(100); return true; }
            var code = await RunProcessAsync(engine, new[] { "volume", "rm", "pocket-native-work" }, kitDir, ct);
            if (code != 0) throw new InvalidOperationException(LanguageService.Text("cacheBusy"));
            Log(LanguageService.Text("cacheCleared"));
            Progress(100, LanguageService.Text("cacheCleared"));
            return true;
        });
    }

    private static void OpenWebsite(string url)
        => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    private void OpenOriginal_Click(object sender, RoutedEventArgs e)
        => OpenWebsite("https://github.com/whovian64/pocket-native/blob/master/START_HERE.md");
    private void OpenDockerDocs_Click(object sender, RoutedEventArgs e)
        => OpenWebsite("https://docs.docker.com/desktop/setup/install/windows-install/");

    private void ApplyTranslations()
    {
        SubtitleText.Text = LanguageService.Text("subtitle");
        NavRecompile.Content = "⌂   " + LanguageService.Text("navRecompile");
        NavUpdates.Content = "↻   " + LanguageService.Text("navUpdates");
        NavEngines.Content = "⚙   " + LanguageService.Text("navEngines");
        NavLibrary.Content = "▣   " + LanguageService.Text("navLibrary");
        NavSettings.Content = "⚙   " + LanguageService.Text("navSettings");
        AboutBtn.Content = "♡   " + LanguageService.Text("about");
        RomTitle.Text = LanguageService.Text("rom");
        SelectRomBtn.Content = LanguageService.Text("chooseRom");
        RomHint.Text = LanguageService.Text("romHint");
        OutTitle.Text = LanguageService.Text("out");
        SelectOutBtn.Content = LanguageService.Text("chooseFolder");
        OutHint.Text = LanguageService.Text("copyInfo");
        NameFolderCheck.Content = LanguageService.Text("nameCheck");
        FolderNameLabel.Text = LanguageService.Text("folderName") + ":";
        OutputPreviewTitle.Text = LanguageService.Text("outputPreview");
        PrepTitle.Text = LanguageService.Text("prepTitle");
        PrepareBtn.Content = LanguageService.Text("prepare");
        BuildBtn.Content = LanguageService.Text("start");
        CancelBtn.Content = LanguageService.Text("cancel");
        OpenFolderBtn.Content = LanguageService.Text("openResult");
        OpenLogsBtn.Content = LanguageService.Text("openLogs");
        CheckUpdateBtn.Content = "↻  " + LanguageService.Text("checkUpdate");
        UpdateCoreBtn.Content = LanguageService.Text("updateCore");
        UpdateProfilesBtn.Content = LanguageService.Text("updateProfiles");
        UpdatePageCheckBtn.Content = "↻  " + LanguageService.Text("checkUpdate");
        UpdatePageCoreBtn.Content = "↓  " + LanguageService.Text("updateCore");
        UpdatePageProfilesBtn.Content = "▣  " + LanguageService.Text("updateProfiles");
        LanguageCardTitle.Text = "◎  " + LanguageService.Text("language");
        LanguageCaption.Text = LanguageService.Text("languageHint");
        ActivityTitle.Text = LanguageService.Text("activity");
        FooterLabel.Text = LanguageService.Text("footer");
        if (work is null) StatusLabel.Text = LanguageService.Text("ready");
        ProgressLabel.Text = $"{WorkProgress.Value:0}{LanguageService.Text("progress")}";
        Feature1.Text = LanguageService.Text("featureEasy");
        Feature2.Text = LanguageService.Text("featureR36S");
        Feature3.Text = LanguageService.Text("featureUpdates");
        Feature4.Text = "◎  PT / EN / ES / RU";
        EngineCardTitle.Text = "⚙  " + LanguageService.Text("engineTitle");
        InstalledCaption.Text = LanguageService.Text("installedVersion");
        AvailableCaption.Text = LanguageService.Text("availableVersion");
        QuickTitle.Text = LanguageService.Text("quickActions");
        QuickOutputBtn.Content = "▣  " + LanguageService.Text("openResult");
        QuickKitBtn.Content = "▣  " + LanguageService.Text("openKit");
        QuickLogBtn.Content = "▤  " + LanguageService.Text("openLogs");
        QuickCleanBtn.Content = "⚒  " + LanguageService.Text("cleanCache");
        UpdatesPageTitle.Text = LanguageService.Text("updatesTitle");
        UpdatesPageInfo.Text = LanguageService.Text("updatesInfo");
        UpdatesNoticeTitle.Text = LanguageService.Text("updatesNoticeTitle");
        UpdatesNoticeText.Text = LanguageService.Text("updatesNoticeText");
        LibraryTitle.Text = LanguageService.Text("libraryTitle");
        LibraryHint.Text = LanguageService.Text("libraryHint");
        RefreshLibraryBtn.Content = "↻  " + LanguageService.Text("refreshLibrary");
        OpenLibrarySelectionBtn.Content = LanguageService.Text("openSelection");
        SettingsTitle.Text = LanguageService.Text("navSettings");
        SettingsInfo.Text = LanguageService.Text("settingsInfo");
        SettingsLanguageTitle.Text = LanguageService.Text("language");
        SettingsHelpTitle.Text = LanguageService.Text("settingsHelpTitle");
        SettingsHelpText.Text = LanguageService.Text("settingsHelpText");
        OfficialDocsBtn.Content = LanguageService.Text("officialDocs");
        DockerDocsBtn.Content = LanguageService.Text("dockerDocs");
        ProgressDisclaimer.Text = LanguageService.Text("progressHint");
        ActiveEngineTitle.Text = LanguageService.Text("engineSelected");
        OpenEnginesBtn.Content = LanguageService.Text("engineManage");
        EnginesPageTitle.Text = LanguageService.Text("navEngines");
        EnginesPageIntro.Text = LanguageService.Text("enginePageIntro");
        EngineSourceTitle.Text = LanguageService.Text("engineSourceTitle");
        OpenEngineSourceBtn.Content = LanguageService.Text("engineOpenOriginal");
        EngineConfigTitle.Text = LanguageService.Text("engineConfigTitle");
        EngineConfigIntro.Text = LanguageService.Text("engineConfigIntro");
        SelectForkBtn.Content = LanguageService.Text("engineChooseFork");
        SelectRecompilerBtn.Content = LanguageService.Text("engineChooseRecompiler");
        EngineResearchTitle.Text = LanguageService.Text("engineResearchTitle");
        EngineResearchText.Text = LanguageService.Text("engineResearchText");
        foreach (var combo in new[] { EngineSelector, EngineSelectorPage })
            foreach (var item in combo.Items.OfType<ComboBoxItem>())
                item.Content = LanguageService.Text("engineName_" + (string)item.Tag);
        RefreshEngineUi();
        UpdateElapsedTime();
        SidebarStatusText.Text = work is null ? LanguageService.Text("readyShort") : LanguageService.Text("workingShort");
        LastCheckLabel.Text = lastCheckDate is null ? LanguageService.Text("notChecked")
            : LanguageService.Text("lastChecked") + " " + lastCheckDate.Value.ToString("HH:mm");
        UpdateOutputPreview();
    }

    private DateTime? lastCheckDate;
    private void UpdateInstalledLabels()
    {
        var installed = updater.InstalledVersion;
        VersionInstalledLabel.Text = installed == "not installed" || installed == "unknown" ? "—" : installed.TrimStart('v');
        VersionLabel.Text = "Pocket Native: " + installed;
    }

    private async Task CheckReleaseInBackgroundAsync()
    {
        try
        {
            var release = await updater.GetLatestAsync(CancellationToken.None);
            UpdateInstalledLabels();
            VersionAvailableLabel.Text = release is null ? "offline" : release.Version;
            VersionLabel.Text = $"Pocket Native: {updater.InstalledVersion}" + (release is null ? " (offline)" : $" / v{release.Version}");
            lastCheckDate = DateTime.Now;
            LastCheckLabel.Text = LanguageService.Text("lastChecked") + " " + lastCheckDate.Value.ToString("HH:mm");
            UpdatesDetails.Text = VersionLabel.Text;
            if (release is not null && updater.NeedsUpdate(release)) Log("Pocket Native stable update available: v"+release.Version);
        }
        catch(Exception ex) { Log("GitHub release check: "+ex.Message); }
    }
    private async void CheckUpdate_Click(object sender, RoutedEventArgs e) => await CheckReleaseInBackgroundAsync();
    private async void UpdateCore_Click(object sender, RoutedEventArgs e) => await ExecuteAsync(async ct =>
    {
        var release = await updater.GetLatestAsync(ct, refresh:true) ?? throw new InvalidOperationException(LanguageService.Text("offline"));
        if (!updater.NeedsUpdate(release)) { Log(LanguageService.Text("latest")); return true; }
        Progress(10, LanguageService.Text("checking"));
        await updater.InstallAsync(release,ct);
        UpdateInstalledLabels();
        Progress(100, LanguageService.Text("updated"));
        return true;
    });
    private async void UpdateProfiles_Click(object sender, RoutedEventArgs e) => await ExecuteAsync(async ct =>
    {
        var release = await updater.GetLatestAsync(ct,refresh:true) ?? throw new InvalidOperationException(LanguageService.Text("offline"));
        if(!Directory.Exists(kitDir)) await updater.InstallAsync(release,ct);
        Progress(10, LanguageService.Text("checking"));
        await updater.UpdateProfilesAsync(release,ct);
        Progress(100,LanguageService.Text("profilesUpdated"));
        return true;
    });

    private void UpdateElapsedTime()
        => ElapsedTimeText.Text = LanguageService.Text("elapsedTime") + " " + elapsedStopwatch.Elapsed.ToString(@"hh\:mm\:ss");

    private void Progress(int percent, string? message = null)
    {
        // Marcos reais de etapas concluídas. Não são um cálculo do tempo restante.
        WorkProgress.Value = Math.Max(WorkProgress.Value, Math.Clamp(percent, 0, 100));
        ProgressLabel.Text = $"{WorkProgress.Value:0}{LanguageService.Text("progress")}";
        if (message is not null) StatusLabel.Text = message;
    }

    private async Task ExecuteAsync(Func<CancellationToken, Task<bool>> job)
    {
        if (work is not null) return;
        work = new CancellationTokenSource();
        elapsedStopwatch.Restart();
        elapsedTimer.Start();
        UpdateElapsedTime();
        PrepareBtn.IsEnabled = BuildBtn.IsEnabled = CheckUpdateBtn.IsEnabled = UpdateCoreBtn.IsEnabled = UpdateProfilesBtn.IsEnabled = false;
        SelectForkBtn.IsEnabled = SelectRecompilerBtn.IsEnabled = false;
        EngineSelector.IsEnabled = EngineSelectorPage.IsEnabled = false;
        CancelBtn.IsEnabled = true;
        SidebarStatusText.Text = LanguageService.Text("workingShort");
        WorkProgress.IsIndeterminate = false;
        WorkProgress.Value = 0;
        ProgressLabel.Text = "0% • etapas concluídas (estimativa)";
        try
        {
            if (await job(work.Token)) Progress(100);
        }
        catch (OperationCanceledException)
        {
            Log("Operação cancelada pelo usuário.");
            StatusLabel.Text = "Operação cancelada.";
        }
        catch (Exception ex)
        {
            Log("ERRO: " + ex);
            StatusLabel.Text = "Falha. Consulte o registro de atividades.";
            MessageBox.Show(ex.Message + "\n\nLog: " + sessionLog, "ReNative3D36S", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            elapsedTimer.Stop();
            elapsedStopwatch.Stop();
            UpdateElapsedTime();
            work?.Dispose();
            work = null;
            CheckUpdateBtn.IsEnabled = UpdateCoreBtn.IsEnabled = UpdateProfilesBtn.IsEnabled = true;
            CancelBtn.IsEnabled = false;
            SidebarStatusText.Text = LanguageService.Text("readyShort");
            UpdateInstalledLabels();
            EngineSelector.IsEnabled = EngineSelectorPage.IsEnabled = true;
            RefreshEngineUi();
        }
    }

    private async Task<bool> PrepareOnlyAsync(CancellationToken ct)
    {
        if (SelectedEngine == "renative") throw new InvalidOperationException(LanguageService.Text("engineStatusOptimized"));
        if (SelectedEngine == "zakuro")
        {
            if (!enginePrefs.RecompilerValid()) throw new InvalidOperationException(LanguageService.Text("engineStatusZakuroMissing"));
            var code = await RunProcessAsync(enginePrefs.RecompilerExecutable, new[] { "--help" },
                Path.GetDirectoryName(enginePrefs.RecompilerExecutable)!, ct);
            if (code != 0) throw new InvalidOperationException("3dsrecomp --help returned exit code " + code);
            Progress(100, LanguageService.Text("engineStatusZakuroReady"));
            return true;
        }
        if (SelectedEngine == "local" && !enginePrefs.LocalForkValid())
            throw new InvalidOperationException(LanguageService.Text("engineStatusForkMissing"));
        var engine = await PrepareEnvironmentAsync(ct, SelectedEngine == "local");
        if (engine is null) return false;
        await EnsureBuildImageAsync(engine, ct, ActiveKit);
        Progress(100, "Ambiente pronto. Agora selecione sua ROM e inicie a recompilação.");
        Log("Preparação concluída com sucesso.");
        return true;
    }

    private async Task<bool> BuildGameAsync(CancellationToken ct)
    {
        var rom = RomPath.Text;
        if (!File.Exists(rom)) throw new FileNotFoundException("Selecione uma ROM existente antes de recompilar.");
        if (SelectedEngine == "renative") throw new InvalidOperationException(LanguageService.Text("engineStatusOptimized"));
        if (SelectedEngine == "zakuro") return await BuildForZakuroAsync(rom, ct);
        if (SelectedEngine == "local" && !enginePrefs.LocalForkValid())
            throw new InvalidOperationException(LanguageService.Text("engineStatusForkMissing"));
        var ext = Path.GetExtension(rom).ToLowerInvariant();
        if (SelectedEngine == "local" && ext == ".cia")
            throw new InvalidOperationException(LanguageService.Text("engineLocalCiaRestriction"));
        if (ext is not (".3ds" or ".cci" or ".cia")) throw new InvalidOperationException("Formato inválido. Aceitos: .3ds, .cci e .cia.");
        if (string.IsNullOrWhiteSpace(OutPath.Text)) throw new InvalidOperationException("Selecione a pasta de saída.");
        Progress(5, "ROM selecionada e verificada.");

        if (ext == ".cia")
        {
            // Verificar as chaves antes de baixar/iniciar Docker evita falhar aos 52%.
            // Valores criptográficos nunca são escritos no log ou exibidos na interface.
            keysPath = RequestValidatedKeys(rom);
            Progress(7, "Arquivo de chaves verificado. Preparando ambiente...");
        }

        var release = SelectedEngine == "pocket" ? await updater.GetLatestAsync(ct, refresh:true) : null;
        if (release is not null && updater.NeedsUpdate(release))
        {
            Log("Getting latest stable Pocket Native v"+release.Version+" before recompilation...");
            await updater.InstallAsync(release,ct);
            UpdateInstalledLabels();
        }
        var engine = await PrepareEnvironmentAsync(ct, SelectedEngine == "local");
        if (engine is null) return false;
        if (ext == ".cia") EnsureCiaConverterCompatibility();
        var selectedKit = ActiveKit;
        var image = await EnsureBuildImageAsync(engine, ct, selectedKit);
        Directory.CreateDirectory(OutPath.Text);
        var stage = Path.Combine(OutPath.Text, ".renative-stage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        Progress(48, "Recompilando o jogo para ARM64...");
        Log("Iniciando recompilação pelo script oficial scripts/container/inside.sh.");

        var romFolder = Path.GetDirectoryName(rom)!;
        var mounts = new List<string>
        {
            "run", "--rm", "-v", $"{selectedKit}:/src:ro", "-v", SelectedEngine == "local" ? "renative-local-work:/work" : "pocket-native-work:/work",
            "-v", $"{stage}:/out", "-v", $"{romFolder}:/rom:ro"
        };
        if (ext == ".cia") mounts.AddRange(new[] { "-v", $"{Path.GetDirectoryName(keysPath!)}:/keys:ro" });
        mounts.AddRange(new[]
        {
            "-e", "CARGO_HOME=/work/cargo", "-e", "CARGO_TARGET_DIR=/work/target",
            "-e", "HOME=/work", image, "bash", "/src/scripts/container/inside.sh", "/rom/" + Path.GetFileName(rom)
        });
        if (ext == ".cia") mounts.Add("/keys/" + Path.GetFileName(keysPath!));

        string? buildError = null;
        void TrackBuildOutput(string line)
        {
            OnBuildOutput(line);
            // O motor pode rejeitar ROMs e chaves mesmo após a validação inicial.
            // Não registrar nem repetir os valores sensíveis das chaves AES.
            if (line.Contains("KeyError: 'generator'", StringComparison.Ordinal))
                buildError = "O conversor CIA não encontrou a entrada 'generator' no aes_keys.txt. Selecione um arquivo de chaves completo, extraído do seu próprio console.";
            else if (line.Contains("KeyError: 'slot0x", StringComparison.Ordinal))
                buildError = "O aes_keys.txt não contém uma das entradas de chave exigidas pelo conversor CIA. Confira a versão e a origem do arquivo de chaves.";
            else if (line.Contains("CIA-layer encrypted", StringComparison.OrdinalIgnoreCase))
                buildError = "Este CIA contém criptografia na camada de instalação (titlekey), que o conversor atual do Pocket Native não suporta. Utilize um dump já descriptografado do seu próprio jogo.";
            else if (line.Contains("seeded NCCH", StringComparison.OrdinalIgnoreCase))
                buildError = "Este CIA precisa de seeddb, que o conversor atual do Pocket Native não suporta. Utilize um dump já descriptografado do seu próprio jogo.";
            else if (line.Contains("mounts denied", StringComparison.OrdinalIgnoreCase) ||
                     line.Contains("drive is not shared", StringComparison.OrdinalIgnoreCase) ||
                     line.Contains("not shared", StringComparison.OrdinalIgnoreCase) ||
                     line.Contains("invalid mount config", StringComparison.OrdinalIgnoreCase))
                buildError = LanguageService.Text("dockerSharingError");
        }
        try
        {
            int code = await RunProcessAsync(engine, mounts, selectedKit, ct, TrackBuildOutput);
            if (code != 0)
                throw new InvalidOperationException(buildError ?? $"A recompilação terminou com código {code}. Confira o log. Se mencionar bind mount, abra Docker Desktop e permita acesso às pastas da ROM e do destino.");
            var stagedOutput = Path.Combine(stage, "Pocket Native");
            var binaries = Path.Combine(stagedOutput, "pocket-native", "games");
            if (!Directory.Exists(stagedOutput) || !Directory.Exists(binaries) ||
                !Directory.EnumerateFiles(stagedOutput, "*.sh").Any() ||
                !Directory.EnumerateFiles(binaries, "title.so", SearchOption.AllDirectories).Any())
                throw new InvalidDataException("O motor terminou sem gerar launcher e biblioteca do jogo. Verifique o log.");
            var script = Directory.EnumerateFiles(stagedOutput, "*.sh").First();
            var titleId = Directory.EnumerateDirectories(binaries).Select(Path.GetFileName).FirstOrDefault() ?? "unknown";
            var displayedName = Path.GetFileNameWithoutExtension(script);
            var preferred = NameFolderCheck.IsChecked == true
                ? (string.IsNullOrWhiteSpace(FolderNameBox.Text) ? displayedName : FolderNameBox.Text)
                : $"Game-{titleId}";
            var safeName = SafeFolderName(preferred);
            var final = Path.Combine(OutPath.Text, safeName);
            int number = 2;
            while (Directory.Exists(final) || File.Exists(final + ".sh")) final = Path.Combine(OutPath.Text, safeName + " (" + number++ + ")");
            // run.sh resolves files relative to the game folder, so launcher + pocket-native must remain siblings.
            Directory.Move(stagedOutput, final);
            var rootName = Path.GetFileName(final);
            var launcherAtRoot = Path.Combine(OutPath.Text,rootName+".sh");
            // EmulationStation often needs launchers directly in roms/ports.
            // Quote the game folder safely even if its name has quotes or spaces.
            var shellFolder = "'" + rootName.Replace("'", "'\"'\"'") + "'";
            var shellId = "'" + titleId.Replace("'", "") + "'";
            File.WriteAllText(launcherAtRoot,
                "#!/bin/bash\nexec \"$(dirname \"$0\")\"/" + shellFolder + "/pocket-native/run.sh " + shellId + "\n",
                new UTF8Encoding(false));
            // Keep just the root launcher, to avoid duplicate entries in recursive Ports menus.
            File.Delete(Path.Combine(final,Path.GetFileName(script)));
            var metadata = new
            {
                app = "ReNative3D36S", appVersion = "0.2.0", upstream = SelectedEngine == "local" ? "Local Pocket Native-compatible fork" : "Pocket Native",
                engineId = SelectedEngine,
                engineVersion = SelectedEngine == "local" ? "local/unverified" : updater.InstalledVersion, titleId, gameName = displayedName,
                generatedUtc = DateTimeOffset.UtcNow, launcher = Path.GetFileName(launcherAtRoot)
            };
            File.WriteAllText(Path.Combine(final,"renative-info.json"),
                System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
                new UTF8Encoding(false));
            latestOutput = final;
            RefreshLibrary();
            UpdateOutputPreview();
            Log("Per-game folder: " + final);
            Log("Top-level launcher: " + launcherAtRoot);
            Log("Copy BOTH the game folder and its sibling .sh into roms/ports (or roms2/ports) on the game SD card.");
            Progress(100, LanguageService.Text("complete"));
            MessageBox.Show(this, LanguageService.Text("complete") + "\n\n" + final + "\n\n" + LanguageService.Text("copyInfo"), "ReNative3D36S", MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }
        finally
        {
            try { if (Directory.Exists(stage)) Directory.Delete(stage, recursive: true); }
            catch (Exception ex) { Log("Temporary build folder could not be removed: " + ex.Message); }
        }
    }

    private async Task<bool> BuildForZakuroAsync(string rom, CancellationToken ct)
    {
        if (!File.Exists(rom)) throw new FileNotFoundException(LanguageService.Text("romMissing"));
        if (Path.GetExtension(rom).ToLowerInvariant() is not (".3ds" or ".cci"))
            throw new InvalidOperationException(LanguageService.Text("engineZakuroRomRestriction"));
        if (!enginePrefs.RecompilerValid())
            throw new InvalidOperationException(LanguageService.Text("engineStatusZakuroMissing"));
        Progress(8, LanguageService.Text("engineZakuroRunning"));
        Log("3dsrecomp (external executable, Windows) selected. Output is managed by the official Zakuro/3dsrecomp tools.");
        Log("Please use a decrypted ROM from your own console. This operation does not generate R36S files.");
        int code = await RunProcessAsync(enginePrefs.RecompilerExecutable, new[] { "build", rom },
            Path.GetDirectoryName(enginePrefs.RecompilerExecutable)!, ct);
        if (code != 0) throw new InvalidOperationException("3dsrecomp build returned exit code " + code + ". Verify Rust/C compiler dependencies and see upstream documentation.");
        Progress(100, LanguageService.Text("engineZakuroComplete"));
        MessageBox.Show(this, LanguageService.Text("engineZakuroComplete"), "ReNative3D36S", MessageBoxButton.OK, MessageBoxImage.Information);
        return true;
    }

    private static string SafeFolderName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c,'-');
        value = value.Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(value)) value = "Game";
        if (value.Length > 90) value = value[..90];
        if (new[] {"CON","PRN","AUX","NUL","COM1","LPT1"}.Contains(value,StringComparer.OrdinalIgnoreCase)) value = "Game-"+value;
        return value;
    }

    private void EnsureCiaConverterCompatibility()
    {
        // O Pocket Native atual lê apenas "generator", enquanto alguns dumps
        // de aes_keys.txt usam "generatorConstant" ou omitem a constante pública.
        // Adapta SOMENTE a cópia em cache do script; não modifica aes_keys.txt.
        const string label = "# ReNative3D36S compatibility: public 3DS key scrambler constant";
        var path = Path.Combine(kitDir, "tools", "cia2cci.py");
        if (!File.Exists(path))
            throw new FileNotFoundException("O conversor CIA do Pocket Native não foi encontrado no cache.", path);

        var source = File.ReadAllText(path);
        if (source.Contains(label, StringComparison.Ordinal))
        {
            Log("Conversor CIA compatível com generator/generatorConstant.");
            return;
        }

        var begin = source.IndexOf("def load_keys(path):", StringComparison.Ordinal);
        var end = begin < 0 ? -1 : source.IndexOf("def normal_key", begin, StringComparison.Ordinal);
        var returnKey = begin < 0 ? -1 : source.IndexOf("    return k", begin, StringComparison.Ordinal);
        if (begin < 0 || end < 0 || returnKey < 0 || returnKey >= end)
            throw new InvalidDataException("A versão atual do conversor CIA mudou e não pode receber a adaptação automática com segurança. Atualize o núcleo ou utilize uma ROM .3ds/.cci já descriptografada.");

        var extra = label + "\n" +
            "    if \"generator\" not in k:\n" +
            "        k[\"generator\"] = k.get(\"generatorConstant\", 0x1FF9E9AAC5FE0408024591DC5D52768A)\n";
        // A linha de marcação deve ter quatro espaços de indentação como as
        // outras linhas da função load_keys. Sem isso o Python rejeita o script.
        extra = "    " + extra;
        var updated = source.Insert(returnKey, extra);

        // A primeira aplicação preserva o original, para facilitar reversão.
        var backup = path + ".original";
        if (!File.Exists(backup)) File.Copy(path, backup);
        File.WriteAllText(path, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Log("Adaptador CIA aplicado ao conversor em cache: generator ausente será preenchido pela constante pública, sem modificar suas chaves AES.");
    }

    private string RequestValidatedKeys(string rom)
    {
        var beside = Path.Combine(Path.GetDirectoryName(rom)!, "aes_keys.txt");
        var inKit = Path.Combine(kitDir, "aes_keys.txt");
        // O arquivo escolhido manualmente tem prioridade sobre os padrões encontrados.
        // Isto permite corrigir arquivos incompletos sem renomeá-los nem editá-los.
        var candidates = new List<string>();
        if (keysPath is not null && File.Exists(keysPath)) candidates.Add(keysPath);
        if (File.Exists(beside) && !candidates.Contains(beside, StringComparer.OrdinalIgnoreCase)) candidates.Add(beside);
        if (File.Exists(inKit) && !candidates.Contains(inKit, StringComparer.OrdinalIgnoreCase)) candidates.Add(inKit);

        foreach (var candidate in candidates)
        {
            var problem = CheckKeysFile(candidate);
            if (problem is null)
            {
                Log("Arquivo aes_keys.txt validado (presença e formato das entradas; valores não registrados).");
                return candidate;
            }
            Log("O aes_keys.txt encontrado não passou na verificação: " + problem);
        }

        while (true)
        {
            MessageBox.Show(
                "Para converter esta ROM CIA, o Pocket Native precisa de um aes_keys.txt compatível, extraído do seu próprio console. " +
                "É necessário incluir a entrada 'slot0x2CKeyX' quando o conteúdo estiver criptografado. " +
                "A constante pública 'generator' é complementada automaticamente pelo ReNative3D36S quando necessário.\n\n" +
                "O arquivo encontrado está ausente ou incompleto. Selecione outro arquivo de chaves. " +
                "Os valores das chaves não serão mostrados no log.\n\n" +
                "Alternativa: selecione uma ROM .3ds/.cci já descriptografada, obtida do seu próprio jogo.",
                "Verificação de chaves CIA", MessageBoxButton.OK, MessageBoxImage.Information);
            var dlg = new OpenFileDialog
            {
                Title = "Selecione um aes_keys.txt completo, do seu próprio console",
                Filter = "Arquivo de chaves|aes_keys.txt|Arquivos de texto|*.txt",
                FileName = "aes_keys.txt",
                CheckFileExists = true
            };
            if (dlg.ShowDialog(this) != true)
                throw new OperationCanceledException("Seleção de chaves cancelada. Nenhum arquivo foi modificado.");

            var problem = CheckKeysFile(dlg.FileName);
            if (problem is null)
            {
                Log("Arquivo aes_keys.txt validado (presença e formato das entradas; valores não registrados).");
                return dlg.FileName;
            }
            Log("O arquivo de chaves escolhido não passou na verificação: " + problem);
            var retry = MessageBox.Show(problem + "\n\nDeseja escolher outro arquivo?", "Chaves CIA incompatíveis",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (retry != MessageBoxResult.Yes)
                throw new InvalidDataException("O aes_keys.txt selecionado está incompleto. " + problem);
        }
    }

    private static string? CheckKeysFile(string path)
    {
        try
        {
            using (var file = File.OpenRead(path))
            {
                Span<byte> bom = stackalloc byte[3];
                if (file.Read(bom) == 3 && bom.SequenceEqual(new byte[] { 0xEF, 0xBB, 0xBF }))
                    return "O arquivo contém marca BOM de UTF-8, não reconhecida pelo leitor atual do Pocket Native. Salve uma cópia como UTF-8 sem BOM.";
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in File.ReadLines(path))
            {
                int equal = line.IndexOf('=');
                if (equal < 0) continue;
                var name = line[..equal].Trim();
                var value = line[(equal + 1)..].Trim();
                var hex = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
                if (hex.Length == 0 || !hex.All(Uri.IsHexDigit))
                    return $"A entrada '{name}' não está em formato hexadecimal reconhecido pelo conversor.";
                seen.Add(name);
            }
            // A constante pública generator pode estar ausente: o adaptador local
            // do conversor a completa automaticamente (sem fabricar chaves KeyX).
            if (!seen.Contains("slot0x2CKeyX"))
                return "Falta a entrada 'slot0x2CKeyX' no aes_keys.txt, necessária para conteúdos NCCH criptografados.";
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "Não foi possível ler o arquivo de chaves: " + ex.Message;
        }
    }

    private void OnBuildOutput(string line)
    {
        if (line.Contains("== decrypting the .cia", StringComparison.OrdinalIgnoreCase)) Progress(52, "Descriptografando arquivo CIA...");
        else if (line.Contains("== building the recompiler", StringComparison.OrdinalIgnoreCase)) Progress(57, "Compilando o recompilador e o motor...");
        else if (line.Contains("== recompiling the game", StringComparison.OrdinalIgnoreCase)) Progress(70, "Recompilando o código do jogo...");
        else if (line.Contains("== making the system font", StringComparison.OrdinalIgnoreCase)) Progress(82, "Preparando a fonte do sistema...");
        else if (line.Contains("== putting Pocket Native together", StringComparison.OrdinalIgnoreCase)) Progress(91, "Montando os arquivos de saída...");
        else if (line.Contains("== done:", StringComparison.OrdinalIgnoreCase)) Progress(96, "Validando arquivos gerados...");
    }

    private async Task<string?> PrepareEnvironmentAsync(CancellationToken ct, bool custom = false)
    {
        Progress(8, "Verificando núcleo do Pocket Native...");
        if (custom)
        {
            if (!enginePrefs.LocalForkValid()) throw new InvalidOperationException(LanguageService.Text("engineInvalidFork"));
            Log("Using a user-selected compatible local fork: " + enginePrefs.LocalForkDirectory);
        }
        else await EnsureKitAsync(ct);
        Progress(26, "Verificando Docker/Podman...");
        var engine = await FindReadyEngineAsync(ct);
        if (engine is not null)
        {
            Log("Motor de contêiner pronto: " + engine);
            Log("Docker Desktop must remain open. If you see a mount/drive sharing error, allow access to the ROM/output drive in Docker Desktop settings.");
            Progress(35, "Contêiner disponível.");
            return engine;
        }

        var candidates = FindEngineCandidates();
        if (candidates.Count == 0)
        {
            var choice = MessageBox.Show(
                "Docker Desktop ou Podman não foram encontrados.\n\nDeseja instalar o Docker Desktop usando o winget? O Windows pode solicitar permissão de administrador e reinicialização.",
                "Preparar ambiente", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (choice != MessageBoxResult.Yes)
            {
                Progress(26, "Dependência pendente: instale Docker ou Podman.");
                Log("Instalação do contêiner adiada pelo usuário.");
                return null;
            }
            Log("Instalando Docker Desktop via gerenciador oficial do Windows...");
            var install = await RunProcessAsync("winget.exe", new[] { "install", "-e", "--id", "Docker.DockerDesktop", "--accept-source-agreements", "--accept-package-agreements" }, ActiveKit, ct);
            if (install != 0)
                throw new InvalidOperationException($"Não foi possível instalar o Docker Desktop (código {install}). Verifique o log e as permissões do Windows.");
            Log("Instalador do Docker finalizado. Verificando o serviço...");
        }
        else Log("Contêiner instalado, porém o serviço ainda não responde.");

        TryStartDockerDesktop();
        Progress(30, "Aguardando inicialização do Docker/Podman...");
        // O Docker Desktop pode solicitar que o usuário conclua WSL2 ou reinicie o Windows.
        for (int attempt = 0; attempt < 40; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            engine = await FindReadyEngineAsync(ct);
            if (engine is not null)
            {
                Progress(35, "Motor de contêiner pronto.");
                Log("Contêiner iniciado e respondendo aos comandos.");
                return engine;
            }
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
        Progress(30, "Docker/Podman instalado, mas ainda não está pronto.");
        Log("Aguardando ação do Windows: abra Docker Desktop, conclua WSL2/virtualização e retome. O código do Pocket Native permanece salvo.");
        MessageBox.Show("O Docker Desktop ainda não respondeu. Abra o Docker Desktop e conclua qualquer configuração de WSL2/virtualização solicitada. Caso o Windows peça reinicialização, reinicie e clique novamente em 'Iniciar recompilação'.\n\nNenhum jogo foi recompilado nesta tentativa.", "Preparação pendente", MessageBoxButton.OK, MessageBoxImage.Information);
        return null;
    }

    private async Task EnsureKitAsync(CancellationToken ct)
    {
        var inner = Path.Combine(kitDir,"scripts","container","inside.sh");
        var container = Path.Combine(kitDir,"scripts","container","Containerfile");
        if (File.Exists(inner) && File.Exists(container))
        {
            Log("Pocket Native kit in local cache: " + updater.InstalledVersion);
            Progress(25, "Pocket Native ready.");
            return;
        }
        var release = await updater.GetLatestAsync(ct,refresh:true);
        if (release is null) throw new InvalidOperationException("Pocket Native kit is not installed and GitHub could not be reached. Connect to the internet and retry.");
        Log("Downloading latest stable Pocket Native v"+release.Version);
        await updater.InstallAsync(release,ct);
        Progress(25,"Pocket Native source downloaded.");
        UpdateInstalledLabels();
    }

    private async Task<string> EnsureBuildImageAsync(string engine, CancellationToken ct, string sourceKit)
    {
        var containerFile = Path.Combine(sourceKit, "scripts", "container", "Containerfile");
        var hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(containerFile, ct))).ToLowerInvariant()[..12];
        var imageName = "pocket-native-build:" + hash;
        Progress(37, "Verificando a imagem de compilação...");
        if (await RunProcessAsync(engine, new[] { "image", "inspect", imageName }, sourceKit, ct, quiet: true) == 0)
        {
            Log("Imagem de compilação já existe: " + imageName);
            Progress(47, "Ambiente de compilação disponível.");
            return imageName;
        }
        Progress(39, "Criando imagem de compilação (primeira vez pode demorar)...");
        Log("Construindo imagem Docker/Podman oficial. O primeiro uso pode baixar centenas de MB.");
        int code = await RunProcessAsync(engine, new[] { "build", "-f", containerFile, "-t", imageName, Path.GetDirectoryName(containerFile)! }, sourceKit, ct);
        if (code != 0) throw new InvalidOperationException($"Falha na criação da imagem (código {code}). Consulte o log para os detalhes do Docker/Podman.");
        Progress(47, "Ambiente de compilação preparado.");
        return imageName;
    }

    private List<string> FindEngineCandidates()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var candidates = new[]
        {
            "docker.exe", Path.Combine(programFiles, "Docker", "Docker", "resources", "bin", "docker.exe"),
            "podman.exe", Path.Combine(programFiles, "RedHat", "Podman", "podman.exe")
        };
        var available = new List<string>();
        foreach (var candidate in candidates)
        {
            if (Path.IsPathFullyQualified(candidate))
            {
                if (File.Exists(candidate) && !available.Contains(candidate, StringComparer.OrdinalIgnoreCase)) available.Add(candidate);
            }
            else
            {
                var paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
                foreach (var dir in paths)
                {
                    var full = Path.Combine(dir.Trim('"'), candidate);
                    if (File.Exists(full) && !available.Contains(full, StringComparer.OrdinalIgnoreCase)) available.Add(full);
                }
            }
        }
        return available;
    }

    private async Task<string?> FindReadyEngineAsync(CancellationToken ct)
    {
        foreach (var engine in FindEngineCandidates())
        {
            try
            {
                using var probe = CancellationTokenSource.CreateLinkedTokenSource(ct);
                probe.CancelAfter(TimeSpan.FromSeconds(10));
                if (await RunProcessAsync(engine, new[] { "info" }, Directory.Exists(ActiveKit) ? ActiveKit : Environment.CurrentDirectory, probe.Token, quiet: true) == 0) return engine;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { /* CLI travou: testa próxima */ }
            catch (System.ComponentModel.Win32Exception) { /* removido ou inacessível */ }
        }
        return null;
    }

    private void TryStartDockerDesktop()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var gui = Path.Combine(programFiles, "Docker", "Docker", "Docker Desktop.exe");
        if (!File.Exists(gui)) return;
        try { Process.Start(new ProcessStartInfo(gui) { UseShellExecute = true }); Log("Solicitada inicialização do Docker Desktop."); }
        catch (Exception ex) { Log("Não foi possível abrir Docker Desktop automaticamente: " + ex.Message); }
    }

    private async Task<int> RunProcessAsync(string exe, IEnumerable<string> args, string cwd, CancellationToken ct,
        Action<string>? onOutput = null, bool quiet = false)
    {
        var start = new ProcessStartInfo(exe)
        {
            WorkingDirectory = cwd, UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var a in args) start.ArgumentList.Add(a);
        using var process = new Process { StartInfo = start };
        if (!process.Start()) throw new InvalidOperationException("Não foi possível iniciar o processo: " + exe);
        using var registration = ct.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });
        async Task ConsumeAsync(StreamReader reader)
        {
            while (await reader.ReadLineAsync(ct) is { } line)
            {
                if (!quiet)
                    await Dispatcher.InvokeAsync(() => { Log(line); onOutput?.Invoke(line); });
            }
        }
        await Task.WhenAll(ConsumeAsync(process.StandardOutput), ConsumeAsync(process.StandardError), process.WaitForExitAsync(ct));
        ct.ThrowIfCancellationRequested();
        return process.ExitCode;
    }

    private void Log(string line)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}";
        LogBox.AppendText(entry);
        // Mantém a janela responsiva durante builds longos; o arquivo preserva tudo.
        if (LogBox.Text.Length > 150_000) LogBox.Text = LogBox.Text[^100_000..];
        LogBox.ScrollToEnd();
        try { File.AppendAllText(sessionLog, entry, Encoding.UTF8); }
        catch { /* falha de permissão no log não deve cancelar recompilação */ }
    }
}
