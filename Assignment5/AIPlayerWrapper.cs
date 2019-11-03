using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CustomExceptions;

namespace AIPlayerSpace
{
    public class AIPlayerWrapper
    {
        /* 
         * Interfaces between the AIPlayer and Json Inputs
         * Provides a method called JsonCommand which is how Json Inputs interact with AIPlayer
         * Also provides checks to make sure Json Inputs are correct
         * Json Commands must be in the format ["register"], ["receive-stones", Stone], and ["make-a-move", Boards]
         * Returns JSON data as a JToken (if input is valid) 
         * Holds an AIPlayer object
         */

        private AIPlayer _player;

        public JToken JsonCommand(JToken jtoken, string name = "no name", string AIType = "dumb", int n = 1)
        {
            JsonValidation.ValidateJTokenAIPlayer(jtoken);

            switch (jtoken.ElementAt(0).ToObject<string>())
            {
                case "register":
                    if (AIType == "less dumb")
                        _player = new AIPlayer(name, AIType, n);
                    else
                        _player = new AIPlayer(name, AIType);
                    return JToken.Parse(JsonConvert.SerializeObject(name));
                case "receive-stones":
                    JsonValidation.ValidateStone(jtoken.ElementAt(1));
                    _player.ReceiveStones(jtoken.ElementAt(1).ToObject<string>());
                    return JToken.Parse(JsonConvert.SerializeObject(null));
                case "make-a-move":
                    JsonValidation.ValidateBoards(jtoken.ElementAt(1));
                    try
                    {
                        return JToken.Parse(JsonConvert.SerializeObject(_player.MakeAMove(
                            jtoken.ElementAt(1).ToObject<string[][][]>())));
                    }
                    catch (AIPlayerException e)
                    {
                        return JToken.Parse(JsonConvert.SerializeObject(e.Message));
                    }
            }

            throw new InvalidJsonInputException("Unrecognized JSONCommand passed to AIPlayerWrapper");

        }
    }
}
