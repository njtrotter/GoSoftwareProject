using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RuleCheckerSpace;
using CustomExceptions;
using BoardSpace;

namespace AIPlayerSpace
{
    class AIPlayer
    {
        string _name;
        string _stone;
        string _AIType;
        int _n;
        
        /*
         * Constructor for dumb AIPlayer
         */
        public AIPlayer(string name, string aiType)
        {
            _name = name;
            _AIType = aiType;
        }

        /*
         * Constructor for less dumb AIPlayer
         */
        public AIPlayer(string name, string aiType, int n)
        {
            _name = name;
            _AIType = aiType;
            _n = n;
        }

        /*
         * Sets stone of player
         */
        public void ReceiveStones(string stone)
        {
            _stone = stone;
        }

        /*
         * Returns a point if there is a valid move given the board history
         *      point is determined by _AIType
         * Returns "pass" if there are no valid moves
         * Throws an exception if board history is illegal
         */
        public string MakeAMove(string[][][] boards)
        {
            try
            {
                RuleChecker.CheckHistory(_stone, boards);
            }
            catch(RuleCheckerException)
            {
                throw new AIPlayerException("This history makes no sense!");
            }

            switch (_AIType)
            {
                case "less dumb":
                    string oppositeStone;
                    if (_stone == "B")
                        oppositeStone = "W";
                    else
                        oppositeStone = "B";

                    Board boardObject = new Board(boards[0]);
                    List<string> pointsList = boardObject.GetPoints(oppositeStone);
                    List<string> iterate;
                    List<List<string>> possibleMoves = new List<List<string>>();

                    //Find all oppositeStones with only one adjacent liberty
                    //Add {point, adjacentLiberty} to a possibleMoves
                    foreach (string point in pointsList)
                    {
                        iterate = boardObject.GetAdjacentLiberties(point);
                        iterate.Insert(0, point);
                        if (iterate.Count == 2)
                            possibleMoves.Add(iterate);
                    }

                    List<List<string>> temp = new List<List<string>>();
                    //Check the validity of each possibleMove
                    //Also check that making that possibleMove will result in a capture
                    //Failing these condition, remove move of possibleMoves
                    foreach (List<string> move in possibleMoves)
                    {
                        try
                        {
                            RuleChecker.CheckMove(_stone, move[1], boards);
                        }
                        catch (Exception e)
                        {
                            if (!(e is RuleCheckerException) && !(e is BoardException))
                                throw;
                            continue;
                        }

                        boardObject = new Board(boards[0]);
                        boardObject.PlaceStone(_stone, move[1]);
                        if (!boardObject.Reachable(move[0], " "))
                            temp.Add(move);
                    }
                    possibleMoves = temp;

                    //sort in lowest column lowest row order (numeric)
                    possibleMoves.Sort(ComparePossibleMoves);

                    //for n == 1, return the first possible Move (if it exists)
                    if (_n == 1)
                        if (possibleMoves.Count > 0)
                            return possibleMoves[0][1];
                    //Otherwise, n > 1, and if there is only one possibleMove, return it
                    if (possibleMoves.Count == 1)
                        return possibleMoves[0][1];
                    //Otherwise, play dumb
                    goto case "dumb";
                case "dumb":
                    for (int i = 0; i < 19; i++)
                        for (int j = 0; j < 19; j++)
                        {
                            try
                            {
                                string point = (i + 1).ToString() + "-" + (j + 1).ToString();
                                RuleChecker.CheckMove(_stone, point, boards);
                                return point;
                            }
                            catch (Exception e)
                            {
                                if (!(e is RuleCheckerException) && !(e is BoardException))
                                    throw;
                            }
                        }
                    break;
            }

            return "pass";
        }



        //Helper function for sorting possible moves()
        private static int ComparePossibleMoves(List<string> x, List<string> y)
        {
            int[] px = ParsingHelper.ParsePoint(x[1]);
            int[] py = ParsingHelper.ParsePoint(y[1]);

            if (px[0] == py[0])
            {
                if (px[1] > py[1])
                    return 1;
                return -1;
            }
            if (px[0] > py[0])
                return 1;
            return -1;
        }
    }
}
