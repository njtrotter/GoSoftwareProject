using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CustomExceptions;

namespace BoardSpace
{
    /* 
     * Interfaces between the Board and Json Inputs
     * Provides a method called JsonCommand which is how other classes interact with the board
     * Also provides checks to make sure other classes interact correctly with the board
     * Json Commands must be in the format [Board, Statement]
     *      Statements can be a query or a command:
     *      Query is of the form ["occupied?", Point] or ["occupies?", Stone, Point] or ["reachable?", Point, MaybeStone]
     *      Command is of the form ["place", Stone, Point] or ["remove", Stone, Point] or ["get-points", MaybeStone]
     * Returns JSON data as a JToken (if input is valid)
     */
    public class BoardWrapper
    {
        public JToken JsonCommand(JToken jtoken)
        {
            JToken statement;
            Board boardObject;

            JsonValidation.ValidateJTokenBoard(jtoken);
            JsonValidation.ValidateBoard(jtoken.ElementAt(0));
            boardObject = new Board(jtoken.ElementAt(0).ToObject<string[][]>());
            statement = jtoken.ElementAt(1);

            switch (statement.ElementAt(0).ToObject<string>())
            {
                case "occupied?":
                    JsonValidation.ValidateStatementElements(statement, 1);
                    JsonValidation.ValidatePoint(statement.ElementAt(1));
                    return JToken.Parse(JsonConvert.SerializeObject(boardObject.Occupied(
                        statement.ElementAt(1).ToObject<string>())));
                case "occupies?":
                    JsonValidation.ValidateStatementElements(statement, 2);
                    JsonValidation.ValidateStone(statement.ElementAt(1));
                    JsonValidation.ValidatePoint(statement.ElementAt(2));
                    return JToken.Parse(JsonConvert.SerializeObject(boardObject.Occupies(
                        statement.ElementAt(1).ToObject<string>(),
                        statement.ElementAt(2).ToObject<string>())));
                case "reachable?":
                    JsonValidation.ValidateStatementElements(statement, 2);
                    JsonValidation.ValidatePoint(statement.ElementAt(1));
                    JsonValidation.ValidateMaybeStone(statement.ElementAt(2));
                    return JToken.Parse(JsonConvert.SerializeObject(boardObject.Reachable(
                        statement.ElementAt(1).ToObject<string>(),
                        statement.ElementAt(2).ToObject<string>())));
                case "place":
                    JsonValidation.ValidateStatementElements(statement, 2);
                    JsonValidation.ValidateStone(statement.ElementAt(1));
                    JsonValidation.ValidatePoint(statement.ElementAt(2));
                    try //PlaceStone may return an exception
                    {
                        return JToken.Parse(JsonConvert.SerializeObject(boardObject.PlaceStone(
                            statement.ElementAt(1).ToObject<string>(),
                            statement.ElementAt(2).ToObject<string>())));
                    }
                    catch (BoardException)
                    {
                        return JToken.Parse(JsonConvert.SerializeObject("This seat is taken!"));
                    }
                case "remove":
                    JsonValidation.ValidateStatementElements(statement, 2);
                    JsonValidation.ValidateStone(statement.ElementAt(1));
                    JsonValidation.ValidatePoint(statement.ElementAt(2));
                    try
                    { //RemoveStone may return an exception
                        return JToken.Parse(JsonConvert.SerializeObject(boardObject.RemoveStone(
                            statement.ElementAt(1).ToObject<string>(),
                            statement.ElementAt(2).ToObject<string>())));
                    }
                    catch (BoardException)
                    {
                        return JToken.Parse(JsonConvert.SerializeObject("I am just a board! I cannot remove what is not there!"));
                    }
                case "get-points":
                    JsonValidation.ValidateStatementElements(statement, 1);
                    JsonValidation.ValidateMaybeStone(statement.ElementAt(1));
                    return JToken.Parse(JsonConvert.SerializeObject(boardObject.GetPoints(
                        statement.ElementAt(1).ToObject<string>())));
            }

            throw new InvalidJsonInputException("Unrecognized query/command passed to BoardWrapper");

        }
    }
}
