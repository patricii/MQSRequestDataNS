﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MQSRequestData
{
    public partial class ApkMQS : MetroFramework.Forms.MetroForm
    {
        public ApkMQS()
        {
            InitializeComponent();

        }
        string url = "mqs.motorola.com";
        string urlPareto = "mqs.motorola.com/NTF_Pareto2/Default10.aspx?enc=INbDb3OXdMp3vde2LbmjfOqENqspcgXNx/9sSFuBt4l9YJObzeJfOcCHKc3GbKGAGwWF5fcyX0zSJaKBMrGv7/9C3vQtCHLGErFQnT+6UylYGmdsJPlvfKLrkaYE5qCz";
        string home = "MQS Home";
        string pareto = "MQS - Failure & NTF Pareto Report";
        string user = string.Empty;
        string password = string.Empty;
        string erroMsg = string.Empty;
        string strAction = string.Empty;
        ConverterHtmlToDt conv = new ConverterHtmlToDt();

        /*protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            buttonRun_Click(null, null);
        }*/
        private void webpage_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            ((Dictionary<string, object>)((WebBrowser)sender).Tag)["NavigationError"] = string.Empty;
            ((Dictionary<string, object>)((WebBrowser)sender).Tag)["Navigated"] = false;

        }
        public void runApp()
        {
            if (checkBoxLogin.Checked)
            {
                user = textBoxUser.Text;
                password = textBoxPassword.Text;
                erroMsg = GetYieldThreadSafeWithLogin(user, password, url, home);
            }
            else
                erroMsg = GetYieldThreadSafe(url, home);

            if (erroMsg == string.Empty)
            {
                labelStatus.Text = "Page loaded successfully!";
                //MessageBox.Show("MQS Data Request downloaded successfully!");
            }
            else
            {
                labelStatus.Text = "Page Error: " + erroMsg;
                //MessageBox.Show("Page Error: " + erroMsg);
            }
            Close();
        }
        private void buttonRun_Click(object sender, EventArgs e)
        {
            runApp();
        }
        public void webBrowser1_NewWindow(object sender, CancelEventArgs e)
        {
            e.Cancel = true; // Cancel the new window event
            WebBrowser browser = (WebBrowser)sender; // Get the current WebBrowser control
            // Open the new URL in the current WebBrowser control
            browser.Navigate(browser.StatusText);
        }
        private void webpage_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                string domain;

                if (((WebBrowser)sender).Document != null)
                    domain = ((WebBrowser)sender).Document.Domain;
                else
                    throw new Exception("Page Docment does not exist");

                if (((WebBrowser)sender).Tag == null)
                    throw new Exception("Web Infos cannot be null");


                if (!((Dictionary<string, object>)((WebBrowser)sender).Tag).ContainsKey("URL_Title"))
                    throw new Exception("Web infos missing URL_Title Key");

                string strExpectedTitle = ((Dictionary<string, object>)((WebBrowser)sender).Tag)["URL_Title"].ToString();

                if (strExpectedTitle != null && !((WebBrowser)sender).Document.Title.Equals(strExpectedTitle))
                    throw new Exception("page reached mismatch");

            }
            catch (Exception error)
            {
                if (!((Dictionary<string, object>)((WebBrowser)sender).Tag).ContainsKey("NavigationError"))
                    throw new Exception("Web infos missing NavigationError Key");

                ((Dictionary<string, object>)((WebBrowser)sender).Tag)["NavigationError"] = error.Message;

                if (error.TargetSite.Name.Equals("GetDomain"))
                    ((Dictionary<string, object>)((WebBrowser)sender).Tag)["NavigationError"] = "Cannot load Page";

            }

            if (!((Dictionary<string, object>)((WebBrowser)sender).Tag).ContainsKey("Navigated"))
                throw new Exception("Web infos missing Navigated Key");

            ((Dictionary<string, object>)((WebBrowser)sender).Tag)["Navigated"] = true;

        }

        public string GetYieldThreadSafeWithLogin(string user, string password, string url, string urlTitle = null)
        {
            string errorMessage = string.Empty;
            labelStatus.Text = "Connecting to MQS System..";

            try
            {
                using (WebBrowser webComponent = new WebBrowser())
                {
                    TabPage tab = new TabPage();
                    metroTabControl1.Controls.Add(tab);
                    metroTabControl1.SelectTab(metroTabControl1.TabCount - 1);
                    webComponent.Parent = tab;
                    webComponent.Dock = DockStyle.Fill;

                    webComponent.Navigating += new WebBrowserNavigatingEventHandler(webpage_Navigating);
                    webComponent.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webpage_DocumentCompleted);
                    webComponent.NewWindow += new CancelEventHandler(webBrowser1_NewWindow);


                    Dictionary<string, object> WebInfos = new Dictionary<string, object>() { { "NavigationError", "" }, { "Navigated", false }, { "URL_Title", urlTitle } };

                    webComponent.Tag = WebInfos;
                    webComponent.Navigate(url);

                    do
                    {
                        Application.DoEvents();
                        Thread.Sleep(1);
                    } while ((bool)((Dictionary<string, object>)webComponent.Tag)["Navigated"] == false);

                    if (!string.IsNullOrEmpty(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString()))
                        throw new Exception(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString());

                    labelStatus.Text = "Connected!!!";

                    loginMQS(webComponent);

                    errorMessage = StartBrowser(webComponent, url, urlTitle);

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        Thread.Sleep(1000);
                        errorMessage = StartBrowser(webComponent, url, urlTitle);
                    }

                    // metroTabControl1.SelectedTab.Text = webComponent.DocumentTitle;

                    navigateYieldTab(webComponent);

                    //New TAB "MQS - Yield Report"

                    webComponent.Navigate(urlPareto);
                    WebInfos = new Dictionary<string, object>() { { "NavigationError", "" }, { "Navigated", false }, { "URL_Title", pareto } };
                    webComponent.Tag = WebInfos;
                    do
                    {
                        Application.DoEvents();
                        Thread.Sleep(1);
                    } while ((bool)((Dictionary<string, object>)webComponent.Tag)["Navigated"] == false);

                    if (!string.IsNullOrEmpty(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString()))
                        throw new Exception(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString());

                    //metroTabControl1.SelectedTab.Text = webComponent.DocumentTitle;
                    labelStatus.Text = "Extrating Data...";
                    Thread.Sleep(2000);

                    setYieldParameters(webComponent);
                    do
                    {
                        Application.DoEvents();
                        Thread.Sleep(1);
                    } while ((bool)((Dictionary<string, object>)webComponent.Tag)["Navigated"] == false);

                    if (!string.IsNullOrEmpty(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString()))
                        throw new Exception(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString());

                    documentTextParser(webComponent.DocumentText);

                    if (!string.IsNullOrEmpty(errorMessage))
                        throw new Exception(errorMessage);

                    webComponent.Dispose();
                }

            }
            catch (Exception error)
            {
                errorMessage = error.Message;
            }

            return errorMessage;

        }
        public string GetYieldThreadSafe(string url, string urlTitle = null)
        {
            labelStatus.Text = "Connecting to MQS System..";
            string errorMessage = string.Empty;

            try
            {
                using (WebBrowser webComponent = new WebBrowser())
                {
                    TabPage tab = new TabPage();
                    metroTabControl1.Controls.Add(tab);
                    metroTabControl1.SelectTab(metroTabControl1.TabCount - 1);
                    webComponent.Parent = tab;
                    webComponent.Dock = DockStyle.Fill;
                    webComponent.Navigating += new WebBrowserNavigatingEventHandler(webpage_Navigating);
                    webComponent.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webpage_DocumentCompleted);
                    webComponent.NewWindow += new CancelEventHandler(webBrowser1_NewWindow);
                    Dictionary<string, object> WebInfos = new Dictionary<string, object>() { { "NavigationError", "" }, { "Navigated", false }, { "URL_Title", urlTitle } };
                    webComponent.Tag = WebInfos;
                    webComponent.Navigate(url);
                    do
                    {
                        Application.DoEvents();
                        Thread.Sleep(1);
                    } while ((bool)((Dictionary<string, object>)webComponent.Tag)["Navigated"] == false);

                    if (!string.IsNullOrEmpty(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString()))
                        throw new Exception(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString());
                    labelStatus.Text = "Connected!!!";
                    errorMessage = StartBrowser(webComponent, url, urlTitle);

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        Thread.Sleep(1000);
                        errorMessage = StartBrowser(webComponent, url, urlTitle);
                    }
                    //metroTabControl1.SelectedTab.Text = webComponent.DocumentTitle;
                    navigateYieldTab(webComponent);
                    //New TAB "MQS - Yield Report"
                    webComponent.Navigate(urlPareto);
                    WebInfos = new Dictionary<string, object>() { { "NavigationError", "" }, { "Navigated", false }, { "URL_Title", pareto } };
                    webComponent.Tag = WebInfos;
                    do
                    {
                        Application.DoEvents();
                        Thread.Sleep(1);
                    } while ((bool)((Dictionary<string, object>)webComponent.Tag)["Navigated"] == false);

                    if (!string.IsNullOrEmpty(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString()))
                        throw new Exception(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString());

                    //metroTabControl1.SelectedTab.Text = webComponent.DocumentTitle;
                    labelStatus.Text = "Extrating Data...";
                    Thread.Sleep(2000);
                    setYieldParameters(webComponent);
                    do
                    {
                        Application.DoEvents();
                        Thread.Sleep(1);
                    } while ((bool)((Dictionary<string, object>)webComponent.Tag)["Navigated"] == false);

                    if (!string.IsNullOrEmpty(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString()))
                        throw new Exception(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString());

                    //Report Page
                    documentTextParser(webComponent.DocumentText);

                    if (!string.IsNullOrEmpty(errorMessage))
                        throw new Exception(errorMessage);

                    webComponent.Dispose();
                }
            }
            catch (Exception error)
            {
                errorMessage = error.Message;
            }
            return errorMessage;
        }

        public void loginMQS(WebBrowser webComponent)
        {

            strAction = "EnterLogin";
            HtmlElement UserElement = webComponent.Document.GetElementById("ctl00_main_txtUserID");
            if (UserElement == null)
                throw new Exception("Cannot find User Name Element");
            UserElement.SetAttribute("value", user);

            HtmlElement PassElement = webComponent.Document.GetElementById("ctl00_main_txtPassword");
            if (PassElement == null)
                throw new Exception("Cannot find Password Element");
            PassElement.SetAttribute("value", password);

            HtmlElement buttonLoginElement = webComponent.Document.GetElementById("ctl00_main_btnLogin");
            if (buttonLoginElement == null)
                throw new Exception("Cannot find button login Element");
            buttonLoginElement.InvokeMember("click");
            Thread.Sleep(2000);

        }
        public void navigateYieldTab(WebBrowser webComponent)
        {
            strAction = "MqsLoad";

            HtmlElement tabMainElement = webComponent.Document.GetElementById("ctl00_main_tabMain_tabReports_tab");

            if (tabMainElement == null)
                throw new Exception("Cannot find tabMain Element");

            tabMainElement.InvokeMember("click");

            HtmlElement tabYieldElement = webComponent.Document.GetElementById("ctl00_main_tabMain_tabReports_HyperLink2");
            if (tabYieldElement == null)
                throw new Exception("Cannot find tabMain Hyperlink Element");

            tabYieldElement.InvokeMember("click");

        }
        public void setYieldParameters(WebBrowser webComponent)
        {

            HtmlElement LocationElement = webComponent.Document.GetElementById("LocationList");
            if (LocationElement == null)
                throw new Exception("Cannot find LocationList Element");

            /* if (comboBoxSites.Text == "MDB_TDF_Newsan")
                 LocationElement.SetAttribute("value", "95");

             else
                 LocationElement.SetAttribute("value", "16"); //MDB_Jaguariuna Default*/

            LocationElement.SetAttribute("value", "83"); //only Newsan site for Javier

            HtmlElement dateT1 = webComponent.Document.GetElementById("Accordion_Normal_content_TextBox1");
            if (dateT1 == null)
                throw new Exception("Cannot find LocationList Element");

            //DateTime today = DateTime.Today;
            dateT1.SetAttribute("value", dateTimePickerStart.Text);

            HtmlElement dateT2 = webComponent.Document.GetElementById("Accordion_Normal_content_TextBox2");
            if (dateT2 == null)
                throw new Exception("Cannot find LocationList Element");
            dateT2.SetAttribute("value", dateTimePickerEnd.Text);


            HtmlElement timeT1 = webComponent.Document.GetElementById("Accordion_Normal_content_TextBox6");
            if (timeT1 == null)
                throw new Exception("Cannot find LocationList Element");
            string hourStart = textBoxStartHour.Text;
            timeT1.SetAttribute("value", hourStart);

            HtmlElement timeT2 = webComponent.Document.GetElementById("Accordion_Normal_content_TextBox7");
            if (timeT2 == null)
                throw new Exception("Cannot find LocationList Element");
            string hourEnd = textBoxEndHour.Text;
            timeT2.SetAttribute("value", hourEnd);

            HtmlElement button3Element = webComponent.Document.GetElementById("Button3");
            if (button3Element == null)
                throw new Exception("Cannot find button3 Element");

            button3Element.InvokeMember("click");
        }
        public string StartBrowser(WebBrowser webComponent, string url, string urlTitle = null)
        {
            string errorMessage = string.Empty;
            try
            {
                if (webComponent == null)
                {
                    webComponent = new WebBrowser();
                    webComponent.Navigating += new WebBrowserNavigatingEventHandler(webpage_Navigating);
                    webComponent.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webpage_DocumentCompleted);
                    webComponent.NewWindow += new CancelEventHandler(webBrowser1_NewWindow);
                    Dictionary<string, object> WebInfos = new Dictionary<string, object>() { { "NavigationError", "" }, { "Navigated", false }, { "URL_Title", "" } };
                    webComponent.Tag = WebInfos;
                }

                ((Dictionary<string, object>)webComponent.Tag)["URL_Title"] = urlTitle;

                strAction = "MqsLoad";

                webComponent.Navigate(url);
                do
                {
                    Application.DoEvents();
                    Thread.Sleep(1);
                } while ((bool)((Dictionary<string, object>)webComponent.Tag)["Navigated"] == false);

                if (!string.IsNullOrEmpty(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString()))
                    throw new Exception(((Dictionary<string, object>)webComponent.Tag)["NavigationError"].ToString());

            }
            catch (Exception error)
            {
                errorMessage = error.Message;
                labelStatus.Text = "Page Error: " + errorMessage;
                //MessageBox.Show("Error: " + errorMessage);
            }
            return errorMessage;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (checkBoxLogin.Checked)
            {
                textBoxUser.Enabled = true;
                textBoxPassword.Enabled = true;
            }
        }

        private void buttonFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBoxSave.Text = folderBrowserDialog1.SelectedPath;
            }
        }
        public void documentTextParser(string documentText)
        {
            documentText = documentText.Substring(documentText.LastIndexOf("border=\"1\" rules=\"all\" cellSpacing=\"0\" cellPadding=\"4\">") + 39);
            documentText = documentText.Replace("<TBODY>", "<HTML><HEAD></HEAD><BODY><FORM><TABLE><TBODY>");
            documentText = documentText.Replace("cellPadding=\"4\">", "");
            string cleanPage = documentText;
            DateTime today = DateTime.Now;
            try
            {
                string pathSaveFile = textBoxSave.Text;
                if (!Directory.Exists(pathSaveFile))
                    Directory.CreateDirectory(pathSaveFile);

                string directoryName = pathSaveFile + "\\DailyMQSData.html";
                using (StreamWriter sw = File.CreateText(directoryName))
                {
                    sw.Write(cleanPage);
                }
                //clean data for csv convertion
                cleanPage = cleanPage.Replace("%PFail / PH", "PFail_PH");
                cleanPage = cleanPage.Replace("%PNtf / PH", "PNtf_PH");
                cleanPage = cleanPage.Replace("%PNtf / PFail", "PNtf_PFail");
                cleanPage = cleanPage.Replace("%TFail / TH", "TFail_TH");
                cleanPage = cleanPage.Replace("%TNtf / TH", "TNtf_TH");
                cleanPage = cleanPage.Replace("%TNtf / TFail", "TNtf_TFail");
                cleanPage = cleanPage.Replace("%", "");
                cleanPage = cleanPage.Replace("5GFR1BDTST", "T5GFR1BDTST");

                directoryName = pathSaveFile + "\\DailyMQSData.xls";
                using (StreamWriter sw = File.CreateText(directoryName))
                {
                    sw.Write(cleanPage);
                }

                conv.htmlToCsv(cleanPage);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex);
            }
        }
    }
}
