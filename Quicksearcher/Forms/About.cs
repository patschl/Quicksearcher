using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Quicksearcher
{
    public partial class About : Form
    {
        public About()
        {
            InitializeComponent();
        }

        private void ok_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void github_link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/patschl/Quicksearcher");
        }
    }
}