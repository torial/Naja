using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
namespace Naja
{
    class CodeGeneration
    {
        static Dictionary<string, Action<ASTNode, StringBuilder>> nodeGenerator;
        static CodeGeneration()
        {
            nodeGenerator = new Dictionary<string, Action<ASTNode, StringBuilder>>();
            nodeGenerator[Grammar.ProgramNonTerminal.Name] = GenerateProgramCode;
            nodeGenerator[Grammar.FunctionNonTerminal.Name] = GenerateFunctionCode;
            nodeGenerator[Grammar.StatementNonTerminal.Name] = GenerateStatementCode;
            nodeGenerator[Grammar.ExpressionNonTerminal.Name] = GenerateExpressionCode;
            nodeGenerator[Grammar.UnaryNonTerminal.Name] = GenerateUnaryCode;
        }

        private static void GenerateUnaryCode(ASTNode node, StringBuilder output)
        {
            var unaryOp = node.Children.First();
            switch(unaryOp.Type){
                case nameof(Tokens.NotKeyword):
                    output.Replace("{unary}", "TODO SOMETHING HERE");
                    break;
                case nameof(Tokens.BitwiseComplement):
                    output.Replace("{unary}", "TODO SOMETHING HERE");
                    break;
                case nameof(Tokens.NegationUnary):
                    output.Replace("{unary}", "TODO SOMETHING HERE");
                    break;
            }
        }

        private static void GenerateExpressionCode(ASTNode node, StringBuilder output)
        {
            bool hasUnary = node.Children.Exists(n => n.Type == Grammar.UnaryNonTerminal.Name);
            if (hasUnary)
            {
                output.Replace("{expression}","{expression}\n{unary}");
            }
            var intLiteral = node.Children.Find(n => n.Type == Tokens.IntLiteral.Name);
            output.Replace("{expression}",intLiteral.Text);
        }

        private static void GenerateStatementCode(ASTNode node, StringBuilder output)
        {
            var returnStatement = node.Children.Find(n => n.Type == Tokens.ReturnKeyword.Name);
            output.Replace("{function_body}", "ret {expression}");
            ApplyToChildren(node, output);
        }

        private static void GenerateFunctionCode(ASTNode node, StringBuilder output)
        {
            var Id = node.Children.Find(n => n.Type == Tokens.Identifier.Name);
            output.Replace("{function_name}", Id.Text);
            ApplyToChildren(node, output);
        }

        private static void GenerateProgramCode(ASTNode node, StringBuilder output)
        {
            const string program_template = @"
format PE GUI
entry {function_name}

section '.text' code readable executable

{function_name}:
        {function_body}
        call [ExitProcess]

section '.idata' import data readable writeable

dd 0,0,0,RVA kernel_name, RVA kernel_table
dd 0,0,0,0,0

kernel_table: 
	ExitProcess dd RVA _ExitProcess
    dd 0
	kernel_name db 'KERNEL32.DLL',0

  	_ExitProcess dw 0
    db 'ExitProcess',0

section '.reloc' fixups data readable discardable; needed for Win32s
";
            output.Append(program_template);
            ApplyToChildren(node, output);
        }

        private static void ApplyToChildren(ASTNode node, StringBuilder output)
        {
            foreach (ASTNode child in node.Children)
            {
                if (nodeGenerator.TryGetValue(child.Type, out Action<ASTNode, StringBuilder> nonterminalBuilder))
                {
                    nonterminalBuilder(child, output);
                }
            }

        }

        public CodeGeneration()
        {
            
        }

        public string GenerateForNode(ASTNode rootNode)
        {
            StringBuilder sbOutput = new StringBuilder();
            if (Grammar.NonTerminals.FindIndex((t)=>t.Name == rootNode.Type)==-1)
            {
                Program.Log($"Unable to generate code.  Nonterminal `{rootNode.Type}` was provided.");
                return string.Empty;
            }
            nodeGenerator[rootNode.Type](rootNode, sbOutput);
            return sbOutput.ToString();
        }
    }
}
