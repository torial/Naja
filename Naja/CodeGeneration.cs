using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.Serialization;

namespace Naja
{
    class CodeGeneration
    {
        static Dictionary<string, Action<ASTNode, StringBuilder>> nodeGenerator;
        static CodeGeneration()
        {
            //Each non terminal has a function that handles its special case.  
            nodeGenerator = new Dictionary<string, Action<ASTNode, StringBuilder>>();
            nodeGenerator[Grammar.ProgramNonTerminal.Name] = GenerateProgramCode;
            nodeGenerator[Grammar.FunctionNonTerminal.Name] = GenerateFunctionCode;
            nodeGenerator[Grammar.StatementNonTerminal.Name] = GenerateStatementCode;
            nodeGenerator[Grammar.ExpressionNonTerminal.Name] = GenerateExpressionCode;
            nodeGenerator[Grammar.UnaryNonTerminal.Name] = GenerateUnaryCode;
        }

        /// <summary>
        /// Generates the program code:  currently this entails just using a Windows template for a single function, but will expand
        /// as more options / features added.
        /// </summary>
        /// <param name="node">Program Non Terminal</param>
        /// <param name="output">String builder that accumulates the assembly code.</param>
        private static void GenerateProgramCode(ASTNode node, StringBuilder output)
        {
            #region Program Template
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
            #endregion
            output.Append(program_template);
            ApplyToChildren(node, output);
        }

        /// <summary>
        /// For each child of a non-terminal node, it will be processed if it is a non-terminal.
        /// 
        /// <important>It IS expected that the non-terminal code will handle all terminal children it has.</important>
        /// </summary>
        /// <param name="node">Non Terminal Node</param>
        /// <param name="output">String builder that accumulates the assembly code.</param>
        /// <param name="skipFirst">Whether the first child node should be skipped  This may have to be changed to having a skipped node list??.</param>
        private static void ApplyToChildren(ASTNode node, StringBuilder output, bool skipFirst = false)
        {
            bool firstNode = true;
            foreach (ASTNode child in node.Children)
            {
                if (firstNode && skipFirst)
                {
                    firstNode = false;
                    continue;
                }

                firstNode = false;
                if (nodeGenerator.TryGetValue(child.Type, out Action<ASTNode, StringBuilder> nonterminalBuilder))
                {
                    nonterminalBuilder(child, output);
                }
            }

        }

        /// <summary>
        /// Generates the unary code.
        /// 
        /// <important>Expects the expression value to be in EAX!</important>
        /// </summary>
        /// <param name="node">Non Terminal UNARY Node</param>
        /// <param name="output">String builder that accumulates the assembly code.</param>
        private static void GenerateUnaryCode(ASTNode node, StringBuilder output)
        {
            var unaryOp = node.Children.First();
            StringBuilder unaryCode = new StringBuilder();
            //Forces the expression code to added before the unary code is.
            ApplyToChildren(node, unaryCode, skipFirst: true);
            switch (unaryOp.Type)
            {
                case nameof(Tokens.NotKeyword):
                    unaryCode.AppendLine("cmp eax, 0\nmov eax, 0\nsete al");
                    break;
                case nameof(Tokens.BitwiseComplement):
                    unaryCode.AppendLine("not eax");
                    break;
                case nameof(Tokens.Minus):
                    unaryCode.AppendLine("neg eax");
                    break;
                default:
                    throw new InvalidGrammarException($"Unary Non Terminal did not have a unary first child terminal.  Had {unaryOp.Type} instead.");
            }
            output.Append(unaryCode.ToString());
        }

        private static void GenerateExpressionCode(ASTNode node, StringBuilder output)
        {
            StringBuilder expressionCode = new StringBuilder();
            //What we need to do:
            //a) 

            bool hasUnary = node.Exists(n => n.Type == Grammar.UnaryNonTerminal.Name);
            var intLiteral = node.Find(n => n.Type == Tokens.IntLiteral.Name);
            string statement = "mov eax, " + intLiteral.Text;
            if (hasUnary)
            {
                output.Replace("{expression}", statement + "\n{unary}\n{expression}");
            }
            else
            {
                output.Replace("{expression}", statement);
            }
        }

        private static void GenerateStatementCode(ASTNode node, StringBuilder output)
        {
            var returnStatement = node.Find(n => n.Type == Tokens.ReturnKeyword.Name);
            output.Replace("{function_body}", "{expression}\n\tret eax");
            ApplyToChildren(node, output);
        }

        private static void GenerateFunctionCode(ASTNode node, StringBuilder output)
        {
            var Id = node.Find(n => n.Type == Tokens.Identifier.Name);
            output.Replace("{function_name}", Id.Text);
            ApplyToChildren(node, output);
        }


        public string GenerateCodeFromNode(ASTNode rootNode)
        {
            StringBuilder sbOutput = new StringBuilder();
            if (Grammar.NonTerminals.FindIndex((t) => t.Name == rootNode.Type) == -1)
            {
                Program.Log($"Unable to generate code.  Nonterminal `{rootNode.Type}` was provided.");
                return string.Empty;
            }
            nodeGenerator[rootNode.Type](rootNode, sbOutput);
            return sbOutput.ToString();
        }
    }

    #region Exceptions
    [Serializable]
    internal class InvalidGrammarException : Exception
    {
        public InvalidGrammarException()
        {
        }

        public InvalidGrammarException(string message) : base(message)
        {
        }

        public InvalidGrammarException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidGrammarException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
    #endregion
}
