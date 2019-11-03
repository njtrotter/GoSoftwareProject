using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BoardSpace;
using CustomExceptions;

namespace RuleCheckerSpace
{
    static class RuleChecker
    {
        private const int _board_size = 19;

        class Scores
        {
            int B { get; set; }
            int W { get; set; }

            public Scores(int b, int w)
            {
                B = b;
                W = w;
            }
        }

        public static JObject Score(string[][] board)
        {
            int blackScore = 0;
            int whiteScore = 0;
            Board boardObject = new Board(board);

            for(int i = 0; i < _board_size; i++)
                for(int j = 0; j < _board_size; j++)
                {
                    if (board[i][j] == "B")
                        blackScore++;
                    else if (board[i][j] == "W")
                        whiteScore++;
                    else
                    {
                        bool bTerritory = boardObject.Reachable((j + 1).ToString() + "-" + (i + 1).ToString(), "B");
                        bool wTerritory = boardObject.Reachable((j + 1).ToString() + "-" + (i + 1).ToString(), "W");
                        if (bTerritory && wTerritory)
                            continue;
                        if (bTerritory)
                            blackScore++;
                        if (wTerritory)
                            whiteScore++;
                    }
                }
            JObject jObject = new JObject(
                new JProperty("B", blackScore),
                new JProperty("W", whiteScore));
            return jObject;
        }

        public static bool Pass()
        {
            return true;
        }

