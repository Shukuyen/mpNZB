﻿using System;
using System.Windows.Forms;
using System.Xml;

using MediaPortal.Profile;

namespace mpNZB
{
  public partial class frmSetup : Form
  {
    public frmSetup()
    {
      InitializeComponent();
    }

    private void frmSetup_Load(object sender, EventArgs e)
    {
      Settings mpSettings = new Settings(MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Config) + @"\mpNZB.xml");

      // Plugin Settings
      // ##################################################
      txtUpdateFreq.Text = mpSettings.GetValueAsInt("#Plugin", "UpdateFrequency", 1).ToString();
      txtDisplayName.Text = mpSettings.GetValue("#Plugin", "DisplayName");
      // ##################################################

      // Client Settings
      // ##################################################
      string strGrabber = mpSettings.GetValue("#Client", "Grabber");
      cmbGrabber.Text = ((strGrabber.Length > 0) ? strGrabber : "SABnzbd");

      string strHost = mpSettings.GetValue("#Client", "Host");
      string strPort = mpSettings.GetValue("#Client", "Port");
      txtHost.Text = ((strHost.Length > 0) ? strHost : "127.0.0.1");
      txtPort.Text = ((strPort.Length > 0) ? strPort : "8080");
      txtAPIKey.Text = mpSettings.GetValue("#Client", "APIKey");

      chkCatSelect.Enabled = (cmbGrabber.Text == "SABnzbd");
      chkCatSelect.Checked = mpSettings.GetValueAsBool("#Client", "CatSelect", false);

      chkAuth.Checked = mpSettings.GetValueAsBool("#Client", "Auth", false);
      txtUsername.Enabled = chkAuth.Checked;
      txtPassword.Enabled = chkAuth.Checked;
      txtUsername.Text = mpSettings.GetValue("#Client", "Username");
      txtPassword.Text = mpSettings.GetValue("#Client", "Password");
      // ##################################################

      // Site Settings
      // ##################################################
      txtMaxResults.Text = mpSettings.GetValueAsInt("#Sites", "MaxResults", 50).ToString();
      chkMyTVSeries.Checked = mpSettings.GetValueAsBool("#Sites", "MyTVSeries", false);
      // ##################################################

      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.Load(MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Config) + @"\mpNZB.xml");

      // Searches
      // ##################################################
      foreach (XmlNode nodeItem in xmlDoc.SelectNodes("profile/section[@name='#Searches']/entry"))
      {
        lvSearches.Items.Add(nodeItem.Attributes["name"].InnerText).SubItems.Add(nodeItem.InnerText);
        mpSettings.RemoveEntry("#Searches", nodeItem.Attributes["name"].InnerText);
      }
      // ##################################################

      // Groups
      // ##################################################
      foreach (XmlNode nodeItem in xmlDoc.SelectNodes("profile/section[@name='#Groups']/entry"))
      {
        lvGroups.Items.Add(nodeItem.Attributes["name"].InnerText);
        mpSettings.RemoveEntry("#Groups", nodeItem.Attributes["name"].InnerText);
      }
      // ##################################################

      mpSettings.Dispose();
    }

    private void fncSaveConfig()
    {
      Settings mpSettings = new Settings(MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Config) + @"\mpNZB.xml");
      
      // Plugin Settings
      // ##################################################
      int intUpdateFreq = 1;
      int.TryParse(txtUpdateFreq.Text, out intUpdateFreq);
      mpSettings.SetValue("#Plugin", "UpdateFrequency", intUpdateFreq);
      mpSettings.SetValue("#Plugin", "DisplayName", txtDisplayName.Text);
      // ##################################################

      // Client Settings
      // ##################################################
      mpSettings.SetValue("#Client", "Grabber", cmbGrabber.Text);

      mpSettings.SetValue("#Client", "Host", txtHost.Text);
      mpSettings.SetValue("#Client", "Port", txtPort.Text);
      mpSettings.SetValue("#Client", "APIKey", txtAPIKey.Text);

      mpSettings.SetValueAsBool("#Client", "CatSelect", chkCatSelect.Checked);
      
      mpSettings.SetValueAsBool("#Client", "Auth", chkAuth.Checked);
      mpSettings.SetValue("#Client", "Username", txtUsername.Text);
      mpSettings.SetValue("#Client", "Password", txtPassword.Text);
      // ##################################################

      // Site Settings
      // ##################################################
      int intMaxResults = 50;
      int.TryParse(txtMaxResults.Text, out intMaxResults);
      mpSettings.SetValue("#Sites", "MaxResults", intMaxResults);
      mpSettings.SetValueAsBool("#Sites", "MyTVSeries", chkMyTVSeries.Checked);
      // ##################################################

      // Searches
      // ##################################################
      foreach (ListViewItem Item in lvSearches.Items)
      {
        mpSettings.SetValue("#Searches", Item.SubItems[0].Text, Item.SubItems[1].Text);
      }
      // ##################################################

      // Groups
      // ##################################################
      foreach (ListViewItem Item in lvGroups.Items)
      {
        mpSettings.SetValue("#Groups", Item.Text, String.Empty);
      }
      // ##################################################

      mpSettings.Dispose();
    }

    private void btnOK_Click(object sender, EventArgs e)
    {
      fncSaveConfig();
      this.Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
      this.Close();
    }

    private void chkAuth_CheckedChanged(object sender, EventArgs e)
    {
      txtUsername.Enabled = chkAuth.Checked;
      txtPassword.Enabled = chkAuth.Checked;
    }

    private void btnTestConn_Click(object sender, EventArgs e)
    {
      // Setup Client
      // ##################################################
      Clients.iClient Client;
      switch (cmbGrabber.Text)
      {
        case "SABnzbd":
          Client = new Clients.SABnzbd(txtHost.Text, txtPort.Text, txtAPIKey.Text, false, chkAuth.Checked, txtUsername.Text, txtPassword.Text, 1);
          if (Client.Version().Length != 0)
          {
            MessageBox.Show(null, "Connection: OK", "Status", MessageBoxButtons.OK, MessageBoxIcon.Information);
          }
          else
          {
            MessageBox.Show(null, "Connection: Failed", "Status", MessageBoxButtons.OK, MessageBoxIcon.Error);
          }
          break;
      }
      // ##################################################
    }

    private void btnSearchAdd_Click(object sender, EventArgs e)
    {
      if ((txtSearchName.Text.Length > 0) && (txtSearchString.Text.Length > 0))
      {
        lvSearches.Items.Add(txtSearchName.Text).SubItems.Add(txtSearchString.Text);
        txtSearchName.Text = "";
        txtSearchString.Text = "";
      }
      else
      {
        MessageBox.Show(null, "Please fill all fields.", "Status", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void btnSearchDelete_Click(object sender, EventArgs e)
    {
      if (lvSearches.SelectedItems.Count > 0)
      {
        lvSearches.Items.Remove(lvSearches.SelectedItems[0]);
      }
      else
      {
        MessageBox.Show(null, "Nothing selected.", "Status", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void btnGroupAdd_Click(object sender, EventArgs e)
    {
      if (txtGroup.Text.Length > 0)
      {
        lvGroups.Items.Add(txtGroup.Text);
        txtGroup.Text = "";
      }
      else
      {
        MessageBox.Show(null, "Please fill all fields.", "Status", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void btnGroupDelete_Click(object sender, EventArgs e)
    {
      if (lvGroups.SelectedItems.Count > 0)
      {
        lvGroups.Items.Remove(lvGroups.SelectedItems[0]);
      }
      else
      {
        MessageBox.Show(null, "Nothing selected.", "Status", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }
  }
}