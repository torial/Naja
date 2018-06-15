using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Naja
{
    class Grammar
    {
        public static Token ProgramNonTerminal = Token.Create(nameof(ProgramNonTerminal), "<program>");
        public static Token FunctionNonTerminal = Token.Create(nameof(FunctionNonTerminal), "<function>");
        public static Token StatementNonTerminal = Token.Create(nameof(StatementNonTerminal), "<statement>");
        public static Token ExpressionNonTerminal = Token.Create(nameof(ExpressionNonTerminal),"<expression>");
        public static Token UnaryNonTerminal = Token.Create(nameof(UnaryNonTerminal), "<unary>");
        public static Token TermNonTerminal = Token.Create(nameof(TermNonTerminal), "<term>");
        public static Token FactorNonTerminal = Token.Create(nameof(FactorNonTerminal), "<factor>");
        public static Token KleeneNonTerminal = Token.Create(nameof(KleeneNonTerminal), "<kleene-star>");

        public static List<Token> NonTerminals = new List<Token>() { 
            ProgramNonTerminal,
            FunctionNonTerminal,
            StatementNonTerminal,
            ExpressionNonTerminal,
            TermNonTerminal,
            FactorNonTerminal,
            UnaryNonTerminal,
            KleeneNonTerminal
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
            ProductionRules[ExpressionNonTerminal] = new List<GrammarRule>(){
                new GrammarRule(TermNonTerminal,new KleeneStar(Tokens.Plus,TermNonTerminal)),
                new GrammarRule(TermNonTerminal,new KleeneStar(Tokens.Minus,TermNonTerminal)),
            };
            ProductionRules[TermNonTerminal] = new List<GrammarRule>(){
                new GrammarRule(FactorNonTerminal,new KleeneStar(Tokens.Multiply, FactorNonTerminal)),
                new GrammarRule(FactorNonTerminal,new KleeneStar(Tokens.Divide, FactorNonTerminal))
            };
            ProductionRules[KleeneNonTerminal] = new KleeneGrammarRule();
            ProductionRules[FactorNonTerminal] = new List<GrammarRule>() { 
                new GrammarRule(Tokens.ParenthesisOpen,ExpressionNonTerminal,Tokens.ParenthesisClose),
                new GrammarRule(Tokens.IntLiteral),
                new GrammarRule(UnaryNonTerminal,FactorNonTerminal)};

            ProductionRules[UnaryNonTerminal] = new List<GrammarRule>() { 
                new GrammarRule(Tokens.BitwiseComplement, ExpressionNonTerminal),
                new GrammarRule(Tokens.NotKeyword, ExpressionNonTerminal),
                new GrammarRule(Tokens.Minus, ExpressionNonTerminal)
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
            List<string> errors;
            return TryProcessRule(lexer, currentRule, start, currentNode, out errors);
        }

        private bool IsNonTerminal(Token token)
        {
            return NonTerminals.Contains(token) || token.Name == KleeneNonTerminal.MatchExpression;
        }

        private bool TryProcessRule(Lexer lexer, GrammarRule currentRule, Token currentRuleName,ASTNode currentNode, out List<string> errors)
        {
            if (currentRule is KleeneGrammarRule)
            {
                return TryProcessKleeneRule(lexer, currentRule, currentRuleName, currentNode, out errors);
            }

            //for both Kleene Rule and normal rule, make it so that just like the reversable lexer, it is also possible to reset
            //the currentNode to its current state if parsing fails...
            var workingNode = new ASTNode(currentNode.Type, "working Node");
            using (var context = new ReversableLexer(lexer))
            {
                try
                {
                    foreach (var token in currentRule.Tokens)
                    {
                        if (IsNonTerminal(token))
                        {
                            bool productionRuleFound = TryProcessNonTerminal(lexer, token, workingNode);
                            if (!productionRuleFound)
                            {
                                //If none of the possible child non-terminal rules could be matched, then the rule has failed altogether.
                                return false;
                            }
                            continue;
                        }

                        var lexeme = GetNextNonSpaceLexeme(lexer);
                        if (lexeme.Type == token.Name)
                        {
                            ASTNode child = new ASTNode(lexeme);
                            workingNode.Children.Add(child);
                        }
                        else
                        {
                            ErrorOccurred(context,
                                $"Error parsing at rule {currentRuleName} for token '{token.Name}', got lexeme '{lexeme.Type}<{lexeme.Text}>'");
                            return false;
                        }//token parsing

                    }//for each token
                }
                finally
                {
                    errors = context.Errors;
                }

            }//context
            currentNode.Children.AddRange(workingNode.Children);
            return true;
        }

        private bool TryProcessKleeneRule(Lexer lexer, GrammarRule currentRule, Token currentRuleName, ASTNode currentNode, out List<string> errors)
        {
            //Kleene rule: all the tokens can be parsed consecutively or none of the tokens.. and both scenarios are success.
            if (!(currentRuleName is KleeneStar))
            {
                throw new InvalidGrammarException($"The token was not the expected KleeneStar token, got {currentRuleName.Name} instead.");
            }
            var kleeneStar = (KleeneStar)currentRuleName;
            var kleeneNode = new ASTNode(KleeneNonTerminal.Name,kleeneStar.MatchExpression);
            var subRule = new GrammarRule(kleeneStar.Tokens);
            bool successfullyProcessedRule = true;
            errors = new List<string>();
            while (successfullyProcessedRule)
            {
                List<string> errs;
                var workingNode = new ASTNode(KleeneNonTerminal.Name, "working Node");
                successfullyProcessedRule = TryProcessRule(lexer, subRule, currentRuleName, workingNode, out errs);
                if (successfullyProcessedRule)
                {
                    kleeneNode.Children.AddRange(workingNode.Children);
                }
                
            }
            currentNode.Children.Add(kleeneNode);
            return true;
        }

        private List<GrammarRule> GetTokenRules(Token token)
        {
            //All non-terminals that are regular (ie not changing) will match here.
            if (NonTerminals.Contains(token))
            {
                return ProductionRules[token];
            }
            //The only non-regular terminal at this point is the Kleene Star
            return ProductionRules[KleeneNonTerminal];
        }

        private bool TryProcessNonTerminal(Lexer lexer, Token token, ASTNode currentNode)
        {
            var tokenNode = new ASTNode(token.Name);
            currentNode.Children.Add(tokenNode);
            bool productionRuleFound = false;
            List<List<string>> listOfErrors = new List<List<string>>();
            List<string> errors = new List<string>();
            var tokenRules = GetTokenRules(token);
            foreach (var rule in tokenRules)
            {
                if (TryProcessRule(lexer, rule, token, tokenNode, out errors))
                {
                    productionRuleFound = true;
                    break;
                }
                listOfErrors.Add(errors);
            }
            if (!productionRuleFound)
            {
                if (tokenRules.Count == 1)
                {
                    Program.Log(string.Join("\r\n", errors));
                }
                else if (lexer.CurrentLexemeName == Lexer.EndOfFile)
                {
                    Program.Log($"{DateTime.Now.ToString()}: Unexpected end of file occurred.  Expected a {token.Name}.");
                }
                else
                {
                    Program.Log($"{DateTime.Now.ToString()}: Unable to find syntax that matches {token.Name} at {lexer.CurrentLexemeName}.");

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
            context.Errors.Add($"{DateTime.Now.ToLongTimeString()}: {error}");
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

    class KleeneGrammarRule : GrammarRule
    {
        
    }
    
}
