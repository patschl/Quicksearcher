using System.Windows.Forms;

namespace Quicksearcher
{
    public partial class HotkeyPopup : Form
    {
        private HotkeyPopup()
        {
            InitializeComponent();
        }

        private KeyEventArgs _hotkey;
        
        public static KeyEventArgs RecordKey()
        {
            var form = new HotkeyPopup();
            form.ShowDialog();
            
            return form._hotkey;
        }

        private void HotkeyPopup_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.ControlKey or Keys.Menu or Keys.ShiftKey:
                    return;
                case Keys.Escape:
                    _hotkey = null;
                    break;
                default:
                    _hotkey = e;
                    break;
            }

            Close();
        }
    }
}