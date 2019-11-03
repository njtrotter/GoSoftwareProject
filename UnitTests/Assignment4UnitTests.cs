using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RuleCheckerSpace;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FluentAssertions;
using FluentAssertions.Json;


namespace UnitTests
{
    [TestClass]
    public class Assignment4UnitTests
    {
        private List<string> _test_files = new List<string>();

        [TestMethod]
        //Test all files found in TestFiles/ in the build folder
        public void PeerTests()
        {
            List<string> inputs = new List<string>();
            List<string> outputs = new List<string>();
            DirectorySearch("TestFiles/4");
            foreach (string file in _test_files)
            {
                if (file.Length - file.LastIndexOf('i') == 6)
                    inputs.Add(file);
                else
                    outputs.Add(file);
            }


            for (int i = 0; i < inputs.Count; i++)
                JToken.Parse(TestJson(inputs[i])).Should().BeEquivalentTo(
                    JToken.Parse(ExtractJson(outputs[i])));
        }

        //Helper function for peer tests
        private void DirectorySearch(string sDir)
        {
            foreach (string d in Directory.GetDirectories(sDir))
            {
                foreach (string f in Directory.GetFiles(d))
                {
                    _test_files.Add(f);
                }
                DirectorySearch(d);
            }
        }

        //Parse json from a file
        private string ExtractJson(string filePath)
        {
            string json;

            //read from file
            using (StreamReader r = new StreamReader(filePath))
            {
                json = r.ReadToEnd();
            }

            return json;
        }

        //Parse json from a file and run it through BoardWrapper
        //Returns output of BoardWrapper.JsonCommand
        private string TestJson(string filePath)
        {
            string json = ExtractJson(filePath);

            //Parse console input
            List<JToken> jTokenList = ParsingHelper.ParseJson(json);

            List<JToken> finalList = new List<JToken>();
            foreach (JToken jtoken in jTokenList)
            {
                finalList.Add(RuleCheckerWrapper.JsonCommand(jtoken));
            }

            return JsonConvert.SerializeObject(finalList);
        }
    }
}
