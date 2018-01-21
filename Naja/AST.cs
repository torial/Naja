using System;
using System.Collections.Generic;
using System.Text;

namespace Naja
{
    class ASTNode : Lexeme
    {
        public List<ASTNode> Children;

        public ASTNode(string type):this(type, type)
        {

        }
        public ASTNode(Lexeme lexeme):this(lexeme.Type, lexeme.Text)
        {
        }
        public ASTNode(string type, string text)
        {
            this.Text = text;
            this.Type = type;
            Children = new List<ASTNode>();
        }

        public string Prettify(int depth = 0)
        {
            var baseText = Type == Text ? Type : $"{Type}<{Text.Trim()}>";
            StringBuilder sbResult = new StringBuilder("".PadRight(depth,'\t'));
            sbResult.Append(baseText);
            sbResult.AppendLine();
            foreach(var child in Children)
            {
                sbResult.Append(child.Prettify(depth + 1));
            }
            return sbResult.ToString();
        }
    }

}
