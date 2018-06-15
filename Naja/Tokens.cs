using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Naja
{
    [DebuggerDisplay("{Name}({MatchExpression})")]
    class Token
    {
        public string Name;
        public string MatchExpression;
        public bool HasSubTokens = false;

        public static Token Create(string name, string matchExpression)
        {
            var token = new Token();
            token.Name = name;
            token.MatchExpression = matchExpression;
            return token;
        }
    }

    class KleeneStar:Token
    {
        public Token[] Tokens;
        public KleeneStar(params Token[] tokens)
        {
            Name = "<kleene-star>";
            Tokens = tokens;
            if (tokens == null || tokens.Length == 0)
            {
                throw new InvalidGrammarException("Kleene Star must have sub-tokens!  None were provided.");
            }
            HasSubTokens = true;
            MatchExpression =@"\(" + string.Join(@"\s*", from t in tokens select t.MatchExpression) + @"\)*";
        }
    }

    static class Patterns
    {
        public const string BraceOpen = "{";
        public const string BraceClose = "}";
        public const string ParenthesisOpen = @"\(";
        public const string ParenthesisClose = @"\)";
        public const string Colon = ":";
        public const string Tab = @"\t";
        public const string SpaceBetweenTokens = "[ ]";
        public const string SpaceIndent = "[ ]{4}";
        public const string IntType = "int";
        public const string AsKeyword = "as";
        public const string DefKeyword = "def";
        public const string ReturnKeyword = "return";
        public const string Identifier = @"[a-zA-Z]\w*";
        public const string IntLiteral = @"[0-9]+";
        public const string NewLine = @"(\r\n)|(\n)";
        public const string SpaceSpecial = @"(?<=\W) ";
        public const string NotKeyword = "not";
        public const string NegationUnary = "-";
        public const string BitwiseComplement = "~";
        public const string Plus = "[+]";
        public const string Multiply = "[*]";
        public const string Divide = "[/]";
    }

    static class Tokens
    {
        public static readonly Token BraceOpen;
        public static readonly Token BraceClose;
        public static readonly Token ParenthesisOpen;
        public static readonly Token ParenthesisClose;
        public static readonly Token Colon;
        public static readonly Token Tab;
        public static readonly Token SpaceBetweenTokens;
        public static readonly Token SpaceIndent;
        public static readonly Token IntType;
        public static readonly Token AsKeyword;
        public static readonly Token DefKeyword;
        public static readonly Token ReturnKeyword;
        public static readonly Token Identifier;
        public static readonly Token IntLiteral;
        public static readonly Token NewLine;
        public static readonly Token SpaceSpecial;
        public static readonly Token NotKeyword;
        public static readonly Token BitwiseComplement;
        public static readonly Token Minus; //ie -9
        public static readonly Token Plus;
        public static readonly Token Multiply;
        public static readonly Token Divide;


        public static Dictionary<string, Token> RegisteredTokens;
        public static Dictionary<string, Token> NonWordTokens;
        static Tokens()
        {
            BraceOpen = Token.Create(nameof(BraceOpen), Patterns.BraceOpen);
            BraceClose = Token.Create(nameof(BraceClose), Patterns.BraceClose);
            ParenthesisOpen = Token.Create(nameof(ParenthesisOpen), Patterns.ParenthesisOpen);
            ParenthesisClose = Token.Create(nameof(ParenthesisClose), Patterns.ParenthesisClose);
            Colon = Token.Create(nameof(Colon), Patterns.Colon);
            SpaceBetweenTokens = Token.Create(nameof(SpaceBetweenTokens), Patterns.SpaceBetweenTokens);
            IntType = Token.Create(nameof(IntType), Patterns.IntType);
            AsKeyword = Token.Create(nameof(AsKeyword), Patterns.AsKeyword);
            DefKeyword = Token.Create(nameof(DefKeyword), Patterns.DefKeyword);
            ReturnKeyword = Token.Create(nameof(ReturnKeyword), Patterns.ReturnKeyword);
            Identifier = Token.Create(nameof(Identifier), Patterns.Identifier);
            IntLiteral = Token.Create(nameof(IntLiteral), Patterns.IntLiteral);

            SpaceIndent = Token.Create(nameof(SpaceIndent), Patterns.SpaceIndent);
            Tab = Token.Create(nameof(Tab), Patterns.Tab);
            NewLine = Token.Create(nameof(NewLine), Patterns.NewLine);
            SpaceSpecial = Token.Create(nameof(SpaceSpecial), Patterns.SpaceSpecial);

            NotKeyword = Token.Create(nameof(NotKeyword), Patterns.NotKeyword);
            Minus = Token.Create(nameof(Minus), Patterns.NegationUnary);
            BitwiseComplement = Token.Create(nameof(BitwiseComplement), Patterns.BitwiseComplement);

            Plus = Token.Create(nameof(Plus), Patterns.Plus);
            Multiply = Token.Create(nameof(Multiply), Patterns.Multiply);
            Divide = Token.Create(nameof(Divide), Patterns.Divide);

            RegisteredTokens = new Dictionary<string, Token>()
            {
                {BraceOpen.Name, BraceOpen }, {BraceClose.Name, BraceClose },
                {Colon.Name,Colon },
                {SpaceBetweenTokens.Name,SpaceBetweenTokens },
                {IntType.Name,IntType },
                {AsKeyword.Name,AsKeyword },
                {DefKeyword.Name,DefKeyword },
                {ReturnKeyword.Name,ReturnKeyword },
                {Identifier.Name,Identifier },
                {IntLiteral.Name,IntLiteral },
                {NotKeyword.Name, NotKeyword},
                {Minus.Name, Minus},
                {BitwiseComplement.Name, BitwiseComplement},
                {Plus.Name, Plus},
                {Multiply.Name, Multiply},
                {Divide.Name, Divide}
            };

            NonWordTokens = new Dictionary<string, Token>()
            {
                {SpaceIndent.Name,SpaceIndent },
                {Tab.Name,Tab },
                {NewLine.Name,NewLine },
                {ParenthesisOpen.Name, ParenthesisOpen }, {ParenthesisClose.Name, ParenthesisClose },
                {SpaceSpecial.Name, SpaceSpecial}
            };

        }
    }

}
