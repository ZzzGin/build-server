//////////////////////////////////////////////////////////////////////////////
// MotherBuildProcess.cs - the main buidler                                 //
// ver 1.0                                                                  //
//                                                                          //
// Author: Jing Qi, zzzgin@hotmail.com                                      //
// Application: CSE681 Project 4-File MotherBuildProcess.cs                 //
// Environment: C# console                                                  //
//////////////////////////////////////////////////////////////////////////////
/* 
* Package Operations: 
* =================== 
* this is the main function entrance which works as a mother builder 
* 
* Public Interface 
* ---------------- 
* initializeDispatcher - init the message dispatcher
* processCmd - process the input 
* startChildProcesses - start a specefic num of child processes
* closeAll - close all the child processes
* setProcessNum - a function which can change the child process num
* closeSpecificProcess - close the specefic num child process
* startRolling - start the "main" as a class
* main - work as a independent application
 
*  
* Required Files: 
* --------------- 
* ChildBuildProcess.cs
* BuildReqeust.cs
* IComm.cs
* PluggableComm.cs
* 
*  
* Maintenance History: 
* -------------------- 
* ver 1.0 : 06 Dec 2017 
* - first release 
*  
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using IComm;
using ProcessPool;
using PluggableComm;
using System.Threading;
using System.Windows;


namespace BuildServer
{
    public class MotherBuildProcess
    {
        public string HostAddress { get; private set; }
        public int NoChildProcesses { get; private set; } = 1;

        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher = new Dictionary<string, Func<CommMessage, CommMessage>>();

        public RdRqBlockingQueue<string, string> processPool = new RdRqBlockingQueue<string, string>();

        public Comm comm = new Comm("http://localhost", 5270);

        // the function of receiving message from child
        void initializeDispatcher()
        {
            Func<CommMessage, CommMessage> testback = (CommMessage msg) =>
            {
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "testdone";
                //reply.arguments = localFileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["testback"] = testback;

            //when receiving "ready", the mother would insert the port number into process pool's ready pool.
            Func<CommMessage, CommMessage> ready = (CommMessage msg) =>
            {
                StringBuilder outLog_ready = new StringBuilder("");
                outLog_ready.AppendFormat("--{0} is now ready.", msg.from);
                Console.Write(outLog_ready);
                // push it into ReadyPool
                processPool.enrdQ(msg.from);
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "show";
                List<string> arguments = new List<string>();
                arguments.Add("This ChildBuildProcess is added to the process pool.");
                reply.arguments = arguments;
                return reply;
            };
            messageDispatcher["ready"] = ready;

            Func<CommMessage, CommMessage> BuildRequest = (CommMessage msg) =>
            {
                processPool.enrqQ(msg.arguments[0]);
                CommMessage BR2Child = new CommMessage(CommMessage.MessageType.reply);
                BR2Child.to = msg.from;
                BR2Child.from = msg.to;
                BR2Child.command = "";
                return BR2Child;
            };
            messageDispatcher["BuildRequest"] = BuildRequest;

            Func<CommMessage, CommMessage> getTestFiles = (CommMessage msg) =>
            {
                string testID = msg.arguments[0];
                string buildDir = "../../../BuilderStorage/" + testID + "/";
                comm.postFile(buildDir, "../../../TestStorage/" + testID + "/", "TestRequest" + testID + ".xml");
                comm.postFile(buildDir, "../../../TestStorage/" + testID + "/", "Out.dll");
                CommMessage BR2TH = new CommMessage(CommMessage.MessageType.reply);
                BR2TH.to = msg.from;
                BR2TH.from = msg.to;
                BR2TH.command = "testFileOK";
                BR2TH.arguments.Add(testID);
                return BR2TH;
            };
            messageDispatcher["getTestFiles"] = getTestFiles;
        }


        public bool processCmd(string[] args)
        {
            if (args.Count() < 2) return false;
            HostAddress = args[0];
            int a = 0;
            int.TryParse(args[1], out a);
            if (a > 0) NoChildProcesses = a;
            return true;
        }

        // init function
        public void startChildProcesses()
        {
            for (int i = 0; i < NoChildProcesses; i++)
            {
                Console.Write("\n  Starting process {0}", i);
                Process.Start("ChildBuildProcess", (5270 + (i + 1)).ToString());
            }
        }

        // close all the children
        public void closeAll()
        {
            for (int i = 0; i < NoChildProcesses; ++i)
            {
                CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
                csndMsg.command = "quit";
                csndMsg.author = "Jing Qi";
                csndMsg.to = "http://localhost:527" + (i + 1).ToString() + "/IPluggableComm";
                csndMsg.from = "http://localhost:5270/IPluggableComm";
                comm.postMessage(csndMsg);
            }
            comm.close();
        }

        // construct: init I number of children
        public MotherBuildProcess(int i)
        {
            NoChildProcesses = i;
            startChildProcesses();
        }

        // the NOChildPRocesses is a private, to modify this, i make a function.
        public void setProcessNum(int i)
        {
            NoChildProcesses = i;
        }

        // constructor
        public MotherBuildProcess() { }

        // Close the #i child
        public void closeSpecificProcess(int i)
        {
            CommMessage csndMsg = new CommMessage(CommMessage.MessageType.reply);
            csndMsg.command = "quit";
            csndMsg.author = "Jing Qi";
            csndMsg.to = "http://localhost:527" + (i).ToString() + "/IPluggableComm";
            csndMsg.from = "http://localhost:5270/IPluggableComm";
            comm.postMessage(csndMsg);
        }

        // similar with main(). when calling this function, the motherBUildProcess would 
        // run, waiting for message from children.
        public void startRolling()
        {
            initializeDispatcher();
            for (int i = 0; i < NoChildProcesses; ++i)
            {
                CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
                csndMsg.command = "test";
                csndMsg.author = "Jing Qi";
                csndMsg.to = "http://localhost:526" + (i + 1).ToString() + "/IPluggableComm";
                csndMsg.from = "http://localhost:5260/IPluggableComm";
                comm.postMessage(csndMsg);
            }

            Thread messageDispatching = new Thread(() =>
            {
                bool keepRolling_messageDispatch = true;
                while (keepRolling_messageDispatch)
                {
                    CommMessage msg = comm.getMessage();
                    if (msg.type == CommMessage.MessageType.closeReceiver)
                        break;
                    msg.show();
                    if (msg.command == null)
                        continue;
                    CommMessage reply = messageDispatcher[msg.command](msg);
                    //reply.show();
                    comm.postMessage(reply);
                }
            });

            Thread deQueueing = new Thread(() =>
            {
                bool keepRolling_deQueue = true;
                while (keepRolling_deQueue)
                {
                    string request = "null";
                    string ready = "null";
                    processPool.deQ(out request, out ready);
                    Console.Write("\nProcess " + request + " is now send to " + ready);
                    CommMessage csndMsg_build = new CommMessage(CommMessage.MessageType.request);
                    csndMsg_build.command = "build";
                    csndMsg_build.author = "Jing Qi";
                    List<string> arguments = new List<string>();
                    arguments.Add(request);
                    csndMsg_build.arguments = arguments;
                    csndMsg_build.to = ready;
                    csndMsg_build.from = "http://localhost:5260/IPluggableComm";
                    comm.postMessage(csndMsg_build);
                }
            });
            //receiving.Start();
            deQueueing.Start();
            messageDispatching.Start();


            // Now test. if it is correct, the first 4 processes would be 
            // dequeued. 
            // 
            //Thread.Sleep(1000);
            //processPool.enrqQ("1111");
            //Thread.Sleep(1000);
            //processPool.enrqQ("2222");
            //Thread.Sleep(1000);
            //processPool.enrqQ("3333");
            //Thread.Sleep(1000);
            //processPool.enrqQ("4444");
            //Thread.Sleep(1000);
            //processPool.enrqQ("5555");
            //Thread.Sleep(1000);
        }


        static void Main(string[] args)
        {
            MotherBuildProcess motherBuildProcess = new MotherBuildProcess();
            motherBuildProcess.initializeDispatcher();

            if (motherBuildProcess.processCmd(args))
                motherBuildProcess.startChildProcesses();

            for (int i = 0; i < motherBuildProcess.NoChildProcesses; ++i)
            {
                CommMessage csndMsg = new CommMessage(CommMessage.MessageType.request);
                csndMsg.command = "test";
                csndMsg.author = "Jing Qi";
                csndMsg.to = "http://localhost:527" + (i + 1).ToString() + "/IPluggableComm";
                csndMsg.from = "http://localhost:5270/IPluggableComm";
                motherBuildProcess.comm.postMessage(csndMsg);
            }

            Thread messageDispatching = new Thread(() =>
            {
                bool keepRolling_messageDispatch = true;
                while (keepRolling_messageDispatch)
                {
                    CommMessage msg = motherBuildProcess.comm.getMessage();
                    if (msg.type == CommMessage.MessageType.closeReceiver)
                        break;
                    msg.show();
                    if (msg.command == null)
                        continue;
                    CommMessage reply = motherBuildProcess.messageDispatcher[msg.command](msg);
                    //reply.show();
                    if (reply.command!="")
                        motherBuildProcess.comm.postMessage(reply);
                }
            });
            messageDispatching.Start();
            //receiving.Start();
            bool keepRolling_deQueue = true;
            while (keepRolling_deQueue)
            {
                string request = "null";
                string ready = "null";
                motherBuildProcess.processPool.deQ(out request, out ready);
                Console.Write("\nProcess " + request + " is now send to " + ready);
                CommMessage csndMsg_build = new CommMessage(CommMessage.MessageType.request);
                csndMsg_build.command = "build";
                csndMsg_build.author = "Jing Qi";
                List<string> arguments = new List<string>();
                arguments.Add(request);
                csndMsg_build.arguments = arguments;
                csndMsg_build.to = ready;
                csndMsg_build.from = "http://localhost:5270/IPluggableComm";
                motherBuildProcess.comm.postMessage(csndMsg_build);
            }
      

        }
    }
}
