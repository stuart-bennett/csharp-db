namespace Database.Parser
{
    public sealed class TableIdentifier
    {
        public static bool Consume(Parser parser)
        {
            return UserDefinedName.Consume(parser);
        }
    }

    // user-defined-name ::= letter[digit | letter | _]...
    public sealed class UserDefinedName
    {
        public static bool Consume(Parser parser)
        {
            Ast node = new Ast(Ast.NodeTypes.String, parser.CurrentValue);
            // must start with a letter
            (bool match, bool end) = parser.ConsumeLetter();
            if (!match) return false;
            while (!end)
            {
                (match, end) = parser.ConsumeLetterOrDigit();
                if (!match) return false;
            }

            parser.AddNode(node);
            return true;
        }
    }

    public sealed class TableName
    {
        public static bool Consume(Parser parser)
        {
            return TableIdentifier.Consume(parser);
        }
    }

    // INSERT INTO table-name [( column-identifier [, column-identifier]...)] VALUES (insert-value[, insert-value]... )
    public sealed class InsertStatement
    {
        public static bool Consume(Parser parser)
        {
            if (!parser.Terminal("INSERT")) return false;
            if (!parser.Terminal("INTO")) return false;
            if (!TableName.Consume(parser)) return false;
            if (parser.Peek("("))
            {
                parser.Terminal("(");
                if (!ColumnIdentifier.Consume(parser)) return false;
                while (parser.Peek(","))
                {
                    parser.Terminal(",");
                    if (!ColumnIdentifier.Consume(parser)) return false;
                }
                parser.Terminal(")");
            }

            if (!parser.Terminal("VALUES")) return false;
            if (!parser.Terminal("(")) return false;
            if (!InsertValue.Consume(parser)) return false;
            while (parser.Peek(","))
            {
                parser.Terminal(",");
                if (!InsertValue.Consume(parser)) return false;
            }
            return parser.Terminal(")");
        }
    }

    /*
    insert-value ::= dynamic-parameter
        | literal
        | NULL
        | USER
    */
    public sealed class InsertValue
    {
        public static bool Consume(Parser parser)
        {
            return parser.Either(
                Literal.Consume,
                SignedInteger.Consume);
        }
    }

    // EXTENSION TO SPEC
    public sealed class SignedInteger
    {
        public static bool Consume(Parser parser)
        {
            if (parser.Peek("+", "-"))
            {
                parser.Terminal("+", "-");
            }

            bool match, end = false;
            while (!end)
            {
                (match, end) = parser.ConsumeDigit();
                if (!match) return false;
            }

            return true;
        }
    }

    /* 
        character-string-literal ::= ''{character}...'' (character is any character
        in the character set of the driver/data source. To include a single literal
        quote character ('') in a character-string-literal, use two literal quote
        characters [''''].) 
    */
    public sealed class CharacterStringLiteral
    {
        public static bool Consume(Parser parser)
        {
            if (!parser.Terminal("\"")) return false;
            while (!parser.Peek("\""))
            {
                (bool match, _) = parser.ConsumeLetterOrDigit();
                if (!match) return false;
            }

            return parser.Terminal("\"");
        }
    }


    // literal ::= character-string-literal
    public sealed class Literal
    {
        public static bool Consume(Parser parser)
        {
            return CharacterStringLiteral.Consume(parser);
        }
    }

    // select-statement ::=
    // SELECT [ALL | DISTINCT] select-list
    // FROM table-reference-list
    // [WHERE search-condition]
    // [order-by-clause]
    public sealed class SelectStatement
    {
        public static bool Consume(Parser parser)
        {
            if (!parser.Terminal("SELECT")) return false;
            parser.AstDown(new Ast(Ast.NodeTypes.Select));
            if (!SelectList.Consume(parser)) return false;
            parser.AstUp();
            if (!parser.Terminal("FROM")) return false;
            parser.AstDown(new Ast(Ast.NodeTypes.TableName));
            return TableReferenceList.Consume(parser);
        }
    }

    // select-list ::= * | select-sublist [, select-sublist]... (select-list cannot contain parameters.)
    public sealed class SelectList
    {
        public static bool Consume(Parser parser)
        {
            return parser.Either(_1, _2);
        }

        private static bool _1(Parser parser)
        {
            if (!parser.Terminal("*")) return false;
            parser.AddNode(new Ast(Ast.NodeTypes.String, "*"));
            return true;
        }

        private static bool _2(Parser parser)
        {
            if (!SelectSublist.Consume(parser)) return false;
            while (parser.Peek(","))
            {
                parser.Terminal(",");
                if (!SelectSublist.Consume(parser)) return false;
            }

            return true;
        }
    }

    // table-reference-list ::= table-reference [,table-reference]...
    public sealed class TableReferenceList
    {
        public static bool Consume(Parser parser)
        {
            if (!TableReference.Consume(parser)) return false;
            while (parser.Peek(","))
            {
                parser.Terminal(",");
                if (!TableReference.Consume(parser)) return false;
            }

            return true;
        }
    }

    // table-reference ::= table-name
    public sealed class TableReference
    {
        public static bool Consume(Parser parser) => TableName.Consume(parser);
    }

    // select-sublist ::= expression
    public sealed class SelectSublist
    {
        public static bool Consume(Parser parser)
        {
            return Expression.Consume(parser);
        }
    }

    // expression ::= term | expression {+|–} term
    public sealed class Expression
    {
        public static bool Consume(Parser parser)
        {
            return Term.Consume(parser);
        }
    }

    // factor ::= [+|–]primary
    public sealed class Factor
    {
        public static bool Consume(Parser parser)
        {
            return Primary.Consume(parser);
        }
    }

    // primary ::= column-name
    public sealed class Primary
    {
        public static bool Consume(Parser parser)
        {
            return ColumnName.Consume(parser);
        }
    }

    // column-name ::= [table-name.]column-identifier
    public sealed class ColumnName
    {
        public static bool Consume(Parser parser)
        {
            return parser.Either(_1, _2);
        }

        private static bool _1(Parser parser)
        {
            // With the table name prefix
            if (!parser.Terminal("[")) return false;
            if (!TableName.Consume(parser)) return false;
            if (!parser.Terminal("}")) return false;
            return parser.Terminal(".") && ColumnIdentifier.Consume(parser);
        }

        private static bool _2(Parser parser)
        {
            // without the table name prefix
            if (!ColumnIdentifier.Consume(parser)) return false;
            return true;
        }
    }

    // column-identifier ::= user-defined-name
    public sealed class ColumnIdentifier
    {
        public static bool Consume(Parser parser)
        {
            return UserDefinedName.Consume(parser);
        }
    }

    // term ::= factor | term {*|/} factor
    public sealed class Term
    {
        public static bool Consume(Parser parser)
        {
            return Factor.Consume(parser);
        }
    }

    /*  
        statement ::= create-table-statement
            | delete-statement-searched
            | drop-table-statement
            | insert-statement
            | select-statement
            | update-statement-searched
    */
    public sealed class Statement
    {
        public static bool Consume(Parser parser)
        {
            return parser.Either(_1, _2);
        }

        private static bool _1(Parser parser)
        {
            return InsertStatement.Consume(parser);
        }

        private static bool _2(Parser parser)
        {
            return SelectStatement.Consume(parser);
        }
    }

}
