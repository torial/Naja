using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

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
            string sourceFile = args[0];
            string path = Path.GetDirectoryName(sourceFile);
            string assemblyFile =Path.Combine(path, Path.GetFileNameWithoutExtension(sourceFile) + ".asm");

            string source = File.ReadAllText(sourceFile).Trim();
            Lexer lexer = new Lexer(source);

            var grammar = new Grammar();
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
    }
}
