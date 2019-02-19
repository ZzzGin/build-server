using System;
using System.Collections.Generic;
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
using PluggableComm;
using Navigator;
using System.IO;
using System.Threading;
using IComm;
using BuildRequest;

namespace P4
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IFileMgr fileMgr { get; set; } = null;  // note: Navigator just uses interface declarations
        Comm comm { get; set; } = null;
        Dictionary<string, Action<CommMessage>> messageDispatcher = new Dictionary<string, Action<CommMessage>>();
        Thread rcvThread = null;
        private List<String> fileSelected = new List<String>();

        public string currentRemotePath { get; set; } = "";
        public Stack<string> remotePathStack { get; set; } = new Stack<string>();

        public MainWindow()
        {
            InitializeComponent();
            initializeEnvironment();
            string uses = "1. in \"File\" tab, you can select one file in \"local files\" and click on \"Upload\" to upload the file to remote repo mock.\n" +
                "2. there are 3 testcases, each of them represent different result:\n" +
                "    -1. ITest.cs TestA.cs TestA_success.cs: build success, test success\n" +
                "    -2. ITest.cs TestB.cs TestB_fail.cs: build success, test fail\n" +
                "    -3. ITest.cs TestC.cs TestC_BuildFail.cs: build fail\n" +
                "3. You can double click on the file in \"remote files\" and select it to \"selected files\", then you can click on \"buildRQ\" to build a buildrequest in repo\n" +
                "4. in \"BuildRequest\" tab, select a buildrequest (here there is 3 b.r. made from the above testcases ) and click on \"build\" and wait for the result\n";
            UsesTextBox.Text = uses;
            fileMgr = FileMgrFactory.create(FileMgrType.Local); // uses Environment
            getTopFiles();
            comm = new Comm(BuildEnvironment.ClientEnvironment.address, BuildEnvironment.ClientEnvironment.port);
            initializeMessageDispatcher();
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
        }
        //----< define how to process each message command >-------------

        void initializeMessageDispatcher()
        {
            // load remoteFiles listbox with files from root

            messageDispatcher["getTopFiles"] = (CommMessage msg) =>
            {
                remoteFilesListBox.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    remoteFilesListBox.Items.Add(file);
                }
            };
            // load remoteDirs listbox with dirs from root

            messageDispatcher["getTopDirs"] = (CommMessage msg) =>
            {
                remoteDirsListBox.Items.Clear();
                foreach (string dir in msg.arguments)
                {
                    remoteDirsListBox.Items.Add(dir);
                }
            };
            // load remoteFiles listbox with files from folder

            messageDispatcher["moveIntoFolderFiles"] = (CommMessage msg) =>
            {
                remoteFilesListBox.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    remoteFilesListBox.Items.Add(file);
                }
            };
            // load remoteDirs listbox with dirs from folder

            messageDispatcher["moveIntoFolderDirs"] = (CommMessage msg) =>
            {
                remoteDirsListBox.Items.Clear();
                foreach (string dir in msg.arguments)
                {
                    remoteDirsListBox.Items.Add(dir);
                }
            };

            messageDispatcher["getBRFiles"] = (CommMessage msg) =>
            {
                BuildRequestListBox.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    string filename = System.IO.Path.GetFileName(file);
                    BuildRequestListBox.Items.Add(filename);
                }
            };

            messageDispatcher["buildResult"] = (CommMessage msg) =>
            {
                string buildresult = msg.arguments[0] + " - " + msg.arguments[1];
                BuildResultListBox.Items.Insert(0, buildresult);
            };

            messageDispatcher["testResult"] = (CommMessage msg) =>
            {
                TestResultListBox.Items.Insert(0, msg.arguments[0]+" - Test: " + msg.arguments[1]);
            };
        }

        //----< define processing for GUI's receive thread >-------------

        void rcvThreadProc()
        {
            Console.Write("\n  starting client's receive thread");
            while (true)
            {
                CommMessage msg = comm.getMessage();
                msg.show();
                if (msg.command == null)
                    continue;

                // pass the Dispatcher's action value to the main thread for execution

                Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });
            }
        }

        //----< make Environment equivalent to ClientEnvironment >-------
        void initializeEnvironment()
        {
            BuildEnvironment.Environment.root = BuildEnvironment.ClientEnvironment.root;
            BuildEnvironment.Environment.address = BuildEnvironment.ClientEnvironment.address;
            BuildEnvironment.Environment.port = BuildEnvironment.ClientEnvironment.port;
            BuildEnvironment.Environment.endPoint = BuildEnvironment.ClientEnvironment.endPoint;
        }

        //----< shut down comm when the main window closes >-------------

        private void Window_Closed(object sender, EventArgs e)
        {
            comm.close();

            // The step below should not be nessary, but I've apparently caused a closing event to 
            // hang by manually renaming packages instead of getting Visual Studio to rename them.

            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        //----< move to up dir >-----------------------
        private void localUp_Click(object sender, RoutedEventArgs e)
        {
            if (fileMgr.currentPath == "")
                return;
            fileMgr.currentPath = fileMgr.pathStack.Peek();
            fileMgr.pathStack.Pop();
            getTopFiles();
        }

        //----< move to top dir >-----------------------
        private void localTop_Click(object sender, RoutedEventArgs e)
        {
            fileMgr.currentPath = "";
            getTopFiles();
        }

        //----< show selected file in code popup window >----------------
        private void localFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string fileName = localFiles.SelectedValue as string;
            try
            {
                string path = System.IO.Path.Combine(BuildEnvironment.ClientEnvironment.root, fileName);
                string contents = File.ReadAllText(path);
                CodePopUp popup = new CodePopUp();
                popup.codeView.Text = contents;
                popup.Show();
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        //----< move into subdir and show files and subdirs >------------
        private void localDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string dirName = localDirs.SelectedValue as string;
            fileMgr.pathStack.Push(fileMgr.currentPath);
            fileMgr.currentPath = dirName;
            getTopFiles();
        }

        //----< move to root of remote directories >---------------------
        /*
         * - sends a message to server to get files from root
         * - recv thread will create an Action<CommMessage> for the UI thread
         *   to invoke to load the remoteFiles listbox
         */
        private void RemoteTop_Click(object sender, RoutedEventArgs e)
        {
            currentRemotePath = "";
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = BuildEnvironment.ClientEnvironment.endPoint;
            msg1.to = BuildEnvironment.ServerEnvironment.endPoint;
            msg1.author = "JQ_Client";
            msg1.command = "getTopFiles";
            msg1.arguments.Add("");
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "getTopDirs";
            comm.postMessage(msg2);
        }

        //----< move into remote subdir and display files and subdirs >--
        /*
         * - sends messages to server to get files and dirs from folder
         * - recv thread will create Action<CommMessage>s for the UI thread
         *   to invoke to load the remoteFiles and remoteDirs listboxs
         */
        private void remoteDirs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string remoteDirName = remoteDirsListBox.SelectedValue as string;
            remotePathStack.Push(currentRemotePath);
            currentRemotePath = remoteDirName;
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = BuildEnvironment.ClientEnvironment.endPoint;
            msg1.to = BuildEnvironment.ServerEnvironment.endPoint;
            msg1.command = "moveIntoFolderFiles";
            msg1.arguments.Add(remoteDirsListBox.SelectedValue as string);
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "moveIntoFolderDirs";
            comm.postMessage(msg2);
        }
        //----< move to parent directory of current remote path >--------
        private void RemoteUp_Click(object sender, RoutedEventArgs e)
        {
            if (currentRemotePath == "")
                return;
            currentRemotePath = remotePathStack.Peek();
            remotePathStack.Pop();
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = BuildEnvironment.ClientEnvironment.endPoint;
            msg1.to = BuildEnvironment.ServerEnvironment.endPoint;
            msg1.command = "moveIntoFolderFiles";
            msg1.arguments.Add(currentRemotePath);
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "moveIntoFolderDirs";
            comm.postMessage(msg2);
        }

        //----< select a file in repo and add it to SelectedFileListBox>--------
        private void RemoteFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string selectedFileName = remoteFilesListBox.SelectedValue as string;
            foreach(String file in SelectedFilesListBox.Items)
            {
                if (file == selectedFileName) return;
            }
            fileSelected.Add(selectedFileName);
            SelectedFilesListBox.Items.Clear();
            foreach(String file in fileSelected)
            {
                SelectedFilesListBox.Items.Add(file);
            }
        }

        //----< upload file to repo server>-----------------------
        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            string fileName = localFiles.SelectedValue as string;
            fileName = System.IO.Path.Combine(BuildEnvironment.ClientEnvironment.root, fileName);
            fileName = System.IO.Path.GetFileName(fileName);
            //fileName = System.IO.Path.GetFullPath(fileName);
            bool transferSuccess = comm.postFile("../../../ClientFiles", "../../../ServerFiles/", fileName);
            currentRemotePath = "";
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = BuildEnvironment.ClientEnvironment.endPoint;
            msg1.to = BuildEnvironment.ServerEnvironment.endPoint;
            msg1.author = "JQ_Client";
            msg1.command = "getTopFiles";
            msg1.arguments.Add("");
            comm.postMessage(msg1);
            CommMessage msg2 = msg1.clone();
            msg2.command = "getTopDirs";
            comm.postMessage(msg2);
        }

        //----< build the selected buildrequest>-----------------------
        private void Build_Click(object sender, RoutedEventArgs e)
        {
            string BRSelected = BuildRequestListBox.SelectedValue as string;
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = BuildEnvironment.ClientEnvironment.endPoint;
            msg1.to = "http://localhost:5270/IPluggableComm";
            msg1.author = "JQ_Client";
            msg1.command = "BuildRequest";
            msg1.arguments.Add(BRSelected);
            comm.postMessage(msg1);
        }

        //----< make a build request and save in >-----------------------
        private void BuildRQ_Click(object sender, RoutedEventArgs e)
        {
            BuildRequest.BuildRequest br = new BuildRequest.BuildRequest();
            DateTime time = DateTime.UtcNow;
            int year = time.Year;
            int month = time.Month;
            int day = time.Day;
            int hour = time.Hour;
            int min = time.Minute;
            int sec = time.Second;
            int ms = time.Millisecond;
            string brID = year.ToString("D4")+ month.ToString("D2")+
                day.ToString("D2")+hour.ToString("D2")+min.ToString("D2")+sec.ToString("D2")+ms.ToString("D3");
            br.requestID = brID;
            br.environment = "cs";
            br.testDir = brID + "/";
            foreach (String file in SelectedFilesListBox.Items)
            {
                br.testedFiles.Add(file);
            }
            br.wrap();
            br.saveXML("../../../ClientFiles/BuildRequest/" + brID + ".xml");

            // send the buildRequest .xml file to the repo
            string fileName = brID + ".xml";
            bool transferSuccess = comm.postFile("../../../ClientFiles/BuildRequest/", "../../../ServerFiles/", fileName);

            // send buildrequest to repo

        }


        //----< delete a selected file when double click in the list box >-----------------------
        private void SelectFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string selectedFileName = SelectedFilesListBox.SelectedValue as string;
            for (int i = fileSelected.Count - 1; i >= 0; --i)
            {
                if (fileSelected[i] == selectedFileName)
                    fileSelected.Remove(fileSelected[i]);
            }
            SelectedFilesListBox.Items.Clear();
            foreach (String file in fileSelected)
            {
                SelectedFilesListBox.Items.Add(file);
            }
        }

        private void BRRefresh_Click(object sender, RoutedEventArgs e)
        {
            CommMessage msg1 = new CommMessage(CommMessage.MessageType.request);
            msg1.from = BuildEnvironment.ClientEnvironment.endPoint;
            msg1.to = BuildEnvironment.ServerEnvironment.endPoint;
            msg1.author = "JQ_Client";
            msg1.command = "getBRFiles";
            msg1.arguments.Add("");
            comm.postMessage(msg1);

        }


        //----< show files and dirs in root path >-----------------------

        public void getTopFiles()
        {
            List<string> files = fileMgr.getFiles().ToList<string>();
            localFiles.Items.Clear();
            foreach (string file in files)
            {
                localFiles.Items.Add(file);
            }
            List<string> dirs = fileMgr.getDirs().ToList<string>();
            localDirs.Items.Clear();
            foreach (string dir in dirs)
            {
                localDirs.Items.Add(dir);
            }
        }


    }
}
