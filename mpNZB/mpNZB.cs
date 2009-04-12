﻿using System;
using System.Xml;

using MediaPortal.GUI.Library;
using MediaPortal.Profile;

namespace mpNZB
{
  public class mpNZB : GUIWindow, ISetupForm
  {

    #region SkinControlAttribute

    [SkinControlAttribute(1)]
    protected GUIButtonControl btnRefreshFeed = null;
    [SkinControlAttribute(2)]
    protected GUIButtonControl btnSelectFeed = null;
    [SkinControlAttribute(3)]
    protected GUIButtonControl btnSearch = null;
    [SkinControlAttribute(4)]
    protected GUIButtonControl btnJobQueue = null;

    [SkinControlAttribute(5)]
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
      if (control == btnRefreshFeed)
      {
        GUIPropertyManager.SetProperty("#Status", "Processing...");
        GUIWindowManager.Process();

        ReadRSS(Site.FeedURL, lstItems);
      }
      if (control == btnSelectFeed)  { SelectSite(false); }
      if (control == btnSearch)      { SelectSite(true); }
      if (control == btnJobQueue)    { Client.Queue(lstItems, this); }
      if (control == btnPause)       { Client.Pause(btnPause.Selected); }
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
            Client.Delete(lstItems, this);
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
        btnRefreshFeed.Disabled = true;

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
              Client = new Clients.SABnzbd(mpSettings.GetValue("#Client", "Host"), mpSettings.GetValue("#Client", "Port"), mpSettings.GetValueAsBool("#Client", "CatSelect", false), mpSettings.GetValueAsBool("#Client", "Auth", false), mpSettings.GetValue("#Client", "Username"), mpSettings.GetValue("#Client", "Password"), mpSettings.GetValueAsInt("#Plugin", "UpdateFrequency", 1));
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
    }

    #endregion

    #region Functions

    private void ReadRSS(string _URL, GUIListControl _List)
    {
      try
      {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(new XmlTextReader(_URL));

        if (xmlDoc.SelectSingleNode("rss[@version='2.0']") != null)
        {
          _List.Clear();

          XmlNodeList xmlNodes = xmlDoc.SelectNodes("rss/channel/item");
          foreach (XmlNode xmlNode in xmlNodes)
          {
            Site.AddItem(xmlNode, _List);
          }

          GUIPropertyManager.SetProperty("#Status", "Found " + xmlNodes.Count.ToString() + " Items");
        }
        else
        {
          GUIPropertyManager.SetProperty("#Status", "Error parsing XML");
        }
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

    private void SelectSite(bool _Search)
    {
      try
      {
        Site = new Sites(_Search);

        if (Site.SiteName.Length != 0)
        {
          if (_Search)
          {
            Site.SetSearch();
          }
          else
          {
            Site.SetFeed();
          }

          if (Site.FeedURL != String.Empty)
          {
            GUIPropertyManager.SetProperty("#Status", "Processing...");
            GUIWindowManager.Process();

            ReadRSS(Site.FeedURL, lstItems);

            btnRefreshFeed.Disabled = false;
            GUIPropertyManager.SetProperty("#PageTitle", Site.SiteName + " - " + Site.FeedName);
          }
        }
      }
      catch (Exception e) { MP.Error(e); }
    }

    #endregion

  }
}