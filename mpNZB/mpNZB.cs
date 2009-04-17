﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml;

using MediaPortal.GUI.Library;
using MediaPortal.Profile;

namespace mpNZB
{
  public class mpNZB : GUIWindow, ISetupForm
  {

    #region SkinControlAttribute

    [SkinControlAttribute(1)]
    protected GUIButtonControl btnRefresh = null;
    [SkinControlAttribute(2)]
    protected GUIButtonControl btnFeeds = null;
    [SkinControlAttribute(3)]
    protected GUIButtonControl btnGroups = null;
    [SkinControlAttribute(4)]
    protected GUIButtonControl btnSearch = null;
    [SkinControlAttribute(5)]
    protected GUIButtonControl btnJobQueue = null;

    [SkinControlAttribute(6)]
    protected GUIToggleButtonControl btnPause = null;

    [SkinControlAttribute(50)]
    protected GUIListControl lstItems = null;

    #endregion

    #region ISetupForm Members

    // Returns the name of the plugin which is shown in the plugin menu
    public string PluginName()
    {
      return "mpNZB";
    }

    // Returns the description of the plugin is shown in the plugin menu
    public string Description()
    {
      return "NZB Downloader";
    }

    // Returns the author of the plugin which is shown in the plugin menu
    public string Author()
    {
      return "Dustin Brett";
    }

    // show the setup dialog
    public void ShowPlugin()
    {
      frmSetup frmSetupDialog = new frmSetup();
      frmSetupDialog.Show();
    }

    // Indicates whether plugin can be enabled/disabled
    public bool CanEnable()
    {
      return true;
    }

    // Get Windows-ID
    public int GetWindowId()
    {
      // WindowID of windowplugin belonging to this setup
      // enter your own unique code
      return 3847;
    }

    // Indicates if plugin is enabled by default;
    public bool DefaultEnabled()
    {
      return true;
    }

    // indicates if a plugin has it's own setup screen
    public bool HasSetup()
    {
      return true;
    }

    /// <summary>
    /// If the plugin should have it's own button on the main menu of MediaPortal then it
    /// should return true to this method, otherwise if it should not be on home
    /// it should return false
    /// </summary>
    /// <param name="strButtonText">text the button should have</param>
    /// <param name="strButtonImage">image for the button, or empty for default</param>
    /// <param name="strButtonImageFocus">image for the button, or empty for default</param>
    /// <param name="strPictureImage">subpicture for the button or empty for none</param>
    /// <returns>true : plugin needs it's own button on home
    /// false : plugin does not need it's own button on home</returns>

    public bool GetHome(out string strButtonText, out string strButtonImage,
      out string strButtonImageFocus, out string strPictureImage)
    {
      // ##################################################
      // Load button text
      // ##################################################
      Settings mpSettings = new Settings(MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Config) + @"\mpNZB.xml");
      strButtonText = mpSettings.GetValue("#Plugin", "DisplayName");
      if (strButtonText.Length == 0) { strButtonText = "mpNZB"; }
      mpSettings.Dispose();
      // ##################################################

      strButtonImage = String.Empty;
      strButtonImageFocus = String.Empty;
      strPictureImage = String.Empty;
      return true;
    }

    // With GetID it will be an window-plugin / otherwise a process-plugin
    // Enter the id number here again
    public override int GetID
    {
      get
      {
        return 3847;
      }

      set
      {
      }
    }

    #endregion

    #region Definition

    private Sites Site;
    private Clients.iClient Client;

    private mpFunctions MP = new mpFunctions();

    #endregion

    #region On<Action>

    public override bool Init()
    {
      // Set "Status" label
      GUIPropertyManager.SetProperty("#Status", "Idle");

      // Load skin
      return Load(GUIGraphicsContext.Skin + @"\mpNZB.xml");
    }

    protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
    {
      if (control == btnRefresh)
      {
        GUIPropertyManager.SetProperty("#Status", "Processing...");
        GUIWindowManager.Process();

        ReadRSS(Site.FeedURL, lstItems);
      }
      if (control == btnFeeds)    { SelectSite("Feeds"); }
      if (control == btnGroups)   { SelectSite("Groups"); }
      if (control == btnSearch)   { SelectSite("Search"); }
      if (control == btnJobQueue) { Client.Queue(lstItems, this); }
      if (control == btnPause)    { Client.Pause(btnPause.Selected); }
      if (control == lstItems)
      {
        switch (lstItems.ListItems[lstItems.SelectedListItemIndex].ItemId)
        {

          // ID Types
          // ====================
          // 1 = NZB
          // 2 = Newzbin ID
          // 3 = Queue Item
          // ====================

          case 1:
          case 2:
            Client.Download(lstItems.ListItems[lstItems.SelectedListItemIndex]);
            break;
          case 3:
            Client.QueueItem(lstItems, this);
            break;
        }
      }
      base.OnClicked(controlId, control, actionType);
    }
    
