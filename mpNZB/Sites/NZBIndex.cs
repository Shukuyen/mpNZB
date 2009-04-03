﻿using System;
using System.Xml;

using MediaPortal.GUI.Library;

namespace mpNZB.Sites
{
  class NZBIndex:iSite
  {
    
    #region Definitions

    public string SiteName
    {
      get { return "NZBIndex"; }
    }

    private string _FeedName;
    public string FeedName
    {
      get { return _FeedName; }
      set { _FeedName = value; }
    }

    private string _FeedURL;
    public string FeedURL
    {
      get { return _FeedURL; }
      set { _FeedURL = value; }
    }

    #endregion    

    #region Functions

    private mpFunctions Dialogs = new mpFunctions();

    public void SetFeed()
    {
    }

    public void Search()
    {
      FeedName = Dialogs.Keyboard();
      if (FeedName.Length > 0)
      {
        Dialogs.Wait();
        FeedURL = "http://www.nzbindex.com/rss/?q=" + FeedName + "&sort=dateTime&max=250";
        Dialogs.bolWaiting = false;
      }
    }

    public void AddItem(XmlNode Node, GUIListControl lstList)
    {
      string strTemp = Node["description"].InnerText.Replace(" ", String.Empty);
      string strSizeText = "<b>".ToLower();
      int intSizePOS = strTemp.ToLower().IndexOf(strSizeText) + strSizeText.Length;

      Dialogs.AddItem(lstList, Node["title"].InnerText, strTemp.Substring(intSizePOS, strTemp.IndexOf("</b>", intSizePOS) - intSizePOS), Node["enclosure"].Attributes["url"].InnerText, 1);
    }

    #endregion

  }
}