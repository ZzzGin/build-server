//////////////////////////////////////////////////////////////////////////////
// ConsoleLogger.cs                                                         //
// ver 1.0                                                                  //
//                                                                          //
// Author: Jing Qi, zzzgin@hotmail.com || jqi101@syr.edu                    //
// Application: CSE681 Project 4-ConsoleLogger.cs                           //
// Environment: C# console                                                  //
//////////////////////////////////////////////////////////////////////////////
/* 
* Package Operations: 
* =================== 
* you can easily use "a string".red to make the console output red characters
* 
* Public Interface 
* ---------------- 
* green - output in green
* red - output in red 
* yellow - output in yellow
* .  
*  
* Required Files: 
* --------------- 
* Nonw
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

namespace MotherBuildProcess
{
    public static class TitleExtension
    {
        static public void green(this StringBuilder astring, char underline = '=')
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\n{0}", astring);
            Console.ResetColor();
        }

        static public void yellow(this StringBuilder astring, char underline = '-')
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\n  {0}", astring);
            Console.ResetColor();
        }

        static public void red(this StringBuilder astring, char underline = '-')
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\n  {0}", astring);
            Console.ResetColor();
        }

        static public void green(this string astring, char underline = '=')
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\n{0}", astring);
            Console.ResetColor();
        }

        static public void yellow(this string astring, char underline = '-')
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("\n  {0}", astring);
            Console.ResetColor();
        }

        static public void red(this string astring, char underline = '-')
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\n  {0}", astring);
            Console.ResetColor();
        }
    }
}
