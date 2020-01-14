﻿using System;
using System.Windows.Forms;

namespace WakaTime.WinForms
{
    public partial class SettingsForm : Form
    {
        internal event EventHandler ConfigSaved;

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            try
            {
                txtAPIKey.Text = WakaTimeConfigFile.ApiKey;
                txtProxy.Text = WakaTimeConfigFile.Proxy;
                chkDebugMode.Checked = WakaTimeConfigFile.Debug;
            }
            catch (Exception ex)
            {
                Logger.Error("Error when loading form SettingsForm:", ex);
                MessageBox.Show(ex.Message);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            try
            {
                Guid apiKey;
                var parse = Guid.TryParse(txtAPIKey.Text.Trim(), out apiKey);

                if (parse)
                {
                    WakaTimeConfigFile.ApiKey = apiKey.ToString();
                    WakaTimeConfigFile.Proxy = txtProxy.Text.Trim();
                    WakaTimeConfigFile.Debug = chkDebugMode.Checked;
                    WakaTimeConfigFile.Save();
                    OnConfigSaved();
                }
                else
                {
                    MessageBox.Show(@"Please enter valid Api Key.");
                    DialogResult = DialogResult.None; // do not close dialog box
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error when saving data from SettingsForm:", ex);
                MessageBox.Show(ex.Message);
            }
        }

        protected virtual void OnConfigSaved()
        {
            var handler = ConfigSaved;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
