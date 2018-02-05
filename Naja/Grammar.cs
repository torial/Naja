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
        public static Token UnaryNonTerminal = Token.Create(nameof(UnaryNonTerminal), "<unary>");

        public static List<Token> NonTerminals = new List<Token>() { 
            ProgramNonTerminal,
            FunctionNonTerminal,
            StatementNonTerminal,
            ExpressionNonTerminal,
            UnaryNonTerminal
        };
        /*
        <program> ::= <function>
        <function> ::= "def" <id> as "int" "(" ")" "\r\n" <statement> 
        <statement> ::= "return" <exp> 
        <exp> ::= <int>         * 
         */
        public Dictionary<Token, List<GrammarRule>> ProductionRules;
        public Grammar()
        {
            ProductionRules = new Dictionary<Token, List<GrammarRule>>();
            //IMPORTANT, the Root Production can only have one rule!
            ProductionRules[ProgramNonTerminal] = new GrammarRule(FunctionNonTerminal);
            ProductionRules[FunctionNonTerminal] = new GrammarRule(Tokens.DefKeyword,
                Tokens.Identifier, Tokens.ParenthesisOpen, Tokens.ParenthesisClose,
                Tokens.AsKeyword, Tokens.IntType,
                Tokens.NewLine, StatementNonTerminal);
            ProductionRules[StatementNonTerminal] = new GrammarRule(
                Tokens.SpaceIndent, Tokens.ReturnKeyword, ExpressionNonTerminal);
            ProductionRules[ExpressionNonTerminal] = new List<GrammarRule>() { 
                new GrammarRule(Tokens.IntLiteral),
                new GrammarRule(UnaryNonTerminal,ExpressionNonTerminal)};

            ProductionRules[UnaryNonTerminal] = new List<GrammarRule>() { 
                new GrammarRule(Tokens.BitwiseComplement, ExpressionNonTerminal),
                new GrammarRule(Tokens.NotKeyword, ExpressionNonTerminal),
                new GrammarRule(Tokens.NegationUnary, ExpressionNonTerminal)
            };
        }

        public bool TryParseGrammar(Lexer lexer, out ASTNode rootNode)
        {
            Token start = ProgramNonTerminal;
            //Parse the grammar.
            ASTNode programStart = new ASTNode(start.Name);
            rootNode = programStart;

            //IMPORTANT: The first rule production should only have one rule
            GrammarRule currentRule = ProductionRules[start].First();
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
                        bool productionRuleFound = TryProcessNonTerminal(lexer, token, currentNode);
                        if (!productionRuleFound)
                        {
                            return false;
                        }
                        continue;
                    }

                    var lexeme = GetNextNonSpaceLexeme(lexer);
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

        private bool TryProcessNonTerminal(Lexer lexer, Token token, ASTNode currentNode)
        {
            var tokenNode = new ASTNode(token.Name);
            currentNode.Children.Add(tokenNode);
            bool productionRuleFound = false;
            foreach (var rule in ProductionRules[token])
            {
                if (TryProcessRule(lexer, rule, token, tokenNode))
                {
                    productionRuleFound = true;
                    break;
                }
            }
            return productionRuleFound;

        }

        private Lexeme GetNextNonSpaceLexeme(Lexer lexer)
        {
            var lexeme = lexer.Next();
            //Skip over whitespace
            while (lexeme.Type == Tokens.SpaceBetweenTokens.Name || lexeme.Type == Tokens.SpaceSpecial.Name)
            {
                lexeme = lexer.Next();
            }
            return lexeme;
        }

        private void ErrorOccurred(ReversableLexer context, string error)
        {
            context.ShouldUndo = true;
            Program.Log($"{DateTime.Now.ToLongTimeString()}: {error}");
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

        public static implicit operator List<GrammarRule>(GrammarRule rule)
        {
            List<GrammarRule> result = new List<GrammarRule>();
            result.Add(rule);
            return result;
        }
    }
    
}
