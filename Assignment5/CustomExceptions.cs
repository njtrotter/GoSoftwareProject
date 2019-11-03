using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CustomExceptions
{
    public class InvalidJsonInputException : Exception
    {
        public InvalidJsonInputException()
        {
        }

        public InvalidJsonInputException(string message)
            : base(message)
        {
        }

        public InvalidJsonInputException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class RuleCheckerException : Exception
    {
        public RuleCheckerException()
        {
        }

        public RuleCheckerException(string message)
            : base(message)
        {
        }

        public RuleCheckerException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class BoardException : Exception
    {
        public BoardException()
        {
        }

        public BoardException(string message)
            : base(message)
        {
        }

        public BoardException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public class AIPlayerException : Exception
    {
        public AIPlayerException()
        {
        }

        public AIPlayerException(string message)
            : base(message)
        {
        }

        public AIPlayerException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }


}
