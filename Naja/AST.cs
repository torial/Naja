using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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

        public bool Exists(Predicate<ASTNode> matcher)
        {
            var exists = Children.Exists(matcher);
            if (exists)
                return true;

            foreach(var child in Children)
            {
                exists = child.Exists(matcher);
                if (exists)
                    return true;
            }

            return false;
        }

        public ASTNode Find(Predicate<ASTNode> matcher)
        {
            var exists = Children.Find(matcher);
            if (exists !=null)
                return exists;

            foreach (var child in Children)
            {
                exists = child.Find(matcher);
                if (exists!=null)
                    return exists;
            }

            return  null;

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
