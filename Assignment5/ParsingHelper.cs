using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


/* Static class that provides methods for parsing
 */
public static class ParsingHelper
{
    /* Takes a string of json values
     * Parses and returns the json values as a list of JTokens
     */
    public static List<JToken> ParseJson(string jsonString)
    {
        List<JToken> jTokenList = new List<JToken>();

        JsonTextReader reader = new JsonTextReader(new StringReader(jsonString));
        reader.SupportMultipleContent = true;
        JsonSerializer serializer = new JsonSerializer();

        while (true)
        {
            if (!reader.Read())
                break;

            JToken jtoken = serializer.Deserialize<JToken>(reader);
            jTokenList.Add(jtoken);
        }

        return jTokenList;
    }

    /*
     * Converts a point in string form "x-y" to and int[] in the form {x - 1, y - 1}
     */
    public static int[] ParsePoint(string point)
    {
        int[] points = new int[2];
        string num = "";
        foreach (char c in point)
        {
            if (c.ToString() == "-")
            {
                points[0] = int.Parse(num) - 1;
                num = "";
                continue;
            }
            num += c;
        }

        points[1] = int.Parse(num) - 1;

        return points;
    }
}
