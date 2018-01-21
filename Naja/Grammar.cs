using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Naja
{
    class Grammar
    {
        public static Token ProgramNonTerminal = Token.Create(nameof(ProgramNonTerminal), "<program>");
        public static Token FunctionNonTerminal = Token.Create(nameof(FunctionNonTerminal), "<function>");
        public static Token StatementNonTerminal = Token.Create(nameof(StatementNonTerminal), "<statement>");
        public static Token ExpressionNonTerminal = Token.Create(nameof(ExpressionNonTerminal),"<expression>");

        public static List<Token> NonTerminals = new List<Token>() { ProgramNonTerminal,
            FunctionNonTerminal,
            StatementNonTerminal,
            ExpressionNonTerminal
        };
        /*
        <program> ::= <function>
        <function> ::= "def" <id> as "int" "(" ")" "\r\n" <statement> 
        <statement> ::= "return" <exp> 
        <exp> ::= <int>         * 
         */
        public Dictionary<Token, GrammarRule> ProductionRules;
        public Grammar()
        {
            ProductionRules = new Dictionary<Token, GrammarRule>();
            ProductionRules[ProgramNonTerminal] = new GrammarRule(FunctionNonTerminal);
            ProductionRules[FunctionNonTerminal] = new GrammarRule(Tokens.DefKeyword,
                Tokens.Identifier,
                Tokens.AsKeyword, Tokens.IntType,
                Tokens.NewLine, StatementNonTerminal);
            ProductionRules[StatementNonTerminal] = new GrammarRule(
                Tokens.Tab, Tokens.ReturnKeyword, ExpressionNonTerminal);
            ProductionRules[ExpressionNonTerminal] = new GrammarRule(Tokens.IntLiteral);
        }

        public bool TryParseGrammar(Lexer lexer, out ASTNode rootNode)
        {
            Token start = ProgramNonTerminal;
            //Parse the grammar.
            ASTNode programStart = new ASTNode(start.Name);
            rootNode = programStart;

            GrammarRule currentRule = ProductionRules[start];
            string currentRuleName = start.Name;
            ASTNode currentNode = programStart;

            return TryProcessRule(lexer, currentRule, start, currentNode);
        }

        private bool TryProcessRule(Lexer lexer, GrammarRule currentRule, Token currentRuleName,ASTNode currentNode)
        {
            using (var context = new ReversableLexer(lexer))
            {
                foreach (var token in currentRule.Tokens)
                {
                    if (NonTerminals.Contains(token))
                    {
                        var tokenNode = new ASTNode(token.Name);
                        currentNode.Children.Add(tokenNode);
                        if (!TryProcessRule(lexer, ProductionRules[token], token, tokenNode))
                        {
                            //Error has already been reported lower
                            return false;
                        }
                        continue;
                    }

                    var lexeme = lexer.Next();
                    //Skip over whitespace
                    while(lexeme.Type == Tokens.SpaceBetweenTokens.Name)
                    {
                        lexeme = lexer.Next();
                    }
                    if (lexeme.Type == token.Name)
                    {
                        ASTNode child = new ASTNode(lexeme);
                        currentNode.Children.Add(child);
                    }
                    else
                    {
                        ErrorOccurred(context, 
                            $"Error parsing at rule {currentRuleName} for token '{token.Name}', got lexeme '{lexeme.Type}<{lexeme.Text}>'");
                        return false;
                    }//token parsing
                }//for each token
            }//context
            return true;
        }

        private void ErrorOccurred(ReversableLexer context, string error)
        {
            context.ShouldUndo = true;
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now.ToLongTimeString()}: {error}");
        }
    }

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
    }
}
