using System;
using System.Collections.Generic;
using System.Linq;

/**
 *   Grammar is based on "SQL Minimum Grammar"
 *   https://docs.microsoft.com/en-us/sql/odbc/reference/appendixes/sql-minimum-grammar?view=sql-server-2017
 **/
namespace Database.Parser
{
    using Lexing;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SQL Parser Test Program");
            Console.Write("Enter a SQL command: ");
            string sql = Console.ReadLine();
            IEnumerable<Token> tokens = Lexer.Lex(sql);
            Console.WriteLine(
                string.Join(
                    Environment.NewLine,
                    tokens.Select(
                        t => string.Format("{0}: {1}", t.TokenType, t.Value))));

            Parser parser = new Parser(tokens.ToArray());
            (Ast ast, bool doesParse) = parser.Parse();
            Console.WriteLine($"Does parse? {doesParse}");
            Console.WriteLine($"AST: {ast}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}