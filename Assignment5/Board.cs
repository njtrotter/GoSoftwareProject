using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CustomExceptions;

namespace BoardSpace
{
    class Board
    {
        private string[][] _board;

        /* A Board object.
         * Contains an array of string arrays which represent a board
         * Provides various queries and commands to check of modify the board
         * board[row][column]
         */
        public Board(string[][] newBoard = null)
        {
            if (newBoard == null)
            {
                //if Board is constructed with no arguments, create an empty board
                newBoard = new string[19][];
                for (int i = 0; i < newBoard.Length; i++)
                    newBoard[i] = new string[19] {" ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " ", " "};
                _board = newBoard;
            }
            else
            {
                _board = new string[19][];
                for (int i = 0; i < 19; i++)
                {
                    _board[i] = new string[19];
                    Array.Copy(newBoard[i], _board[i], 19);
                }
            }
        }

        /* Returns true if point is occupied, false otherwise
         */
        public bool Occupied(string point)
        {
            int[] coordinate = ParsingHelper.ParsePoint(point);
            if (_board[coordinate[1]][coordinate[0]] == " ")
                return false;
            return true;
        }

        /* Returns true if "point" is by "stone", false otherwise
         */
        public bool Occupies(string stone, string point)
        {
            int[] coordinate = ParsingHelper.ParsePoint(point);
            if (_board[coordinate[1]][coordinate[0]] == stone)
                return true;
            return false;
        }
        
        /* Returns true if there is some path of adjacent maybeStones of whatever maybeStone is at "point"
         *      to a "maybeStone" of whatever is being passed into the second argument
         * Uses graph search to find this path
         */
        public bool Reachable(string point, string maybeStone)
        {
            bool[][] searchGraph = new bool[19][];
            for (int i = 0; i < 19; i++)
                searchGraph[i] = new bool[19];

            List<int[]> searchList = new List<int[]>();
            searchList.Add(ParsingHelper.ParsePoint(point));

            int[] p = searchList[0];
            string initialStone = _board[p[1]][p[0]];
            searchGraph[p[1]][p[0]] = true;

            while (searchList.Count != 0)
            {
                p = searchList[0];
                //if this point == maybeStone, maybeStone is reachable
                if (_board[p[1]][p[0]] == maybeStone)
                    return true;
                //otherwise remove this point from the searchList
                searchList.RemoveAt(0);

                //if this point != initialStone, we don't check the stones adjacent to this point and continue
                if (_board[p[1]][p[0]] != initialStone)
                    continue;

                /* 
                 * if there is a point East of this point and it hasn't been searched
                 * add it to the searchList
                 * and mark on the searchGraph that we will search that point
                 */
                if (p[0] != 18 && searchGraph[p[1]][p[0] + 1] != true)
                {
                    searchList.Add(new int[] { p[0] + 1, p[1] });
                    searchGraph[p[1]][p[0] + 1] = true;
                }
                //South ...
                if (p[1] != 18 && searchGraph[p[1] + 1][p[0]] != true)
                {
                    searchList.Add(new int[] { p[0], p[1] + 1 });
                    searchGraph[p[1] + 1][p[0]] = true;
                }
                //West ...
                if (p[0] != 0 && searchGraph[p[1]][p[0] - 1] != true)
                {
                    searchList.Add(new int[] { p[0] - 1, p[1] });
                    searchGraph[p[1]][p[0] - 1] = true;
                }
                //North ...
                if (p[1] != 0 && searchGraph[p[1] - 1][p[0]] != true)
                {
                    searchList.Add(new int[] { p[0], p[1] - 1 });
                    searchGraph[p[1] - 1][p[0]] = true;
                }
            }

            return false;
        }

        /* Places a stone on the board if the given point is empty and returns the board
         * If the given point is not empty, throws an exception
         */
        public string[][] PlaceStone(string stone, string point)
        {
            if (Occupied(point))
                throw new BoardException("Placing a stone at an already occupied point");
            int[] coordinate = ParsingHelper.ParsePoint(point);
            _board[coordinate[1]][coordinate[0]] = stone;
            return _board;
        }

        /* Removes a "stone" on "point" if a maybeStone of type "stone" occupies that point and returns the board
         * If not, throws an exception
         */
        public string[][] RemoveStone(string stone, string point)
        {
            if (!Occupies(stone, point))
                throw new BoardException("Removing a stone does not exist at that point on the board");
            int[] coordinate = ParsingHelper.ParsePoint(point);
            _board[coordinate[1]][coordinate[0]] = " ";
            return _board;
        }

        /* Return list of COORDINATE points that have "maybeStone" occupying it
         * Orders points in lexigraphic order, column first
         */
        public List<string> GetPoints(string maybeStone)
        {
            List<string> points = new List<string>();
            for (int i = 0; i < _board.Length; i++)
                for (int j = 0; j < _board[i].Length; j++)
                    if (_board[i][j] == maybeStone)
                        points.Add((j + 1) + "-" + (i + 1));
            points.Sort(CompareStrings);
            return points;
        }

        //Helper function for sorting in GetPoints()
        private static int CompareStrings(string x, string y)
        {
            for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
            {
                int c = x[i].CompareTo(y[i]);
                if (c == 0)
                    continue;
                if (c < 0)
                    return -1;
                if (c > 0)
                    return 1;
            }

            if (x.Length < y.Length)
                return -1;
            return 1;
        }

        /*
         * Returns all adjacent liberties (if they exist)
         */
        public List<string> GetAdjacentLiberties(string point)
        {
            List<string> adj = new List<string>();
            int[] p = ParsingHelper.ParsePoint(point);
            string eastPoint = (p[0] + 1).ToString() + "-" + (p[1] + 2).ToString();
            if (p[1] != 18 && Occupies(" ", eastPoint))
                adj.Add(eastPoint);

            string southPoint = (p[0] + 2).ToString() + "-" + (p[1] + 1).ToString();
            if (p[0] != 18 && Occupies(" ", southPoint))
                adj.Add(southPoint);

            string westPoint = (p[0] + 1).ToString() + "-" + (p[1]).ToString();
            if (p[1] != 0 && Occupies(" ", westPoint))
                adj.Add(westPoint);

            string northPoint = (p[0]).ToString() + "-" + (p[1] + 1).ToString();
            if (p[0] != 0 && Occupies(" ", northPoint))
                adj.Add(northPoint);

            return adj;
        }
    }
}
