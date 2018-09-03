using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Naja
{
    class Program
    {

        static void Main(string[] args)
        {
            var grammar = new Grammar();

            string sourceFile = args!=null && args.Length > 0 ? args[0]:"";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !string.IsNullOrEmpty(sourceFile))
            {
                string path = Path.GetDirectoryName(sourceFile);
                string assemblyFile = Path.Combine(path, Path.GetFileNameWithoutExtension(sourceFile) + ".asm");

                string source = File.ReadAllText(sourceFile).Trim();
                Lexer lexer = new Lexer(source);

                if (!grammar.TryParseGrammar(lexer, out ASTNode rootNode))
                {
                    //Error has already been printed
                    return;
                }

                Debug.WriteLine(rootNode.Prettify());
                /*
                var match = reMatcher.Match(source);
                string returnCode = match.Groups["RC"].Value;
                System.IO.File.WriteAllText(assemblyFile, string.Format(assembly_format, returnCode));

                Process procCompiler = new Process();
                procCompiler.StartInfo = new ProcessStartInfo();
                procCompiler.StartInfo.FileName = @"c:\fasm\fasm.exe";
                procCompiler.StartInfo.Arguments = assemblyFile;
                procCompiler.Start();

                procCompiler.WaitForExit();
                */
            }
            else
            {
                var rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:","");
                Log("RootDir:" + rootDir);
                var validTestCasesPath = Path.Combine(rootDir, "TestFiles", "Valid");
                var invalidTestCasesPath = Path.Combine(rootDir, "TestFiles", "Invalid");

                CodeGeneration generator = new CodeGeneration();

                foreach (string filepath in Directory.GetFiles(validTestCasesPath,"*.naja"))
                {
                    string source = File.ReadAllText(filepath);
                    Lexer lexer = new Lexer(source);
                    if (!lexer.IsLexable){
                        Log($"FAILED  - {Path.GetFileName(filepath)} unable to Lex file");

                    }
                    else if (!grammar.TryParseGrammar(lexer, out ASTNode rootNode))
                    {
                        Log($"FAILED  - {Path.GetFileName(filepath)} was supposed to succeed");    
                    }
                    else
                    {
                        Log($"SUCCESS - {Path.GetFileName(filepath)} was supposed to succeed");    
                        string assembler_code = generator.GenerateCodeFromNode(rootNode);
                        Log(assembler_code);
                    }
                }
                foreach (string filepath in Directory.GetFiles(invalidTestCasesPath, "*.naja"))
                {
                    string source = File.ReadAllText(filepath);
                    Lexer lexer = new Lexer(source);
                    if (!lexer.IsLexable)
                    {
                        Log($"FAILED  - {Path.GetFileName(filepath)} unable to Lex file");
                    }
                    else if (grammar.TryParseGrammar(lexer, out ASTNode rootNode))
                    {
                        Log($"FAILED  - {Path.GetFileName(filepath)} was supposed to fail");
                    }
                    else
                    {
                        Log($"SUCCESS - {Path.GetFileName(filepath)} was supposed to fail");
                    }
                }
            }

        }

        public static void Log(string toLog)
        {
            Debug.WriteLine(toLog);
            Console.WriteLine(toLog);
        }
    }

}