        /* Determines the legality of a play
         * If a play is illegal, throw an exception with a description of violated rule (wrapper should catch this)
         * Else return true
         * Rules to check for:
         * 5) Game starts with blank board
         *  5b) Previous board states must be included (at least 3 after the second turn)
         * 6) Black goes first
         *  6b) Players alternate playing (use get-points, previous player must have placed or passed)
         *  6.7) Each move is either placing a stone or passing
         *  6x) Don't randomly remove stones that have liberties
         * 7) Any piece without liberties should be removed from the board
         * 7a) A play is illegal if the placed stone must be immediately removed
         * 8) Ko - cannot place a stone such that you recreate the board position following one's previous move
         * 9) The game ends when both players have passed consecutively
         */
        public static bool Play(string stone, string point, string[][][] boards)
        {
            Board boardObject;

            string oppositeStone;
            if (stone == "B")
                oppositeStone = "W";
            else
                oppositeStone = "B";

            //Store black and white points for the previous game states
            List<List<List<string>>> pointsLists = new List<List<List<string>>>(); //[0][1].Count == current board, white points
            foreach (string[][] board in boards)
            {
                boardObject = new Board(board);
                List<List<string>> points = new List<List<string>>();
                points.Add(boardObject.GetPoints("B"));
                points.Add(boardObject.GetPoints("W"));
                pointsLists.Add(points);
            }


            //Rule 5 and 5b
            if (boards.Length == 1 || boards.Length == 2)
            {
                string[][] board = boards[boards.Length - 1];
                boardObject = new Board(board);
                if (boardObject.GetPoints(" ").Count != 361)
                    throw new RuleCheckerException("Rule 5 violated in Play: game must start with an empty board");
            }

            //Rule 6
            if (boards.Length == 1)
                if (stone != "B")
                    throw new RuleCheckerException("Rule 6 violated in Play: black must play first");
            if (boards.Length == 2)
                if (stone != "W")
                    throw new RuleCheckerException("Rule 6b violated in Play: white must make the second move");
            if (boards.Length == 3) //Special case where black passes first round
                if (pointsLists[1][0].Count == 0 && pointsLists[1][1].Count == 0 &&
                    pointsLists[2][0].Count == 0 && pointsLists[2][1].Count == 0)
                    if (stone != "B")
                        throw new RuleCheckerException("Rule 6 violated in Play: players must play in succession, black must play first");

            //Rule 6b, 6.7, and 6x, and 7
            for (int i = 1; i < boards.Length; i++)
            {
                int currentPlayer, nextPlayer; //think in terms of board[i]
                if (stone == "B" && i == 1 || stone == "W" && i == 2)
                {
                    currentPlayer = 1;
                    nextPlayer = 0;
                }
                else
                {
                    currentPlayer = 0;
                    nextPlayer = 1;
                }

                //Check that 0 new stones are added by nextPlayer
                List<string> newStones = new List<string>(pointsLists[i - 1][nextPlayer]);
                foreach (string s in pointsLists[i][nextPlayer])
                    newStones.Remove(s);
                if (newStones.Count != 0)
                    throw new RuleCheckerException("Rule 6b violated in Play: new stones were added, players must play in succesion");

                //Check that only 0 or 1 new stones are added by currentPlayer
                newStones = new List<string>(pointsLists[i - 1][currentPlayer]);
                foreach (string s in pointsLists[i][currentPlayer])
                    newStones.Remove(s);
                if (newStones.Count != 0 && newStones.Count != 1)
                    throw new RuleCheckerException("Rule 67 violated in Play: only one stone can be placed per turn (or 0 for pass)");

                //Check that 0 stones are removed by currentPlayer
                List<string> removedStones = new List<string>(pointsLists[i][currentPlayer]);
                foreach (string s in pointsLists[i - 1][currentPlayer])
                    removedStones.Remove(s);
                if (removedStones.Count != 0)
                    throw new RuleCheckerException("Rule 6x violated in Play: stones of currentPlayer should not be removed");

                //Check that removed stones of nextPlayer have no liberties (after placing newStones)
                //Or check that no stones were removed if currentPlayer passed
                if (newStones.Count == 1)
                {
                    removedStones = new List<string>(pointsLists[i][nextPlayer]);
                    foreach (string s in pointsLists[i - 1][nextPlayer])
                        removedStones.Remove(s);

                    Board hBoard = new Board(boards[i]);
                    try
                    {
                        if (currentPlayer == 1)
                            hBoard.PlaceStone("W", newStones[0]);
                        else
                            hBoard.PlaceStone("B", newStones[0]);
                    }
                    catch
                    {
                        throw new RuleCheckerException("Illegal board history in Play: cannot place stones on occupied points");
                    }

                    foreach (string s in removedStones)
                        if (hBoard.Reachable(s, " "))
                            throw new RuleCheckerException("Rule 6x violated in Play: stones of nextPlayer with liberties should not be removed");

                    //Additionally, check that all stones of nextPlayer that should've been removed are removed
                    foreach (string s in pointsLists[i][nextPlayer])
                        if (!hBoard.Reachable(s, " "))
                            if (!removedStones.Contains(s))
                                throw new RuleCheckerException("Rule 6x violated in Play: stones of nextPlayer without liberties should be removed");
                }
                else
                {
                    if (!pointsLists[i][nextPlayer].SequenceEqual(pointsLists[i - 1][nextPlayer]))
                        throw new RuleCheckerException("Rule 7 violated in Play: stones of nextPlayer with liberties should not be removed");
                }

            }

            //Rule 7
            for (int i = 0; i < boards.Length; i++)
            {
                boardObject = new Board(boards[i]);
                foreach (string b in pointsLists[i][0])
                    if (!boardObject.Reachable(b, " "))
                        throw new RuleCheckerException("Rule 7 violated in Play: stones with no liberties must be removed at point " + b);
                foreach (string w in pointsLists[i][1])
                    if (!boardObject.Reachable(w, " "))
                        throw new RuleCheckerException("Rule 7 violated in Play: stones with no liberties must be removed at point" + w);
            }

            //Rule 7a
            boardObject = new Board(boards[0]);
            boardObject.PlaceStone(stone, point);
            if (!boardObject.Reachable(point, " "))
            {
                bool willCapture = false;
                int[] p = ParsingHelper.ParsePoint(point);
                /* 
                 * If the point has no liberties, check if there are any adjacent oppositeStones
                 * if there are, check if they have any liberties
                 * if they all do, move is illegal
                 */
                string eastPoint = (p[0] + 1).ToString() + "-" + (p[1] + 2).ToString();
                if (p[1] != 18 && boardObject.Occupies(oppositeStone, eastPoint) && boardObject.Reachable(eastPoint, " ") != true)
                    willCapture = true;

                string southPoint = (p[0] + 2).ToString() + "-" + (p[1] + 1).ToString();
                if (p[0] != 18 && boardObject.Occupies(oppositeStone, southPoint) && boardObject.Reachable(southPoint, " ") != true)
                    willCapture = true;

                string westPoint = (p[0] + 1).ToString() + "-" + (p[1]).ToString();
                if (p[1] != 0 && boardObject.Occupies(oppositeStone, westPoint) && boardObject.Reachable(westPoint, " ") != true)
                    willCapture = true;

                string northPoint = (p[0]).ToString() + "-" + (p[1] + 1).ToString();
                if (p[0] != 0 && boardObject.Occupies(oppositeStone, northPoint) && boardObject.Reachable(northPoint, " ") != true)
                    willCapture = true;

                if (!willCapture)
                {
                    throw new RuleCheckerException("Rule 7a violated in Play: self sacrifice is not allowed");
                }
            }

            //Rule 8
            //boardObject contains the current board with the current move
            //pointCounts contains lists of points of black and white stones on the board (before the stone is placed)
            //Resolve the board after the move has been made (remove dead stones) and compare with board past1
            //Also compare current board with board past2 (make sure oppositeStone didn't violate this rule)
            if (boards.Length == 3)
            {
                if (stone == "B")
                    foreach (string w in pointsLists[0][1])
                        if (!boardObject.Reachable(w, " "))
                            boardObject.RemoveStone("W", w);
                if (stone == "W")
                    foreach (string b in pointsLists[0][0])
                        if (!boardObject.Reachable(b, " "))
                            boardObject.RemoveStone("B", b);
                List<string> bPoints = boardObject.GetPoints("B");
                List<string> wPoints = boardObject.GetPoints("W");
                if (bPoints.SequenceEqual(pointsLists[1][0]) && wPoints.SequenceEqual(pointsLists[1][1]))
                    throw new RuleCheckerException("Rule 8 violated: cannot place a stone such that you recreate the board position following one's previous move");
                if (pointsLists[0][0].SequenceEqual(pointsLists[2][0]) && pointsLists[0][1].SequenceEqual(pointsLists[2][1]))
                    throw new RuleCheckerException("Rule 8 violated: cannot place a stone such that you recreate the board position following one's previous move");
            }

            //Rule 9
            if (boards.Length == 3)
                if (pointsLists[0][0].SequenceEqual(pointsLists[1][0]) && pointsLists[0][1].SequenceEqual(pointsLists[1][1])
                    && pointsLists[1][0].SequenceEqual(pointsLists[2][0]) && pointsLists[1][1].SequenceEqual(pointsLists[2][1]))
                    throw new RuleCheckerException("Rule 9 violated: The game ends when both players have passed consecutively");

            return true;
        }

