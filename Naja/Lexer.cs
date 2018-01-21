using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Naja
{
    [DebuggerDisplay("{Type} = {Text}")]
    class Lexeme
    {
        public string Type;
        public string Text;

        public static readonly Lexeme None = new Lexeme() { Type = "None", Text = "None" };
    }

    class ReversableLexer : IDisposable
    {
        public bool ShouldUndo = false;
        private Lexer lexer;
        private int currentLexeme;

        public ReversableLexer(Lexer lexer)
        {
            this.lexer = lexer;
            this.currentLexeme = lexer.CurrentLexeme;
        }

        public void Dispose()
        {
            if (ShouldUndo)
            {
                this.lexer.CurrentLexeme = this.currentLexeme;
            }
        }
    }

    class Lexer
    {
        static Regex reValidTokens;

        static Lexer()
        {
            reValidTokens = BuildValidTokens();
        }

        private List<Lexeme> lexemes = new List<Lexeme>();
        
        private readonly string cleaned;
        public bool IsLexable
        {
            get
            {
                return string.IsNullOrEmpty(cleaned);
            }
        }

        public Lexer(string fileContents)
        {
            cleaned = reValidTokens.Replace(fileContents, ValidTokenMatcher);
            CurrentLexeme = 0;
        }

        public int CurrentLexeme;
        public Lexeme Next()
        {
            if (CurrentLexeme < 0 || CurrentLexeme >= lexemes.Count )
            {
                return Lexeme.None;
            }

            return lexemes[CurrentLexeme++];
        }

        private string ValidTokenMatcher(Match match)
        {
            Lexeme lexeme = new Lexeme();
            for (int i = 1; i < match.Groups.Count; i++)
            {
                var group = match.Groups[i];
                if (group.Success)
                {
                    lexeme.Type = group.Name;
                    break;
                }
            } 
            lexeme.Text = match.Value;
            lexemes.Add(lexeme);
            return string.Empty;
        }

        private static Regex BuildValidTokens()
        {
            StringBuilder tokens = new StringBuilder(@"(?:\b(?:");
            foreach(var token in Tokens.RegisteredTokens.Values)
            {
                tokens.Append("(?<");
                tokens.Append(token.Name);
                tokens.Append(">");
                tokens.Append(token.MatchExpression);
                tokens.Append(")|");
            }
            //get rid of last |
            tokens.Length--;

            tokens.Append(@")\b)|(?:");
            foreach(var token in Tokens.WhitespaceTokens.Values)
            {
                tokens.Append("(?<");
                tokens.Append(token.Name);
                tokens.Append(">");
                tokens.Append(token.MatchExpression);
                tokens.Append(")|");

            }
            //get rid of last |
            tokens.Length--;
            tokens.Append(")");
            return new Regex(tokens.ToString(), RegexOptions.ExplicitCapture );
        }
    }
}
