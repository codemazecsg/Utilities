using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace ClipPad
{
    public partial class frmClipPad : Form
    {
        int rows = 4;
        int cols = 5;

        public frmClipPad()
        {
            InitializeComponent();
        }

        private void frmClipPad_Load(object sender, EventArgs e)
        {

            // must only run 1 b/c of file access conflicts
            Process[] ps = System.Diagnostics.Process.GetProcesses();

            int pcnt = 0;
            foreach (Process p in ps)
            {
                if (p.ProcessName.ToLower() == "clippad")
                {
                    pcnt++;
                }
            }

            // check
            if (pcnt > 1)
            {
                MessageBox.Show("ClipPad is already running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            if (!Directory.Exists("ClipPadNotes"))
            {
                Directory.CreateDirectory("ClipPadNotes");
            }

            // fixed size
            this.Width = 950;
            this.Height = 500;

            int cnt = 0;

            // create boxes
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    TextBox t = new TextBox();
                    t.Multiline = true;
                    t.BorderStyle = BorderStyle.FixedSingle;
                    t.Height = ((this.Height - 40) / rows);
                    t.Width = ((this.Width - 14) / cols);
                    t.Left = (i * t.Width);
                    t.Top = (j * t.Height);
                    t.Tag = cnt.ToString("00");
                    t.Cursor = Cursors.Arrow;
                    t.MouseDown += new MouseEventHandler(txtClipPadBox_MouseDown);
                    t.TextChanged += new System.EventHandler(txtClipPadBox_TextChanged);
                    t.KeyDown += new System.Windows.Forms.KeyEventHandler(txtClipPadBox_KeyDown);

                    if (File.Exists(String.Format(@"{0}\ClipPad{1}.txt", "ClipPadNotes", cnt.ToString("00"))))
                    {
                        string data = File.ReadAllText(String.Format(@"{0}\ClipPad{1}.txt", "ClipPadNotes", cnt.ToString("00")));
                        t.Text = data;
                    }

                    this.Controls.Add(t);
                    cnt++;
                }
            }
        }

        private void txtClipPadBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.D)
            {
                e.SuppressKeyPress = true;
                TextBox t = (TextBox)sender;
                t.Text = "";
            }

            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.X)
            {
                e.SuppressKeyPress = true;
                foreach (Control c in this.Controls)
                {
                    TextBox t = (TextBox)c;

                    t.Text = "";
                }
            }
        }

        private void txtClipPadBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TextBox t = (TextBox)sender;

                t.Text = Clipboard.GetText();

                setData(t.Text, t.Tag.ToString());

                t.ContextMenu = new ContextMenu();
            }
            else if (e.Button == MouseButtons.Left)
            {
                TextBox t = (TextBox)sender;

                if (t.Text != null && t.Text.Length > 0)
                    Clipboard.SetText(t.Text);
            }
            else
            {
                // do nothing
            }
        }

        private void txtClipPadBox_TextChanged(object sender, EventArgs e)
        {
            TextBox t = (TextBox)sender;

            setData(t.Text, t.Tag.ToString());
        }

        private void setData(string data, string tag)
        {
            File.WriteAllText(String.Format(@"{0}\ClipPad{1}.txt", "ClipPadNotes", tag.ToString()), data);
        }

        private void frmClipPad_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Oemplus)
            {
                if (this.Opacity < 100)
                {
                    this.Opacity += .05d;
                }
            }
            else if (e.KeyCode == Keys.OemMinus)
            {
                if (this.Opacity > 0)
                {
                    this.Opacity -= .05d;
                }
            }
            else
            {
                // nothing for now
            }
        }

    }
}
