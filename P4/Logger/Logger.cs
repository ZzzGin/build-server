/////////////////////////////////////////////////////////////////////
// Logger.cs - Provides logging facility for tests                 //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2010 //
/////////////////////////////////////////////////////////////////////

using System;
using System.Text;

namespace Tests
{
    public interface ILogger
    {
        ILogger add(string logitem);
        void showCurrentItem();
        void showAll();
    }

    public class ConsoleLogger : ILogger
    {
        StringBuilder logtext = new StringBuilder();
        string currentItem = null;

        public ConsoleLogger()
        {
            string time = DateTime.Now.ToString();
            string title = "\n\n  Console Log: " + time;
            logtext = new StringBuilder(title);
            logtext.Append("\n " + new string('=', title.Length));
        }
        public ILogger add(string logitem)
        {
            currentItem = logitem;
            logtext.Append("\n" + logitem);
            return this;
        }
        public void showCurrentItem()
        {
            Console.Write("\n" + currentItem);
        }
        public void showAll()
        {
            Console.Write(logtext + "\n");
        }
    }
}
