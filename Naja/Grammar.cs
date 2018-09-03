using System;
using System.Collections.Generic;
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
        private GrammarProcessor GrammarProcessor;
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
                new GrammarRule(Tokens.IntLiteral),
                new GrammarRule(UnaryNonTerminal),
                new GrammarRule(Tokens.ParenthesisOpen,ExpressionNonTerminal,Tokens.ParenthesisClose)};
            ProductionRules[UnaryNonTerminal] = new List<GrammarRule>() { 
                new GrammarRule(Tokens.BitwiseComplement, ExpressionNonTerminal),
                new GrammarRule(Tokens.NotKeyword, ExpressionNonTerminal),
                new GrammarRule(Tokens.Minus, ExpressionNonTerminal)
            };

            GrammarProcessor = new GrammarProcessor(this);
        }

        public bool TryParseGrammar(Lexer lexer, out ASTNode rootNode)
        {
            return GrammarProcessor.TryParseGrammar(lexer, out rootNode);
        }


    }


}
