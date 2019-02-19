//////////////////////////////////////////////////////////////////////////////// CoreBUilder_ICC.cs                                                       //// ver 1.0                                                                  ////                                                                          //// Author: Jing Qi, zzzgin@hotmail.com || jqi101@syr.edu                    //// Application: CSE681 Project 4-File CoreBUilder_ICC.cs                    //// Environment: C# console                                                  /////////////////////////////////////////////////////////////////////////////////* * Package Operations: * =================== * Build multiple .cs files with ICC* * Public Interface * ---------------- * (constructor) public CoreBuilder_ICC(BuildRequest.BuildRequest br) - need a BuildRequest as an input* build - build the files in build Reqeust*  *  *  *  * Required Files: * --------------- * BuildRequest.cs* FileManager.cs** Maintenance History: * -------------------- * ver 1.0 : 06 Dec 2017 * - first release *  */
using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;
using System.Xml.Linq;
using BuildRequest;

namespace CoreBuilder
{
    public class CoreBuilder_ICC
    {
        public List<string> files { get; set; } = new List<string>();
        public string environment { get; set; } = "";
        public string builderOutputDir { get; set; } = "../../../BuilderStorage/";
        public BuildRequest.BuildRequest thisbr = null;
        private bool buildresultFlag = true;
        public CoreBuilder_ICC(BuildRequest.BuildRequest br)
        {
            thisbr = br;
            environment = thisbr.environment;
            builderOutputDir += ("/" + thisbr.requestID + "/");
            if (!Directory.Exists(builderOutputDir))
                Directory.CreateDirectory(builderOutputDir);
            //files.Add(thisbr.testDir + thisbr.testDriver);
            foreach (string f in thisbr.testedFiles)
            {
                files.Add(thisbr.testDir + f);
            }
        }
        public bool build()
        {
            switch (thisbr.environment)
            {
                case "cs":
                    CSBuilderAll();
                    if (buildresultFlag)
                        return true;
                    else
                        return false;
                case "java":
                    JAVABuilder();
                    if (buildresultFlag)
                        return true;
                    else
                        return false;
                case "cpp":
                    CPPBuilder();
                    if (buildresultFlag)
                        return true;
                    else
                        return false;
                default:
                    Console.Write("Unknown Environment.");
                    return false;
            }
        }

        private void CSBuilderAll()
        {
            Console.Write("\nBuilding All in {0}:\n", thisbr.environment);
            string Output = Path.GetFullPath(builderOutputDir + "Out.dll");
            string buildLogFile = Path.GetFullPath(builderOutputDir + "BuildLog" + thisbr.requestID + ".xml");
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            ICodeCompiler icc = codeProvider.CreateCompiler();

            CompilerParameters parameters = new System.CodeDom.Compiler.CompilerParameters();
            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = Output;
            string[] filesArray = files.ToArray();
            for (int i = 0; i < filesArray.Length; ++i)
            {
                filesArray[i] = System.IO.Path.GetFullPath("../../../BuilderStorage/"+filesArray[i]);
            }
            CompilerResults results = codeProvider.CompileAssemblyFromFile(parameters, filesArray);
            StringBuilder buildlog = new StringBuilder("");
            XElement buildlogElem = new XElement("buildLog");
            buildlog.Append("requestID: " + thisbr.requestID + "\n");
            XElement requestIDElem = new XElement("requestID");
            requestIDElem.Add(thisbr.requestID);
            buildlogElem.Add(requestIDElem);

            buildlog.Append("buildedTimeStamp: " + DateTime.Now.ToString() + "\n");
            XElement buildedTimeStampElem = new XElement("buildedTimeStamp");
            buildedTimeStampElem.Add(DateTime.Now.ToString());
            buildlogElem.Add(buildedTimeStampElem);

            XElement buildResultElem = new XElement("buildResult");
            buildlogElem.Add(buildResultElem);

            if (results.Errors.Count == 0)
            {
                buildResultElem.Add("success");
                buildlog.Append("Build Success!\n");
                buildresultFlag = true;
            }
            else
            {
                buildResultElem.Add("fail");
                buildresultFlag = false;
                XElement buildErrorElem = new XElement("buildError");
                buildResultElem.Add(buildErrorElem);
                foreach (CompilerError compilerError in results.Errors)
                {
                    buildlog.Append(compilerError + "\n");
                    buildErrorElem.Add(compilerError);
                }

            }
            Console.Write(buildlog);
            buildlogElem.Save(buildLogFile);
        }

        private void JAVABuilder()
        {
            /* TODO: this is a JAVA Builder*/
        }

        private void CPPBuilder()
        {
            /* TODO: this is a CPP Builder*/
        }
    }
}