        /* Determines the legality of a board history
         * (Basically does what Play() does, but only checks board history)
         */
        public static bool CheckHistory(string stone, string[][][] boards)
        {
            Board boardObject;

            //Store black and white points for the previous game states
            List<List<List<string>>> pointsLists = new List<List<List<string>>>(); //[0][1].Count == current board, white points
            foreach (string[][] board in boards)
            {
                boardObject = new Board(board);
                List<List<string>> points = new List<List<string>>();
                points.Add(boardObject.GetPoints("B"));
                points.Add(boardObject.GetPoints("W"));
                pointsLists.Add(points);
            }

            //Rule 5 and 5b
            if (boards.Length == 1 || boards.Length == 2)
            {
                string[][] board = boards[boards.Length - 1];
                boardObject = new Board(board);
                if (boardObject.GetPoints(" ").Count != 361)
                    throw new RuleCheckerException("Rule 5 violated in Play: game must start with an empty board");
            }

            //Rule 6
            if (boards.Length == 1)
                if (stone != "B")
                    throw new RuleCheckerException("Rule 6 violated in Play: black must play first");
            if (boards.Length == 2)
                if (stone != "W")
                    throw new RuleCheckerException("Rule 6b violated in Play: white must make the second move");
            if (boards.Length == 3) //Special case where black passes first round
                if (pointsLists[1][0].Count == 0 && pointsLists[1][1].Count == 0 &&
                    pointsLists[2][0].Count == 0 && pointsLists[2][1].Count == 0)
                    if (stone != "B")
                        throw new RuleCheckerException("Rule 6 violated in Play: players must play in succession, black must play first");

            //Rule 6b, 6.7, and 6x
            for (int i = 1; i < boards.Length; i++)
            {
                int currentPlayer, nextPlayer; //think in terms of board[i]
                if (stone == "B" && i == 1 || stone == "W" && i == 2)
                {
                    currentPlayer = 1;
                    nextPlayer = 0;
                }
                else
                {
                    currentPlayer = 0;
                    nextPlayer = 1;
                }

                //Check that 0 new stones are added by nextPlayer
                List<string> newStones = new List<string>(pointsLists[i - 1][nextPlayer]);
                foreach (string s in pointsLists[i][nextPlayer])
                    newStones.Remove(s);
                if (newStones.Count != 0)
                    throw new RuleCheckerException("Rule 6b violated in Play: new stones were added, players must play in succesion");

                //Check that only 0 or 1 new stones are added by currentPlayer
                newStones = new List<string>(pointsLists[i - 1][currentPlayer]);
                foreach (string s in pointsLists[i][currentPlayer])
                    newStones.Remove(s);
                if (newStones.Count != 0 && newStones.Count != 1)
                    throw new RuleCheckerException("Rule 67 violated in Play: only one stone can be placed per turn (or 0 for pass)");

                //Check that 0 stones are removed by currentPlayer
                List<string> removedStones = new List<string>(pointsLists[i][currentPlayer]);
                foreach (string s in pointsLists[i - 1][currentPlayer])
                    removedStones.Remove(s);
                if (removedStones.Count != 0)
                    throw new RuleCheckerException("Rule 6x violated in Play: stones of currentPlayer should not be removed");

                //Check that removed stones of nextPlayer have no liberties (after placing newStones)
                //Or check that no stones were removed if currentPlayer passed
                if (newStones.Count == 1)
                {
                    removedStones = new List<string>(pointsLists[i][nextPlayer]);
                    foreach (string s in pointsLists[i - 1][nextPlayer])
                        removedStones.Remove(s);

                    Board hBoard = new Board(boards[i]);
                    try
                    {
                        if (currentPlayer == 1)
                            hBoard.PlaceStone("W", newStones[0]);
                        else
                            hBoard.PlaceStone("B", newStones[0]);
                    }
                    catch
                    {
                        throw new RuleCheckerException("Illegal board history in Play: cannot place stones on occupied points");
                    }

                    foreach (string s in removedStones)
                        if (hBoard.Reachable(s, " "))
                            throw new RuleCheckerException("Rule 6x violated in Play: stones of nextPlayer with liberties should not be removed");

                    //Additionally, check that all stones of nextPlayer that should've been removed are removed
                    foreach (string s in pointsLists[i][nextPlayer])
                        if (!hBoard.Reachable(s, " "))
                            if (!removedStones.Contains(s))
                                throw new RuleCheckerException("Rule 6x violated in Play: stones of nextPlayer without liberties should be removed");
                }
                else
                {
                    if (!pointsLists[i][nextPlayer].SequenceEqual(pointsLists[i - 1][nextPlayer]))
                        throw new RuleCheckerException("Rule 6x violated in Play: stones of nextPlayer with liberties should not be removed");
                }

            }

            //Rule 7
            for (int i = 0; i < boards.Length; i++)
            {
                boardObject = new Board(boards[i]);
                foreach (string b in pointsLists[i][0])
                    if (!boardObject.Reachable(b, " "))
                        throw new RuleCheckerException("Rule 7 violated in Play: stones with no liberties must be removed at point " + b);
                foreach (string w in pointsLists[i][1])
                    if (!boardObject.Reachable(w, " "))
                        throw new RuleCheckerException("Rule 7 violated in Play: stones with no liberties must be removed at point" + w);
            }

            //Rule 8
            //pointCounts contains lists of points of black and white stones on the board (before the stone is placed)
            //Compare current board with board past2 (make sure oppositeStone didn't violate this rule)
            if (boards.Length == 3)
                if (pointsLists[0][0].SequenceEqual(pointsLists[2][0]) && pointsLists[0][1].SequenceEqual(pointsLists[2][1]))
                    throw new RuleCheckerException("Rule 8 violated: cannot place a stone such that you recreate the board position following one's previous move");

            //Rule 9
            if (boards.Length == 3)
                if (pointsLists[0][0].SequenceEqual(pointsLists[1][0]) && pointsLists[0][1].SequenceEqual(pointsLists[1][1])
                    && pointsLists[1][0].SequenceEqual(pointsLists[2][0]) && pointsLists[1][1].SequenceEqual(pointsLists[2][1]))
                    throw new RuleCheckerException("Rule 9 violated: The game ends when both players have passed consecutively");

            return true;
        }