    protected override void OnPageLoad()
    {
      try
      {
        // Disable "Refresh Feed" button
        btnRefresh.Disabled = true;

        // Load Settings
        Settings mpSettings = new Settings(MediaPortal.Configuration.Config.GetFolder(MediaPortal.Configuration.Config.Dir.Config) + @"\mpNZB.xml");

        // Setup Page Title
        // ##################################################
        string strPageTitle = mpSettings.GetValue("#Plugin", "DisplayName");
        GUIPropertyManager.SetProperty("#PageTitle", ((strPageTitle.Length == 0) ? "mpNZB" : strPageTitle));
        // ##################################################

        // Setup Client
        // ##################################################
        switch (mpSettings.GetValue("#Client", "Grabber"))
        {
          case "SABnzbd":
            {
              Client = new Clients.SABnzbd(mpSettings.GetValue("#Client", "Host"), mpSettings.GetValue("#Client", "Port"), mpSettings.GetValue("#Client", "APIKey"), mpSettings.GetValueAsBool("#Client", "CatSelect", false), mpSettings.GetValueAsBool("#Client", "Auth", false), mpSettings.GetValue("#Client", "Username"), mpSettings.GetValue("#Client", "Password"), mpSettings.GetValueAsInt("#Plugin", "UpdateFrequency", 1));
              if (Client.Version().Length == 0)
              {
                MP.OK("SABnzbd connection failed.", "Client Status");
                return;
              }
              break;
            }
        }
        // ##################################################

        // Unload Settings
        mpSettings.Dispose();

        // Update Status
        Client.Status();
      }
      catch (Exception e) { MP.Error(e); }
    }

    protected override void OnPageDestroy(int newWindowId)
    {
      base.OnPageDestroy(newWindowId);
      Client.PluginVisible = false;
    }

    protected override void OnShowContextMenu()
    {
      if (lstItems.Count > 1)
      {
        List<GUIListItem> _Items = new List<GUIListItem>();

        _Items.Add(new GUIListItem("Name"));
        _Items.Add(new GUIListItem("Date"));
        _Items.Add(new GUIListItem("Size"));

        GUIListItem _Method = MP.Menu(_Items, "Sort By");
        if (_Method != null)
        {
          _Items = new List<GUIListItem>();

          _Items.Add(new GUIListItem("Ascending"));
          _Items.Add(new GUIListItem("Descending"));

          GUIListItem _Order = MP.Menu(_Items, "Sort Order");
          if (_Order != null)
          {
            switch (_Method.Label)
            {
              case "Name":
                lstItems.ListItems.Sort(delegate(GUIListItem _Item1, GUIListItem _Item2) { return ((_Order.Label == "Ascending") ? _Item1.Label.CompareTo(_Item2.Label) : _Item2.Label.CompareTo(_Item1.Label)); });
                break;
              case "Date":
                lstItems.ListItems.Sort(delegate(GUIListItem _Item1, GUIListItem _Item2) { return ((_Order.Label == "Ascending") ? _Item1.FileInfo.CreationTime.CompareTo(_Item2.FileInfo.CreationTime) : _Item2.FileInfo.CreationTime.CompareTo(_Item1.FileInfo.CreationTime)); });
                break;
              case "Size":
                lstItems.ListItems.Sort(delegate(GUIListItem _Item1, GUIListItem _Item2) { return ((_Order.Label == "Ascending") ? _Item1.FileInfo.Length.CompareTo(_Item2.FileInfo.Length) : _Item2.FileInfo.Length.CompareTo(_Item1.FileInfo.Length)); });
                break;
            }
            GUIPropertyManager.SetProperty("#Status", "Sorted by " + _Method.Label + " (" + _Order.Label + ")");
          }
        }
      }
    }

    #endregion

    #region Functions

    private void ReadRSS(List<string> _URL, GUIListControl _List)
    {
      try
      {
        int intItemCount = 0;
        _List.Clear();

        foreach (string URL in _URL)
        {
          HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(URL);
          webReq.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip");
          HttpWebResponse webResp = (HttpWebResponse)webReq.GetResponse();

          XmlDocument xmlDoc = new XmlDocument();
          xmlDoc.Load(((webResp.ContentEncoding.ToLower().Contains("gzip")) ? new GZipStream(webResp.GetResponseStream(), CompressionMode.Decompress, false) : webResp.GetResponseStream()));
          webResp.Close();

          if (xmlDoc.SelectSingleNode("rss[@version='2.0']") != null)
          {
            XmlNodeList xmlNodes = xmlDoc.SelectNodes("rss/channel/item");
            foreach (XmlNode xmlNode in xmlNodes)
            {
              Site.AddItem(xmlNode, _List);
              intItemCount += 1;
            }
          }
          else
          {
            GUIPropertyManager.SetProperty("#Status", "Error parsing XML");
          }
        }

        _List.ListItems.Sort(delegate(GUIListItem _Item1, GUIListItem _Item2) { return _Item2.FileInfo.CreationTime.CompareTo(_Item1.FileInfo.CreationTime); });
        GUIPropertyManager.SetProperty("#Status", "Found " + intItemCount.ToString() + " Items");
      }
      catch (Exception e) { MP.Error(e); }
      finally
      {
        if (_List.Count > 0)
        {
          this.LooseFocus();
          _List.Focus = true;
        }
      }
    }

    private void SelectSite(string strType)
    {
      try
      {
        Site = new Sites(strType);

        if (Site.SiteName.Length != 0)
        {
          switch (strType)
          {
            case "Feeds":
              Site.SetFeed();
              break;
            case "Groups":
              Site.SetGroup();
              break;
            case "Search":
              Site.SetSearch();
              break;
          }

          if (Site.FeedURL.Count > 0)
          {
            GUIPropertyManager.SetProperty("#Status", "Processing...");
            GUIWindowManager.Process();

            ReadRSS(Site.FeedURL, lstItems);

            btnRefresh.Disabled = false;
            GUIPropertyManager.SetProperty("#PageTitle", Site.SiteName + " [" + Site.FeedName + "]");
          }
        }
      }
      catch (Exception e) { MP.Error(e); }
    }

    #endregion

  }
}