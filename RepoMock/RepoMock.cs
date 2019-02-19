////////////////////////////////////////////////////////////////////////////
// NavigatorServer.cs - File Server for WPF NavigatorClient Application   //
// ver 2.0                                                                //
// Jing Qi, CSE681 - Software Modeling and Analysis, Fall 2017        //
// source: Jim Fawcett
////////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines a single NavigatorServer class that returns file
 * and directory information about its rootDirectory subtree.  It uses
 * a message dispatcher that handles processing of all incoming and outgoing
 * messages.
 * 
 * Maintanence History:
 * --------------------
 * ver 2.0 - 24 Oct 2017
 * - added message dispatcher which works very well - see below
 * - added these comments
 * ver 1.0 - 22 Oct 2017
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluggableComm;
using IComm;
using BuildEnvironment;
using PluggableRepository;
using System.IO;

namespace Navigator
{
    public class NavigatorServer
    {
        IFileMgr localFileMgr { get; set; } = null;
        Comm comm { get; set; } = null;

        Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher =
          new Dictionary<string, Func<CommMessage, CommMessage>>();

        /*----< initialize server processing >-------------------------*/

        public NavigatorServer()
        {
            initializeEnvironment();
            localFileMgr = FileMgrFactory.create(FileMgrType.Local);
        }
        /*----< set Environment properties needed by server >----------*/

        void initializeEnvironment()
        {
            BuildEnvironment.Environment.root = ServerEnvironment.root;
            BuildEnvironment.Environment.address = ServerEnvironment.address;
            BuildEnvironment.Environment.port = ServerEnvironment.port;
            BuildEnvironment.Environment.endPoint = ServerEnvironment.endPoint;
        }
        /*----< define how each message will be processed >------------*/

        void initializeDispatcher()
        {
            Func<CommMessage, CommMessage> getTopFiles = (CommMessage msg) =>
            {
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopFiles";
                reply.arguments = localFileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["getTopFiles"] = getTopFiles;

            Func<CommMessage, CommMessage> getTopDirs = (CommMessage msg) =>
            {
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopDirs";
                reply.arguments = localFileMgr.getDirs().ToList<string>();
                return reply;
            };
            messageDispatcher["getTopDirs"] = getTopDirs;

            Func<CommMessage, CommMessage> moveIntoFolderFiles = (CommMessage msg) =>
            {
                if (msg.arguments.Count() == 1)
                    localFileMgr.currentPath = msg.arguments[0];
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "moveIntoFolderFiles";
                reply.arguments = localFileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["moveIntoFolderFiles"] = moveIntoFolderFiles;

            Func<CommMessage, CommMessage> moveIntoFolderDirs = (CommMessage msg) =>
            {
                if (msg.arguments.Count() == 1)
                    localFileMgr.currentPath = msg.arguments[0];
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "moveIntoFolderDirs";
                reply.arguments = localFileMgr.getDirs().ToList<string>();
                return reply;
            };
            messageDispatcher["moveIntoFolderDirs"] = moveIntoFolderDirs;

            Func<CommMessage, CommMessage> getBRFiles = (CommMessage msg) =>
            {
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getBRFiles";
                string[] bRFiles = Directory.GetFiles("../../../ServerFiles/","*.xml");
                reply.arguments = bRFiles.ToList<string>();
                return reply;
            };
            messageDispatcher["getBRFiles"] = getBRFiles;

            Func<CommMessage, CommMessage> needXML = (CommMessage msg) =>
            {
                comm.postFile("../../../ServerFiles/", msg.arguments[1], msg.arguments[0]);
                CommMessage reply = new CommMessage(CommMessage.MessageType.reply);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "";
                return reply;
            };
            messageDispatcher["needXML"] = needXML;
        }
        /*----< Server processing >------------------------------------*/
        /*
         * - all server processing is implemented with the simple loop, below,
         *   and the message dispatcher lambdas defined above.
         */
        static void Main(string[] args)
        {
            TestUtilities.title("Starting Navigation Server", '=');
            try
            {
                NavigatorServer server = new NavigatorServer();
                server.initializeDispatcher();
                server.comm = new Comm(ServerEnvironment.address, ServerEnvironment.port);

                while (true)
                {
                    CommMessage msg = server.comm.getMessage();
                    if (msg.type == CommMessage.MessageType.closeReceiver)
                        break;
                    msg.show();
                    if (msg.command == null)
                        continue;
                    CommMessage reply = server.messageDispatcher[msg.command](msg);
                    reply.show();
                    server.comm.postMessage(reply);
                }
            }
            catch (Exception ex)
            {
                Console.Write("\n  exception thrown:\n{0}\n\n", ex.Message);
            }
        }
    }
}
