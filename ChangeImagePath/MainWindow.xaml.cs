using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Net;

namespace ChangeImagePath
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<string> documentsSelected = new List<string>();
        public string selectedFolder;

        private LogWriter logWriter;

        private bool backupDocument;

        public bool BackupDocument
        {
            get { return backupDocument; }
            set { backupDocument = value; }
        }

        public MainWindow()
        {
            InitializeComponent();

            logWriter = new LogWriter(Directory.GetCurrentDirectory());

            BackupDocument = true;

            this.DataContext = this;
        }

        private void OnSelectFiles(object sender, RoutedEventArgs e)
        {

            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "XML Files|*.xml";
            dialog.Multiselect = true;
            dialog.InitialDirectory = Directory.GetCurrentDirectory();

            if (dialog.ShowDialog() == true)
            {
                documentsSelected = new List<string>(dialog.FileNames);
            }
        }

        private void OnSelectFolder(object sender, RoutedEventArgs e)
        {
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog folderBrowserDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == true)
            {
                selectedFolder = folderBrowserDialog.SelectedPath;
            }
        }

        private void OnReplace(object sender, RoutedEventArgs e)
        {
            //if (File.Exists(@"D:\Data\Environments\Lab\Resources\Documents\Random\Test.xml.bak"))
            //{
            //    File.Delete(@"D:\Data\Environments\Lab\Resources\Documents\Random\Test.xml");
            //    File.Copy(@"D:\Data\Environments\Lab\Resources\Documents\Random\Test.xml.bak", @"D:\Data\Environments\Lab\Resources\Documents\Random\Test.xml");
            //}

            //documentsSelected.Add(@"D:\Data\Environments\Lab\Resources\Documents\Random\Test.xml");

            WaitBox.Visibility = Visibility.Visible;

            if (documentsSelected.Count > 0)
            {

                GoThroughDocuments(documentsSelected.ToArray());

            }
            else
            {
                if (Directory.Exists(selectedFolder) == true)
                {
                    List<DirectoryInfo> directoriesCache = new List<DirectoryInfo>() { new DirectoryInfo(selectedFolder) };

                    while (directoriesCache.Count > 0)
                    {
                        List<string> paths = new List<string>();

                        foreach (FileInfo file in directoriesCache[0].GetFiles())
                        {
                            paths.Add(file.FullName);
                        }

                        if (paths.Count > 0)
                        {
                            GoThroughDocuments(paths.ToArray());
                        }

                        directoriesCache.AddRange(directoriesCache[0].GetDirectories());

                        directoriesCache.RemoveAt(0);
                    }
                }
            }

            WaitBox.Text = "All Done";
        }


        public void GoThroughDocuments(string[] documents)
        {


            XmlDocument xmlDocument = new XmlDocument();

            foreach (string path in documents)
            {
                
                if (File.Exists(path) == true)
                {
                    if (path.Contains(".xml"))
                    {

                        try
                        {
                            xmlDocument.Load(path);

                            XmlNode eventActionsNode = null;

                            foreach (XmlNode childNode in xmlDocument.ChildNodes)
                            {
                                if (childNode.Name == "document")
                                {
                                    bool variableFound = false;

                                    for (int i = 0; i < childNode.ChildNodes.Count && i > -1; i++)
                                    {
                                        XmlNode docChild = childNode.ChildNodes[i];

                                        if (docChild.Name == "variables")
                                        {
                                            foreach (XmlNode variable in docChild.ChildNodes)
                                            {
                                                XmlAttributeCollection attributes = variable.Attributes;

                                                //string varId = variable.Attributes["id"].Value;
                                                string varName = variable.Attributes["name"].Value;

                                                if (attributes["dataType"] != null && attributes["dataType"].Value == "image")
                                                {
                                                    if (attributes["imageFromPulldown"].Value == "true")
                                                    {
                                                        //attributes["imagePulldownDirectory"].Value = "%UploadDirectory%";
                                                        if (attributes["imagePulldownDirectoryVariable"] == null)
                                                        {
                                                            XmlAttribute xmlAttribute = xmlDocument.CreateAttribute("imagePulldownDirectoryVariable");
                                                            xmlAttribute.Value = "upload-variable";
                                                            attributes.Append(xmlAttribute);
                                                        }
                                                        else
                                                        {
                                                            attributes["imagePulldownDirectoryVariable"].Value = "upload-variable";
                                                        }
                                                    }
                                                   
                                                }

                                                if (varName == "UploadDirectory")
                                                {
                                                    variableFound = true;
                                                }
                                            }

                                            if (variableFound == false)
                                            {
                                                XmlDocument newVarDoc = new XmlDocument();

                                                newVarDoc.Load(Directory.GetCurrentDirectory() + "\\VariableXml.xml");

                                                XmlNode xmlNode = xmlDocument.ImportNode(newVarDoc.FirstChild, false);


                                                docChild.AppendChild(xmlNode);
                                            }
                                        }

                                        if (docChild.Name == "eventActions")
                                        {
                                            eventActionsNode = docChild;
                                        }
                                    }

                                    

                                    if (eventActionsNode == null)
                                    {
                                        eventActionsNode = xmlDocument.CreateElement("eventActions");
                                        childNode.AppendChild(eventActionsNode);
                                    }

                                    XmlNode actionItem = null;

                                    if (eventActionsNode.HasChildNodes)
                                    {
                                        actionItem = eventActionsNode.SelectSingleNode("//item[id='sean-action']");
                                    }

                                    if (actionItem == null)
                                    {
                                        XmlElement actionItemEl = xmlDocument.CreateElement("item");
                                        
                                        actionItemEl.SetAttribute("id", "sean-action");
                                        actionItemEl.SetAttribute("name", "SetUploadDirectory");
                                        actionItemEl.SetAttribute("asynchronous", "false");
                                        actionItemEl.SetAttribute("disabled", "false");
                                        actionItemEl.SetAttribute("eventNames", ";DocumentFullyLoaded;");
                                        actionItemEl.SetAttribute("executionDelay", "50");
                                        actionItemEl.SetAttribute("executionType", "after");

                                        actionItem = actionItemEl;

                                        eventActionsNode.AppendChild(actionItem);
                                        
                                    }

                                    if (!actionItem.HasChildNodes)
                                    {
                                        XmlElement actionActionEl = xmlDocument.CreateElement("action");
                                        actionActionEl.SetAttribute("notes", "");
                                        actionActionEl.SetAttribute("xml", "&lt;action returnType=&quot;&quot;&gt;&lt;line number=&quot;1&quot; type=&quot;execute&quot; execute1=&quot;JavaScript&quot; execute2=&quot;eval&quot; execute_arg_script1=&quot;long string&quot; execute_arg_scriptInput=&quot;&quot;/&gt;&lt;/action&gt;");

                                        actionItem.AppendChild(actionActionEl);
                                    }

                                    string newJavaScript = File.ReadAllText(Directory.GetCurrentDirectory() + "\\jsCompressed.js");

                                    XmlDocument actionDoc = new XmlDocument();
                                    actionDoc.LoadXml(WebUtility.HtmlDecode(actionItem.FirstChild.Attributes["xml"].Value));

                                    foreach (XmlNode actionItemChild in actionDoc.FirstChild.ChildNodes)
                                    {
                                        if (actionItemChild.Attributes["execute1"] != null)
                                        {
                                            if (actionItemChild.Attributes["execute1"].Value == "JavaScript")
                                            {
                                                actionItemChild.Attributes["execute_arg_scriptInput"].Value = newJavaScript;
                                                actionItem.FirstChild.Attributes["xml"].Value = actionDoc.OuterXml;
                                            }
                                        }
                                    }
                                    
                                }
                            }
                        }
                        catch (Exception except)
                        {
                            logWriter.WriteLog(except);
                        }


                        if (backupDocument == true)
                        {
                            if (File.Exists(path + ".bak") == false)
                            {
                                File.Copy(path, path + ".bak", false);
                            }
                        }

                        //logWriter.WriteLog(xmlDocument.OuterXml);

                        xmlDocument.Save(path);



                    }
                }

            }
        }
        
    }
}
