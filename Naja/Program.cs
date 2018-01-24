using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

namespace Naja
{
    class Program
    {
        private static Regex reMatcher = new Regex("def main([(][)])? as int[:]?\\s*^\treturn\\s+(?<RC>\\d+)\\s*",RegexOptions.Compiled | RegexOptions.Multiline);

        private static string assembly_format = @"

; Example of making 32-bit PE program as raw code and data

format PE GUI
entry start

section '.text' code readable executable

  start:
        push    {0}
        call    [ExitProcess]

section '.idata' import data readable writeable

  dd 0,0,0,RVA kernel_name,RVA kernel_table
  dd 0,0,0,0,0

  kernel_table:
    ExitProcess dd RVA _ExitProcess
    dd 0

  kernel_name db 'KERNEL32.DLL',0

  _ExitProcess dw 0
    db 'ExitProcess',0

section '.reloc' fixups data readable discardable       ; needed for Win32s
                                                    ";


        public static class Tokens
        {

        }

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

                var match = reMatcher.Match(source);
                string returnCode = match.Groups["RC"].Value;
                System.IO.File.WriteAllText(assemblyFile, string.Format(assembly_format, returnCode));

                Process procCompiler = new Process();
                procCompiler.StartInfo = new ProcessStartInfo();
                procCompiler.StartInfo.FileName = @"c:\fasm\fasm.exe";
                procCompiler.StartInfo.Arguments = assemblyFile;
                procCompiler.Start();

                procCompiler.WaitForExit();
            }
            else
            {
                var rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace("file:","");
                Log("RootDir:" + rootDir);
                var validTestCasesPath = Path.Combine(rootDir, "TestFiles", "Valid");
                var invalidTestCasesPath = Path.Combine(rootDir, "TestFiles", "Invalid");

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
