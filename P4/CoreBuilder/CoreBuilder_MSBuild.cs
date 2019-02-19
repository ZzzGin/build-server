////////////////////////////////////////////////////////////////////////////////     CoreBuilder_MSBuild.cs                                               //// ver 1.0                                                                  ////                                                                          //// Author: Jing Qi, zzzgin@hotmail.com || jqi101@syr.edu                    //// Application: CSE681 Project 4-File CoreBuilder_MSBuild.cs                                  //// Environment: C# console                                                  /////////////////////////////////////////////////////////////////////////////////* * Package Operations: * =================== * build a project by msbuild (IMPORTANT: THIS CLASS IS NOT USED)* * Public Interface * ---------------- *  BuildCsproj - build a project*  *  *  *   *  * Required Files: * --------------- * None*** Maintenance History: * -------------------- * ver 1.0 : 06 Dec 2017 * - first release *  */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Execution;

namespace CoreBuilder
{
    public class CoreBuilder_MSBuild
    {
        string projectFilePath;
        string projectFileName;
        public string TestDllName;

        public bool BuildCsproj(string projName)
        {
            projectFilePath =
                Path.Combine(@"..\..\..\BuilderStorage\" + projName);

            string[] projectFile = Directory.GetFiles(projectFilePath, "*.csproj");

            projectFileName = projectFile[0];
            TestDllName = Path.GetFileNameWithoutExtension(projectFileName);
            Console.WriteLine("\nTest project path is: {0} \n", projectFileName);
            ConsoleLogger logger = new ConsoleLogger();
            Dictionary<string, string> GlobalProperty = new Dictionary<string, string>();
            BuildRequestData BuildRequest = new BuildRequestData(projectFileName,
                                                                    GlobalProperty, null, new string[] { "Rebuild" }, null);
            BuildParameters bp = new BuildParameters
            {
                Loggers = new List<ILogger> { logger }
            };
            BuildResult buildResult = BuildManager.DefaultBuildManager.Build(bp, BuildRequest);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("The result of this build is: {0}", buildResult.OverallResult);
            Console.ResetColor();
            if (buildResult.OverallResult == 0)
                return true;
            else return false;
        }
    }

#if (TEST_BUILDER)
    //TestBuilder class

    class Test
    {
        public static void Main(string[] args)
        {
            CoreBuilder_MSBuild myBuilder = new CoreBuilder_MSBuild();
            string TestProName = "TestDriver1";
            myBuilder.BuildCsproj(TestProName);
        }
    }
#endif
}
