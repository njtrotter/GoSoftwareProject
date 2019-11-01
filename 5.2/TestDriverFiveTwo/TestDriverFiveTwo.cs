using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIPlayerSpace;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

static class TestDriverFiveTwo
{
    static void Main(string[] args)
    {
        string console = "";
        string input;

        //Read from console
        while ((input = Console.ReadLine()) != null)
            console += input;

        //Parse console input
        List<JToken> jTokenList = ParsingHelper.ParseJson(console);

        List<JToken> finalList = new List<JToken>();

        AIPlayerWrapper aiPlayer = new AIPlayerWrapper();
        JToken toAdd;
        foreach (JToken jtoken in jTokenList)
        {
            toAdd = aiPlayer.JsonCommand(jtoken, "no name", "less dumb");
            if (toAdd.Type != JTokenType.Null)
                finalList.Add(toAdd);
        }

        Console.WriteLine(JsonConvert.SerializeObject(finalList));

        Console.ReadLine();
    }
}
