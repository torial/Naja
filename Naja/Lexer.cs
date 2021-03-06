﻿using System;
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

        public override string ToString()
        {
            return $"{Type}({Text})";
        }
    }

    class ReversableLexer : IDisposable
    {
        public bool ShouldUndo = false;
        private Lexer lexer;
        private int currentLexeme;
        public List<string> Errors;

        public ReversableLexer(Lexer lexer)
        {
            this.lexer = lexer;
            this.currentLexeme = lexer.CurrentLexeme;
            this.Errors = new List<string>();
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
        public const string EndOfFile = "<None>";
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
        private readonly string debug;

        public Lexer(string fileContents)
        {
            cleaned = reValidTokens.Replace(fileContents, ValidTokenMatcher);
            //lexemes.Clear();//For debug purposes ONLY!
            //debug = reValidTokens.Replace(fileContents, DebugValidTokenMatcher);
            CurrentLexeme = 0;
        }

        public int CurrentLexeme;
        public string CurrentLexemeName {
            get{
                if (CurrentLexeme < 0 || CurrentLexeme >= lexemes.Count)
                {
                    return EndOfFile;
                }
                var lexeme = lexemes[CurrentLexeme];
                return lexeme.Type+"("+lexeme.Text+")";
            }}
        public Lexeme Next()
        {
            if (CurrentLexeme < 0 || CurrentLexeme >= lexemes.Count )
            {
                return Lexeme.None;
            }

            return lexemes[CurrentLexeme++];
        }

        public Lexeme Previous()
        {
            if (CurrentLexeme < 0 || CurrentLexeme >= lexemes.Count)
            {
                return Lexeme.None;
            }

            return lexemes[CurrentLexeme--];
        }

        public string Dump()
        {
            StringBuilder sb = new StringBuilder();
            Lexeme next = null;
            while (next!=Lexeme.None)
            {
                next = Next();
                sb.Append(next.ToString());
                sb.Append(" ");
            }
            return sb.ToString();
        }

        private string DebugValidTokenMatcher(Match match)
        {
            ValidTokenMatcher(match);
            return lexemes[lexemes.Count - 1].Type;
        }

        private string ValidTokenMatcher(Match match)
        {
            Lexeme lexeme = new Lexeme();
            for (int i = 1; i < match.Groups.Count; i++)
            {
                var group = match.Groups[i];
                if (group.Success)
                {
                    if (group.Name == Tokens.Identifier.Name)
                    {
                        Token token;
                        if (Tokens.Keywords.TryGetValue(match.Value, out token))
                        {
                            lexeme.Type = token.Name;
                            break; //Identifier aggressively matches, so this protects against that.
                        }
                    }
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
            foreach(var token in Tokens.NonWordTokens.Values)
            {
                tokens.Append("(?<");
                tokens.Append(token.Name);
                tokens.Append(">");
                tokens.Append(token.MatchExpression);
                tokens.Append(")|");

            }
            //get rid of last |
            tokens.Length--;

            tokens.Append(@")|(?:");
            foreach (var token in Tokens.UnaryTokens.Values)
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
