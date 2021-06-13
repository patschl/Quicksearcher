using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Quicksearcher
{
    public class Quicksearcher : ApplicationContext
    {
        private bool _active = true;

        private bool _autostart;

        private bool _searchHighlightedText;

        private readonly NotifyIcon _taskbarIcon;

        private readonly KeyboardHook _hook = new();

        private RegistryKey _hotkeyRegistryConfig;

        private RegistryKey _autostartConfig;

        private const string RegistryKey = "Quicksearcher";
        private const string ActiveName = "Active";
        private const string HotkeyName = "Hotkey";
        private const string SearchHighlightedTextName = "SearchHighlightedText";

        public Quicksearcher()
        {
            Initialize();
            _taskbarIcon = new NotifyIcon
            {
                Visible = true,
                Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Quicksearcher.Resource.pic.ico")),
                Text = @"Quicksearcher",
                ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem("Active", Active) {Checked = _active},
                    new MenuItem("Search Highlighted Text", ToggleMode) {Checked = _searchHighlightedText},
                    new MenuItem("Set Hotkey", SetHotkey),
                    new MenuItem("Launch on startup", ToggleAutostart) {Checked = _autostart},
                    new MenuItem("About", About),
                    new MenuItem("Quit", Quit)
                })
            };
        }

        private void Initialize()
        {
            var key = Registry.CurrentUser.OpenSubKey("Software", true);

            _hotkeyRegistryConfig = key!.OpenSubKey(RegistryKey, true) is { } subKey
                ? subKey
                : key.CreateSubKey(RegistryKey, true);

            LoadConfig();

            _hook.KeyPressed += HotkeyHandler;
        }

        private void Active(object sender, EventArgs eventArgs)
        {
            if (sender is not MenuItem active)
                return;

            active.Checked = !active.Checked;
            _active = active.Checked;
            _hotkeyRegistryConfig.SetValue(ActiveName, _active);
        }

        private void ToggleMode(object sender, EventArgs eventArgs)
        {
            if (sender is not MenuItem searchHighlighted)
                return;

            searchHighlighted.Checked = !searchHighlighted.Checked;
            _searchHighlightedText = searchHighlighted.Checked;
            _hotkeyRegistryConfig.SetValue(SearchHighlightedTextName, _searchHighlightedText);
        }

        private void SetHotkey(object sender, EventArgs eventArgs)
        {
            if (HotkeyPopup.RecordKey() is not { } recordKey)
                return;

            _hook.Unregister();
            _hook.RegisterHotkey(recordKey);
            SaveHotkey(recordKey);
        }

        private void ToggleAutostart(object sender, EventArgs eventArgs)
        {
            if (sender is not MenuItem autostart)
                return;
            
            autostart.Checked = !autostart.Checked;
            _autostart = autostart.Checked;
            if (autostart.Checked)
                _autostartConfig.SetValue(Application.ProductName, Application.ExecutablePath);
            else
                _autostartConfig.DeleteValue(Application.ProductName, false);
        }

        private static void About(object sender, EventArgs eventArgs)
        {
            new About().Show();
        }

        private void Quit(object sender, EventArgs eventArgs)
        {
            _taskbarIcon.Visible = false;
            Application.Exit();
        }

        private void HotkeyHandler(object sender, KeyPressedEventArgs eventArgs)
        {
            if (!_active)
                return;
            
            if (_searchHighlightedText)
                SendKeys.SendWait("^(c)");

            var text = Clipboard.GetText(TextDataFormat.UnicodeText);

            if (!string.IsNullOrEmpty(text))
                Process.Start("https://www.google.com/search?q=" + Uri.EscapeDataString(text));
        }
        
        private void SaveHotkey(KeyEventArgs hotkey)
        {
            var serializableHotkey = KeyPressedEventArgs.FromKeyEventArgs(hotkey);
            using var ms = new MemoryStream();
            new BinaryFormatter().Serialize(ms, serializableHotkey);
            _hotkeyRegistryConfig.SetValue(HotkeyName, ms.ToArray(), RegistryValueKind.Binary);
        }

        private void LoadConfig()
        {
            _autostartConfig = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            
            if (_autostartConfig != null && _autostartConfig.GetValueNames().Contains(Application.ProductName))
                _autostart = true;
            
            if (_hotkeyRegistryConfig.GetValue(SearchHighlightedTextName) is string active)
                _active = bool.Parse(active);            
            
            if (_hotkeyRegistryConfig.GetValue(SearchHighlightedTextName) is string searchHighlightedText)
                _searchHighlightedText = bool.Parse(searchHighlightedText);

            if (_hotkeyRegistryConfig.GetValue(HotkeyName, RegistryValueKind.Binary) is not byte[] binaryHotkey)
                return;

            using var ms = new MemoryStream(binaryHotkey);
            var binaryFormatter = new BinaryFormatter();
            _hook.RegisterHotkey(binaryFormatter.Deserialize(ms) as KeyPressedEventArgs);
        }
    }
}