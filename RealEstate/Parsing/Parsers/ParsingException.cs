using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstate.Parsing.Parsers
{
    public class ParsingException : Exception
    {
        public string UnrecognizedData { get; protected set; }

        public ParsingException(string message,string unrecognizedData): base(message)
        {
            UnrecognizedData = unrecognizedData;
        }
    }
}
