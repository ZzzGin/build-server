//////////////////////////////////////////////////////////////////////////////
// BuildReqeust.cs                                                          //
// ver 1.0                                                                  //
//                                                                          //
// Author: Jing Qi, zzzgin@hotmail.com || jqi101@syr.edu                    //
// Application: CSE681 Project 4-BuildRequest                               //
// Environment: C# console                                                  //
//////////////////////////////////////////////////////////////////////////////
/* 
* Package Operations: 
* =================== 
* this package define the BuildRequest and some functions of it
* 
* Public Interface 
* ---------------- 
* wrap - make requests to a XML structure 
* loadXML - load .xml file to doc 
* parse - parse document for property value
* parseList - parse lsit  for property value
* unwrap - unwrap a .xml file to a BuildRequest class
* 
*  
* Required Files: 
* --------------- 
* None
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
//using System.Text;
//using System.Threading.Tasks;
using System.Xml.Linq;

namespace BuildRequest
{
    public class BuildRequest
    {
        public string requestID { get; set; } = "";
        public string testDir { get; set; } = "";
        public string environment { get; set; } = "";
        public List<string> testedFiles { get; set; } = new List<string>();
        public XDocument doc { get; set; } = new XDocument();

        /*  xml elememts structure:
         *  ┏ requestID
         *  ┣ environment
         *  ┗ test
         *      ┣ testDir
         *      ┣ tested
         *      ┣ tested
         *      ┣ ...
         *      ┗ tested
         * */

        // make requests to a XML structure
        public void wrap()
        {
            XElement buildRequestElem = new XElement("buildRequest");
            doc.Add(buildRequestElem);

            XElement requestIDElem = new XElement("requestID");
            requestIDElem.Add(requestID);
            buildRequestElem.Add(requestIDElem);

            XElement environmentElem = new XElement("environment");
            environmentElem.Add(environment);
            buildRequestElem.Add(environmentElem);

            XElement testElem = new XElement("test");
            buildRequestElem.Add(testElem);

            XElement testDirElem = new XElement("testDir");
            testDirElem.Add(testDir);
            testElem.Add(testDirElem);

            foreach (string file in testedFiles)
            {
                XElement testedElem = new XElement("tested");
                testedElem.Add(file);
                testElem.Add(testedElem);
            }
        }

        // load .xml file to doc
        public bool loadXML(string path)
        {
            try
            {
                doc = XDocument.Load(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        // save doc to Xml file
        public bool saveXML(string path)
        {
            try
            {
                doc.Save(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        // parse document for property value
        public string parse(string propertyName)
        {
            string parseStr = doc.Descendants(propertyName).First().Value;
            if (parseStr.Length > 0)
            {
                switch (propertyName)
                {
                    case "testDir":
                        testDir = parseStr;
                        break;
                    case "requestID":
                        requestID = parseStr;
                        break;
                    case "environment":
                        environment = parseStr;
                        break;
                    default:
                        break;
                }
                return parseStr;
            }
            return "";
        }

        public List<string> parseList(string propertyName)
        {
            List<string> values = new List<string>();
            IEnumerable<XElement> parseElems = doc.Descendants(propertyName);
            if (parseElems.Count() > 0)
            {
                switch (propertyName)
                {
                    case "tested":
                        foreach (XElement elem in parseElems)
                        {
                            values.Add(elem.Value);
                        }
                        testedFiles = values;
                        break;
                    default:
                        break;
                }
            }
            return values;
        }

        public void unwrap(string path, bool ConsoleWrite)
        {
            loadXML(path);
            parse("requestID");
            parse("environment");
            parse("testDir");
            parseList("tested");
            if (ConsoleWrite)
            {
                Console.Write("\nUnwrapping buildRequest from{0}", path);
                Console.Write("\n{0}\n", doc.ToString());
                Console.Write("\n  testEnvironment is \"{0}\"", environment);
                Console.Write("\n  testDir is \"{0}\"", testDir);
                Console.Write("\n  testedFiles are:");
                foreach (string file in testedFiles)
                {
                    Console.Write("\n    \"{0}\"", file);
                }
                Console.Write("\n\n");
            }
        }

#if (TEST_BUILDREQUEST)
        static void Main(string[] args)
        {
            BuildRequest br = new BuildRequest();
            br.requestID = "0";
            br.environment = "cs";
            br.testDir = "123";
            br.wrap();
            br.saveXML("../../../ClientFiles/000.xml");
            BuildRequest br1 = new BuildRequest();
            br1.unwrap("../../../ClientFiles/000.xml", false);

        }
        
#endif
    }
}
