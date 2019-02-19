//////////////////////////////////////////////////////////////////////////////// ChildBuildProcess.cs                                                     //// ver 1.0                                                                  ////                                                                          //// Author: Jing Qi, zzzgin@hotmail.com || jqi101@syr.edu                    //// Application: CSE681 Project 4-FChildBuildProcess.cs                      //// Environment: C# console                                                  /////////////////////////////////////////////////////////////////////////////////* * Package Operations: * =================== * this is an independent application being called as an exe application.* * Public Interface * ---------------- * main - start the independent allication* . * .  *  * Required Files: * --------------- * BuildServer.cs* ConsoleLogger.cs* CoreBuilder.cs* IComm.cs* PluggableComm.cs*** Maintenance History: * -------------------- * ver 1.0 : 06 Dec 2017 * - first release *  */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IComm;
using PluggableComm;
using System.Threading;
using System.IO;
using BuildRequest;
using CoreBuilder;
using TestRequest;

namespace ChildBuildProcess
{
    public class Builder
    {
        public string address { get; set; }
        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher = new Dictionary<string, Func<CommMessage, CommMessage>>();
        public bool ProcessCommandLineArgs(string[] args)
        {
            if (args.Count() == 0) return false;
            address = args[0];
            return true;
        }


        // Main entrance
        static void Main(string[] args)
        {
            Builder ChildBuilder = new Builder();

            if (ChildBuilder.ProcessCommandLineArgs(args))
            {
                Console.Write("\n  ChildBuildProcess Start Up. Address: {0}\n", ChildBuilder.address);
            }
            else
                Console.Write("\n  No Command Line Arguments..\n");

            Comm comm = new Comm("http://localhost", int.Parse(args[0]));
            CommMessage crcvMessage = comm.getMessage();
            crcvMessage.show();
            //ChildBuilder.address = crcvMessage.to;
            while (true)
            {
                crcvMessage = comm.getMessage();

                // TODO: Change switch-case to Fun()
                switch (crcvMessage.command)
                {
                    case "show":
                        crcvMessage.show();
                        break;
                    case "test":
                        crcvMessage.show();
                        "Connection from MotherBuilder to this ChildBuilder is OK.".green();
                        "Sending testback...".yellow();
                        CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
                        csndMsg.command = "testback";
                        csndMsg.author = "Jing Qi";
                        csndMsg.to = crcvMessage.from;
                        csndMsg.from = crcvMessage.to;
                        comm.postMessage(csndMsg);
                        break;
                    case "testdone":
                        "Connection test success. ChildBuilder is Ready.".green();
                        CommMessage csndMsg_ready = new CommMessage(CommMessage.MessageType.request);
                        csndMsg_ready.command = "ready";
                        csndMsg_ready.author = "Jing Qi";
                        csndMsg_ready.to = crcvMessage.from;
                        csndMsg_ready.from = crcvMessage.to;
                        comm.postMessage(csndMsg_ready);
                        break;
                    case "build":
                        // TODO: NOW YOU GET THE BUILD REQUEST, NOW BUILD
                        "Build Request Received.".yellow();
                        crcvMessage.show();
                        //Thread.Sleep(3000); // pretend to be building


                        string BuildTempFileDir = "../../../BuilderStorage/";
                        BuildTempFileDir += System.IO.Path.GetFileNameWithoutExtension(crcvMessage.arguments[0]);
                        if (!System.IO.Directory.Exists(BuildTempFileDir))
                            System.IO.Directory.CreateDirectory(BuildTempFileDir);
                        // gatherFiles
                        CommMessage needFileRequest = new CommMessage(CommMessage.MessageType.request);
                        needFileRequest.command = "needXML";
                        needFileRequest.author = "Jing Qi";
                        needFileRequest.to = "http://localhost:5260/IPluggableComm";
                        needFileRequest.from = crcvMessage.to;
                        needFileRequest.arguments.Add(crcvMessage.arguments[0]);
                        needFileRequest.arguments.Add(BuildTempFileDir);
                        comm.postMessage(needFileRequest);

                        Thread.Sleep(1000);
                        "  --xml files gathered.".green();

                        List<string> files = new List<string>();
                        files = Directory.GetFiles(BuildTempFileDir, "*.xml").ToList<string>();
                        string BuildRequestName = files[0];

                        BuildRequest.BuildRequest br = new BuildRequest.BuildRequest();
                        br.unwrap(BuildTempFileDir +"/"+ System.IO.Path.GetFileName(BuildRequestName), false);
                        List<string> testfiles = br.testedFiles;
                        foreach(string testfile in testfiles)
                        {
                            CommMessage needTestFileRequest = new CommMessage(CommMessage.MessageType.request);
                            needTestFileRequest.command = "needXML";
                            needTestFileRequest.author = "Jing Qi";
                            needTestFileRequest.to = "http://localhost:5260/IPluggableComm";
                            needTestFileRequest.from = crcvMessage.to;
                            needTestFileRequest.arguments.Add(testfile);
                            needTestFileRequest.arguments.Add(BuildTempFileDir);
                            comm.postMessage(needTestFileRequest);
                        }
                        Thread.Sleep(1000);
                        "  --all files gathered.".green();
                        CoreBuilder_ICC Builder = new CoreBuilder_ICC(br);
                        bool buildResult = Builder.build();
                        if (buildResult)
                        {
                            "Build success!...".green();
                            // send log to repo
                            comm.postFile(BuildTempFileDir, "../../../ServerFiles/BuildLog/", "BuildLog" + crcvMessage.arguments[0]);
                            // send a message to client
                            CommMessage message2Client = new CommMessage(CommMessage.MessageType.request);
                            message2Client.command = "buildResult";
                            message2Client.author = "Jing Qi";
                            message2Client.to = "http://localhost:5261/IPluggableComm";
                            message2Client.from = crcvMessage.to;
                            message2Client.arguments.Add(br.requestID);
                            message2Client.arguments.Add("Success");
                            comm.postMessage(message2Client);

                            // make a test request
                            TestRequest.TestRequest tr = new TestRequest.TestRequest();
                            tr.requestID = br.requestID;
                            tr.testDir = BuildTempFileDir;
                            tr.testedFiles.Add("Out.dll");
                            tr.wrap();
                            tr.saveXML(BuildTempFileDir + "/TestRequest" + br.requestID + ".xml");

                            // send test request
                            CommMessage message2TH = new CommMessage(CommMessage.MessageType.request);
                            message2TH.command = "testRequest";
                            message2TH.author = "Jing Qi";
                            message2TH.to = "http://localhost:5262/IPluggableComm";
                            message2TH.from = crcvMessage.to;
                            message2TH.arguments.Add(br.requestID);
                            message2TH.arguments.Add(BuildTempFileDir);
                            comm.postMessage(message2TH);
                        }
                        else
                        {
                            "Build fail!...".red();
                            // send log to repo
                            comm.postFile(BuildTempFileDir, "../../../ServerFiles/BuildLog/", "BuildLog" + crcvMessage.arguments[0]);

                            // send a message to client
                            CommMessage message2Client = new CommMessage(CommMessage.MessageType.request);
                            message2Client.command = "buildResult";
                            message2Client.author = "Jing Qi";
                            message2Client.to = "http://localhost:5261/IPluggableComm";
                            message2Client.from = crcvMessage.to;
                            message2Client.arguments.Add(br.requestID);
                            message2Client.arguments.Add("Fail");
                            comm.postMessage(message2Client);



                        }
                        //for (int i = 0; i < 10; i++)
                        //{
                        //    Console.Write("\nbuild process:{0}", i * 10);
                        //    Thread.Sleep(300);
                        //}
                        //"BUILT SUCCESS!".green();




                        CommMessage csndMsg_Buildready = new CommMessage(CommMessage.MessageType.request);
                        csndMsg_Buildready.command = "ready";
                        csndMsg_Buildready.author = "Jing Qi";
                        csndMsg_Buildready.to = crcvMessage.from;
                        csndMsg_Buildready.from = crcvMessage.to;
                        comm.postMessage(csndMsg_Buildready);
                        break;
                    case "quit":
                        crcvMessage.show();
                        comm.close();
                        "Closed.".red();
                        return;
                }
            }
        }
    }
}
