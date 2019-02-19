//////////////////////////////////////////////////////////////////////////////// TestHarnessMock.cs                                                       //// ver 1.0                                                                  ////                                                                          //// Author: Jing Qi, zzzgin@hotmail.com || jqi101@syr.edu                    //// Application: CSE681 Project 4-File TestHarnessMock.cs                    //// Environment: C# console                                                  /////////////////////////////////////////////////////////////////////////////////* * Package Operations: * =================== * this is a independent testHarnessMock. it has a main entrance in this file.* * Public Interface * ---------------- * initializeDispatcher - init the message Dispatcher* main - the main entrance of this application*    *  * Required Files: * --------------- * FileManager.cs* IComm.cs* Ipluggable.cs* Loader.cs* Logger.cs* PluggableComm.cs* Tester.cs* TestRequest.cs* *** Maintenance History: * -------------------- * ver 1.0 : 06 Dec 2017 * - first release *  */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluggableComm;
using IComm;
using System.Windows;
using System.Windows.Threading;
using TestRequest;
using System.Threading;

namespace TestHarness
{
    class TestHarnessMock
    {
        Comm comm { get; set; } = null;
        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher = new Dictionary<string, Func<CommMessage, CommMessage>>();

        public TestHarnessMock()
        {

        }

        void initializeDispatcher()
        {
            Func<CommMessage, CommMessage> testRequest = (CommMessage msg) =>
            {
                msg.show();
                //create the temp dir
                string TestTempFileDir = "../../../TestStorage/" + msg.arguments[0] + "/";
                if (!System.IO.Directory.Exists(TestTempFileDir))
                    System.IO.Directory.CreateDirectory(TestTempFileDir);
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = "http://localhost:5270/IPluggableComm";
                reply.from = msg.to;
                reply.command = "getTestFiles";
                reply.arguments.Add(msg.arguments[0]);
                return reply;
            };
            messageDispatcher["testRequest"] = testRequest;
            
            Func<CommMessage, CommMessage> testFileOK = (CommMessage msg) =>
            {
                msg.show();
                //do Test
                string TestTempFileDir = "../../../TestStorage/" + msg.arguments[0] + "/";
                TestRequest.TestRequest TR = new TestRequest.TestRequest();
                TR.unwrap(TestTempFileDir + "TestRequest" + msg.arguments[0] + ".xml", false);
                Tester tstr = new Tester();
                Thread t = tstr.SelectConfigAndRun(TestTempFileDir);
                t.Join();
                bool Testresult = tstr.ShowTestResults();
                

                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = "http://localhost:5261/IPluggableComm";
                reply.from = msg.to;
                reply.command = "testResult";
                reply.arguments.Add(msg.arguments[0]);
                reply.arguments.Add(Testresult?"Success":"Fail");
                return reply;
            };
            messageDispatcher["testFileOK"] = testFileOK;
        }

        static void Main(string[] args)
        {
            TestHarnessMock THM = new TestHarnessMock();
            THM.comm = new Comm("http://localhost", 5262);
            THM.initializeDispatcher();

            Console.Write("\n  starting test harness at: http://localhost:5262");
            while (true)
            {
                CommMessage msg = THM.comm.getMessage();
                msg.show();
                if (msg.command == null)
                    continue;
                
                CommMessage reply = THM.messageDispatcher[msg.command](msg);
                if (reply.command != "")
                    THM.comm.postMessage(reply);

            }

        }
    }
}
