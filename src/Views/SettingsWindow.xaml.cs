using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ScreenHealthGuardian.Services;

namespace ScreenHealthGuardian.Views;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly LocalizationService _locService;
    private readonly Action? _onSaved;

    public SettingsWindow(ConfigService configService, LocalizationService locService, Action? onSaved = null)
    {
        InitializeComponent();

        _configService = configService;
        _locService = locService;
        _onSaved = onSaved;

        LoadSettings();
        ApplyLocalization();
    }

    private void LoadSettings()
    {
        var config = _configService.Current;

        TxtLookAway.Text = config.LookAwayIntervalMin.ToString();
        TxtPosture.Text = config.PostureIntervalMin.ToString();
        TxtIdle.Text = Math.Max(1, config.IdleThresholdSec / 60).ToString();
        TxtDismiss.Text = config.AlertAutoDismissSec.ToString();

        ChkSound.IsChecked = config.SoundEnabled;
        ChkAutoStart.IsChecked = config.AutoStart;

        foreach (ComboBoxItem item in CmbLanguage.Items)
        {
            if ((string)item.Tag == config.Language)
            {
                CmbLanguage.SelectedItem = item;
                break;
            }
        }

        string displayMode = config.DisplayMode ?? "all";
        foreach (ComboBoxItem item in CmbDisplayMode.Items)
        {
            if ((string)item.Tag == displayMode)
            {
                CmbDisplayMode.SelectedItem = item;
                break;
            }
        }
        if (CmbDisplayMode.SelectedItem == null && CmbDisplayMode.Items.Count > 0)
        {
            CmbDisplayMode.SelectedIndex = 0;
        }
    }

    private void ApplyLocalization()
    {
        HeaderTitle.Text = _locService.Get("settings_title");
        HeaderSubtitle.Text = _locService.Get("settings_subtitle");
        SecIntervalsLabel.Text = _locService.Get("settings_sec_intervals");
        EyeRestLabel.Text = _locService.Get("settings_eye_rest");
        PostureLabel.Text = _locService.Get("settings_posture");
        IdleLabel.Text = _locService.Get("settings_idle");
        DismissLabel.Text = _locService.Get("settings_dismiss");

        SecPreferencesLabel.Text = _locService.Get("settings_sec_preferences");
        ChkSound.Content = _locService.Get("settings_sound");
        ChkAutoStart.Content = _locService.Get("settings_autostart");
        LanguageLabel.Text = _locService.Get("settings_language");
        DisplayModeLabel.Text = _locService.Get("settings_display_mode");
        CmbItemDisplayAll.Content = _locService.Get("settings_display_mode_all");
        CmbItemDisplayActive.Content = _locService.Get("settings_display_mode_active");

        BtnCancel.Content = _locService.Get("settings_cancel");
        BtnSave.Content = _locService.Get("settings_save");
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtLookAway.Text, out int lookAway) || lookAway < 1) lookAway = 20;
        if (!int.TryParse(TxtPosture.Text, out int posture) || posture < 1) posture = 45;
        if (!int.TryParse(TxtIdle.Text, out int idleMin) || idleMin < 1) idleMin = 2;
        if (!int.TryParse(TxtDismiss.Text, out int dismiss) || dismiss < 5) dismiss = 30;

        string lang = "en";
        if (CmbLanguage.SelectedItem is ComboBoxItem selectedLang)
        {
            lang = (string)selectedLang.Tag;
        }

        string displayMode = "all";
        if (CmbDisplayMode.SelectedItem is ComboBoxItem selectedDisplay)
        {
            displayMode = (string)selectedDisplay.Tag;
        }

        bool autoStart = ChkAutoStart.IsChecked ?? false;
        bool sound = ChkSound.IsChecked ?? true;

        _configService.Update(c =>
        {
            c.LookAwayIntervalMin = lookAway;
            c.PostureIntervalMin = posture;
            c.IdleThresholdSec = idleMin * 60;
            c.AlertAutoDismissSec = dismiss;
            c.SoundEnabled = sound;
            c.AutoStart = autoStart;
            c.Language = lang;
            c.DisplayMode = displayMode;
        });

        _locService.SetLanguage(lang);
        SyncAutoStartRegistry(autoStart);

        _onSaved?.Invoke();
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SyncAutoStartRegistry(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            string exePath = Environment.ProcessPath ?? "";
            if (enable && !string.IsNullOrEmpty(exePath))
            {
                key.SetValue("ScreenHealthGuardian", $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue("ScreenHealthGuardian", false);
            }
        }
        catch
        {
            // Ignore registry permission errors
        }
    }
}
