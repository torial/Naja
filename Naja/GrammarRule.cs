using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Naja
{
    interface IRule
    {
        List<Token> Tokens { get; set; }
    }

    [DebuggerDisplay(":= {_debug}")]
    class GrammarRule : IRule
    {
        public List<Token> Tokens { get; set; }
        private string _debug;
        public GrammarRule(params Token[] productionParts)
        {
            Tokens = new List<Token>();
            Tokens.AddRange(productionParts);
            var nameList = from p in productionParts select p.Name;
            _debug = string.Join(" ", nameList);
        }

        public static implicit operator List<GrammarRule>(GrammarRule rule)
        {
            return new List<GrammarRule>(){rule};
        }

        public override string ToString()
        {
            return _debug;
        }
    }

    class KleeneGrammarRule : GrammarRule
    {
        
    }

}
