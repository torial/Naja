using System;
using System.Collections.Generic;
using System.Linq;

namespace Naja
{
    class GrammarProcessor
    {
        private Grammar Grammar;
        public GrammarProcessor(Grammar parent)
        {
            Grammar = parent;
        }

        private bool HasShownEOLError = false;

        public bool TryParseGrammar(Lexer lexer, out ASTNode rootNode)
        {
            Token start = Grammar.ProgramNonTerminal;
            //Parse the grammar.
            ASTNode programStart = new ASTNode(start.Name);
            rootNode = programStart;

            //IMPORTANT: The first rule production should only have one rule
            GrammarRule currentRule = Grammar.ProductionRules[start].First();
            string currentRuleName = start.Name;
            ASTNode currentNode = programStart;
            List<string> errors;
            HasShownEOLError = false;
            return TryProcessRule(lexer, currentRule, start, currentNode, out errors);
        }

        private bool IsNonTerminal(Token token)
        {
            return Grammar.NonTerminals.Contains(token) || token.Name == Grammar.KleeneNonTerminal.MatchExpression;
        }

        private bool TryProcessRule(Lexer lexer, GrammarRule currentRule, Token currentRuleName, ASTNode currentNode, out List<string> errors)
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
                        if (!TryProcessToken(context, lexer, currentRuleName, token, workingNode))
                        {
                            return false;
                        }
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

        private bool TryProcessToken(ReversableLexer context, Lexer lexer, Token currentRuleName,
                                     Token token,ASTNode workingNode)
        {
            if (IsNonTerminal(token))
            {
                bool productionRuleFound = TryProcessNonTerminal(lexer, token, workingNode);
                //If none of the possible child non-terminal rules could be matched, then the rule has failed altogether.
                return productionRuleFound;
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
            var kleeneNode = new ASTNode(Grammar.KleeneNonTerminal.Name, kleeneStar.MatchExpression);
            var subRule = new GrammarRule(kleeneStar.Tokens);
            bool successfullyProcessedRule = true;
            errors = new List<string>();
            while (successfullyProcessedRule)
            {
                List<string> errs;
                var workingNode = new ASTNode(Grammar.KleeneNonTerminal.Name, "working Node");
                successfullyProcessedRule = TryProcessRule(lexer, subRule, currentRuleName, workingNode, out errs);
                if (successfullyProcessedRule)
                {
                    kleeneNode.Children.AddRange(workingNode.Children);
                }

            }
            //only add kleene node if it has children
            if (kleeneNode.Children.Count > 0)
                currentNode.Children.Add(kleeneNode);
            return true;
        }
        private bool TryProcessNonTerminal(Lexer lexer, Token token, ASTNode currentNode)
        {
            var tokenNode = new ASTNode(token.Name);
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
                //Program.Log($"Could not process rule: {rule}");
            }
            if (!productionRuleFound)
            {
                LogError(tokenRules.Count == 1, errors, lexer, token);
            }
            else if (HasResults(tokenNode))
            {
                currentNode.Children.Add(tokenNode);
            }
            return productionRuleFound;
        }

        #region Helper Fxns

        private bool HasResults(ASTNode tokenNode)
        {
            return tokenNode.Type != Grammar.KleeneNonTerminal.MatchExpression || tokenNode.Children.Count > 0;
        }

        private List<GrammarRule> GetTokenRules(Token token)
        {
            //All non-terminals that are regular (ie not changing) will match here.
            if (Grammar.NonTerminals.Contains(token))
            {
                return Grammar.ProductionRules[token];
            }
            //The only non-regular terminal at this point is the Kleene Star
            return Grammar.ProductionRules[Grammar.KleeneNonTerminal];
        }

        /// <summary>
        /// Skips the `SpaceBetweenTokens` and `SpaceSpecial` lexemes, returns next Non Space Lexeme
        /// </summary>
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

        /// <summary>
        /// Skips the `SpaceBetweenTokens` and `SpaceSpecial` lexemes
        /// </summary>
        private void SkipSpaces(Lexer lexer)
        {
            var lexeme = lexer.Next();
            //Skip over whitespace
            while (lexeme.Type == Tokens.SpaceBetweenTokens.Name || lexeme.Type == Tokens.SpaceSpecial.Name)
            {
                lexeme = lexer.Next();
            }
            lexer.Previous(); //This resets the ++ that happened at the successful finding of a token.
        }

        #endregion

        #region Error Fxns
        private void LogError(bool IsRuleErrors, List<string> errors, Lexer lexer, Token token)
        {
            if (IsRuleErrors)
            {
                Program.Log(string.Join("\r\n", errors));
            }
            else
            {
                //Program.Log(lexer.Dump());
                LogError(lexer, token);
            }

        }

        private void LogError(Lexer lexer, Token token)
        {
            if (lexer.CurrentLexemeName == Lexer.EndOfFile)
            {
                if (!HasShownEOLError)
                {
                    HasShownEOLError = true;
                    Program.Log($"{DateTime.Now.ToString()}: Unexpected end of file occurred.  Expected a {token.Name}.");
                }
            }
            else
            {
                Program.Log($"{DateTime.Now.ToString()}: Unable to find syntax that matches {token.Name} at {lexer.CurrentLexemeName}.");

            }
        }

        private void ErrorOccurred(ReversableLexer context, string error)
        {
            context.ShouldUndo = true;
            context.Errors.Add($"{DateTime.Now.ToLongTimeString()}: {error}");
        }
        #endregion
    }
}