        /* Determines the legality of a move
         * (Basically does what Play() does, but only checks if the move is legal)
         */
         public static bool CheckMove(string stone, string point, string[][][] boards)
        {
            Board boardObject;

            string oppositeStone;
            if (stone == "B")
                oppositeStone = "W";
            else
                oppositeStone = "B";

            //Store black and white points for the previous game states
            List<List<List<string>>> pointsLists = new List<List<List<string>>>(); //[0][1].Count == current board, white points
            foreach (string[][] board in boards)
            {
                boardObject = new Board(board);
                List<List<string>> points = new List<List<string>>();
                points.Add(boardObject.GetPoints("B"));
                points.Add(boardObject.GetPoints("W"));
                pointsLists.Add(points);
            }

            //Rule 7a
            boardObject = new Board(boards[0]);
            boardObject.PlaceStone(stone, point);
            if (!boardObject.Reachable(point, " "))
            {
                bool willCapture = false;
                int[] p = ParsingHelper.ParsePoint(point);
                /* 
                 * If the point has no liberties, check if there are any adjacent oppositeStones
                 * if there are, check if they have any liberties
                 * if they all do, move is illegal
                 */
                string eastPoint = (p[0] + 1).ToString() + "-" + (p[1] + 2).ToString();
                if (p[1] != 18 && boardObject.Occupies(oppositeStone, eastPoint) && boardObject.Reachable(eastPoint, " ") != true)
                    willCapture = true;

                string southPoint = (p[0] + 2).ToString() + "-" + (p[1] + 1).ToString();
                if (p[0] != 18 && boardObject.Occupies(oppositeStone, southPoint) && boardObject.Reachable(southPoint, " ") != true)
                    willCapture = true;

                string westPoint = (p[0] + 1).ToString() + "-" + (p[1]).ToString();
                if (p[1] != 0 && boardObject.Occupies(oppositeStone, westPoint) && boardObject.Reachable(westPoint, " ") != true)
                    willCapture = true;

                string northPoint = (p[0]).ToString() + "-" + (p[1] + 1).ToString();
                if (p[0] != 0 && boardObject.Occupies(oppositeStone, northPoint) && boardObject.Reachable(northPoint, " ") != true)
                    willCapture = true;

                if (!willCapture)
                {
                    throw new RuleCheckerException("Rule 7a violated in Play: self sacrifice is not allowed");
                }
            }

            //Rule 8
            //boardObject contains the current board with the current move
            //pointCounts contains lists of points of black and white stones on the board (before the stone is placed)
            //Resolve the board after the move has been made (remove dead stones) and compare with board past1
            if (boards.Length == 3)
            {
                if (stone == "B")
                    foreach (string w in pointsLists[0][1])
                        if (!boardObject.Reachable(w, " "))
                            boardObject.RemoveStone("W", w);
                if (stone == "W")
                    foreach (string b in pointsLists[0][0])
                        if (!boardObject.Reachable(b, " "))
                            boardObject.RemoveStone("B", b);
                List<string> bPoints = boardObject.GetPoints("B");
                List<string> wPoints = boardObject.GetPoints("W");
                if (bPoints.SequenceEqual(pointsLists[1][0]) && wPoints.SequenceEqual(pointsLists[1][1]))
                    throw new RuleCheckerException("Rule 8 violated: cannot place a stone such that you recreate the board position following one's previous move");
            }

            return true;
        }
    }
}
