/////////////////////////////////////////////////////////////////////
// FileMgr - provides file and directory handling for navigation   //
// ver 1.0                                                         //
// author: Jing Qi                                                 //
// source:Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2017 //
/////////////////////////////////////////////////////////////////////
/* * Package Operations: * =================== * provides file and directory handling for navigation* * Public Interface * ---------------- * getFiles - getFiles in specefic dir * setDir - set to a specefic dir* . * . * .  *  * Required Files: * --------------- * *** Maintenance History: * -------------------- * ver 1.0 : 06 Dec 2017 * - first release *  */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Navigator
{
    public enum FileMgrType { Local, Remote }

    ///////////////////////////////////////////////////////////////////
    // NavigatorClient uses only this interface and factory

    public interface IFileMgr
    {
        IEnumerable<string> getFiles();
        IEnumerable<string> getDirs();
        bool setDir(string dir);
        Stack<string> pathStack { get; set; }
        string currentPath { get; set; }
    }

    public class FileMgrFactory
    {
        static public IFileMgr create(FileMgrType type)
        {
            if (type == FileMgrType.Local)
                return new LocalFileMgr();
            else
                return null;  // eventually will have remote file Mgr
        }
    }

    ///////////////////////////////////////////////////////////////////
    // Concrete class for managing local files

    public class LocalFileMgr : IFileMgr
    {
        public string currentPath { get; set; } = "";
        public Stack<string> pathStack { get; set; } = new Stack<string>();

        public LocalFileMgr()
        {
            pathStack.Push(currentPath);  // stack is used to move to parent directory
        }
        //----< get names of all files in current directory >------------

        public IEnumerable<string> getFiles()
        {
            List<string> files = new List<string>();
            string path = Path.Combine(BuildEnvironment.Environment.root, currentPath);
            string absPath = Path.GetFullPath(path);
            files = Directory.GetFiles(path).ToList<string>();
            for (int i = 0; i < files.Count(); ++i)
            {
                files[i] = Path.Combine(currentPath, Path.GetFileName(files[i]));
            }
            return files;
        }
        //----< get names of all subdirectories in current directory >---

        public IEnumerable<string> getDirs()
        {
            List<string> dirs = new List<string>();
            string path = Path.Combine(BuildEnvironment.Environment.root, currentPath);
            dirs = Directory.GetDirectories(path).ToList<string>();
            for (int i = 0; i < dirs.Count(); ++i)
            {
                string dirName = new DirectoryInfo(dirs[i]).Name;
                dirs[i] = Path.Combine(currentPath, dirName);
            }
            return dirs;
        }
        //----< sets value of current directory - not used >-------------

        public bool setDir(string dir)
        {
            if (!Directory.Exists(dir))
                return false;
            currentPath = dir;
            return true;
        }
    }
#if (TEST_FILEMANAGER)
    class TestFileMgr
    {
        static void Main(string[] args)
        {
            BuildEnvironment.Environment.root = BuildEnvironment.ServerEnvironment.root;
            BuildEnvironment.Environment.address = BuildEnvironment.ServerEnvironment.address;
            BuildEnvironment.Environment.port = BuildEnvironment.ServerEnvironment.port;
            BuildEnvironment.Environment.endPoint = BuildEnvironment.ServerEnvironment.endPoint;
            IFileMgr fileMgr;
            fileMgr = FileMgrFactory.create(FileMgrType.Local);
            List<string> files = fileMgr.getFiles().ToList<string>();
            List<string> dirs = fileMgr.getDirs().ToList<string>();
            foreach (string file in files)
            {
                Console.Write(file+"\n");
            }
            foreach(string dir in dirs)
            {
                Console.Write(dir + "\n");
            }
        }
    }
}
#endif
