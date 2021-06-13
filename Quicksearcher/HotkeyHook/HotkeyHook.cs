using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Quicksearcher
{
    public sealed class KeyboardHook : IDisposable
    {
        private sealed class Window : NativeWindow, IDisposable
        {
            private const int WmHotkey = 0x0312;

            public Window() => CreateHandle(new CreateParams());

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                
                if (m.Msg != WmHotkey) return;

                var key = (Keys) (((int) m.LParam >> 16) & 0xFFFF);
                var modifier = (ModifierKeys) ((int) m.LParam & 0xFFFF);
                
                KeyPressed?.Invoke(this, new KeyPressedEventArgs(modifier, key));
            }

            public event EventHandler<KeyPressedEventArgs> KeyPressed;

            #region IDisposable Members

            public void Dispose() => DestroyHandle();

            #endregion
        }

        private readonly Window _window = new();

        private const int HotkeyId = 1337;

        public KeyboardHook()
        {
            _window.KeyPressed += delegate(object _, KeyPressedEventArgs args)
            {
                KeyPressed?.Invoke(this, args);
            };
        }

        public void RegisterHotkey(KeyEventArgs args)
        {
            RegisterHotkey(KeyPressedEventArgs.FromKeyEventArgs(args));
        }

        public void RegisterHotkey(KeyPressedEventArgs keyPressedEventArgs)
        {
            RegisterHotkey(keyPressedEventArgs.Modifier, keyPressedEventArgs.Key);
        }

        private void RegisterHotkey(ModifierKeys modifier, Keys key)
        {
            if (!RegisterHotKey(_window.Handle, HotkeyId, (uint) modifier, (uint) key))
                throw new InvalidOperationException("Couldn’t register the hot key.");
        }

        public void Unregister() => UnregisterHotKey(_window.Handle, HotkeyId);

        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        #region IDisposable Members

        public void Dispose()
        {
            UnregisterHotKey(_window.Handle, HotkeyId);
            _window.Dispose();
        }

        #endregion
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
    
    [Serializable]
    public class KeyPressedEventArgs : EventArgs
    {
        public KeyPressedEventArgs() {}
        
        public KeyPressedEventArgs(ModifierKeys modifier, Keys key)
        {
            Modifier = modifier;
            Key = key;
        }

        public ModifierKeys Modifier { get; }

        public Keys Key { get; }

        public static KeyPressedEventArgs FromKeyEventArgs(KeyEventArgs keyEventArgs)
        {
            var mod = ModifierKeys.None;
            mod |= keyEventArgs.Alt ? ModifierKeys.Alt : ModifierKeys.None;
            mod |= keyEventArgs.Control ? ModifierKeys.Control : ModifierKeys.None;
            mod |= keyEventArgs.Shift ? ModifierKeys.Shift : ModifierKeys.None;

            return new KeyPressedEventArgs(mod, keyEventArgs.KeyCode);
        }
    }

    [Flags]
    public enum ModifierKeys : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4
    }
    
    
}