using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/**
 *   Grammar is based on "SQL Minimum Grammar"
 *   https://docs.microsoft.com/en-us/sql/odbc/reference/appendixes/sql-minimum-grammar?view=sql-server-2017
 **/

namespace Database.Parser
{
    static class Contract
    {
        const string NullContractError = "Must not be null";
        public static void NotNull(object value, string argName)
        {
            if (value == null)
                throw new ArgumentException(NullContractError, argName);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SQL Parser Test Program");
            Console.Write("Enter a SQL command: ");
            string sql = Console.ReadLine();
            Console.WriteLine(string.Join(Environment.NewLine, Lexer
                .Lex(sql)
                .Select(t => string.Format("{0}: {1}", t.TokenType, t.Value))));
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }

    sealed class TokenExpression
    {
        private readonly Lazy<Regex> _compiledRegEx;
        private readonly Token.Type _tokenType;

        public TokenExpression(string pattern, Token.Type tokenType)
        {
            Contract.NotNull(pattern, nameof(pattern));

            _tokenType = tokenType;
            _compiledRegEx = new Lazy<Regex>(
                () => new Regex("^" + pattern, RegexOptions.Compiled));
        }

        public (bool match, Token token, string remaining) Test(string value)
        {
            Contract.NotNull(value, nameof(value));

            Match match = _compiledRegEx.Value.Match(value);
            if (!match.Success)
                return (false, null, value);

            return (true,
                new Token(match.Value, _tokenType),
                value.Substring(match.Index + match.Length));
        }
    }

    public sealed class TableIdentifier
    {
        public static Ast Consume(Parser parser)
        {
            return new Ast(
                new Operation(Operation.Type.TABLE_IDENTIFIER),
                UserDefinedName.Consume(parser));
        }
    }

    // user-defined-name ::= letter[digit | letter | _]...
    public sealed class UserDefinedName
    {
        public static Ast Consume(Parser parser)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(parser.ConsumeLetter().First());
            sb.Append(parser
                .ConsumeLetterOrDigit()
                .TakeWhile(x => x != default(char))
                .ToArray());

            return new Ast(
                new Operation(Operation.Type.USER_DEFINED_NAME, sb.ToString()));
        }
    }

    public sealed class TableName
    {
        public static Ast Consume(Parser parser)
        {
            return new Ast(
                new Operation(Operation.Type.TABLE_NAME),
                TableIdentifier.Consume(parser));
        }
    }

    // INSERT INTO table-name [( column-identifier [, column-identifier]...)] VALUES (insert-value[, insert-value]... )
    public sealed class InsertStatement
    {
        public static Ast Consume(Parser parser)
        {
            parser.Take("INSERT");
            parser.Take("INTO");
            return new Ast(
                new Operation(Operation.Type.INSERT_STATEMENT),
                TableName.Consume(parser));
        }
    }

    // select-statement ::=
    // SELECT [ALL | DISTINCT] select-list
    // FROM table-reference-list
    // [WHERE search-condition]
    // [order-by-clause]
    public sealed class SelectStatement
    {
        public static Ast Consume(Parser parser)
        {
            return new Ast(new Operation(Operation.Type.SELECT_STATEMENT), SelectSublist.Consume(parser));
        }
    }

    // select-list ::= * | select-sublist [, select-sublist]... (select-list cannot contain parameters.)
    public sealed class SelectList
    {
        public static Ast Consume(Parser parser)
        {
            return new Ast(new Operation(Operation.Type.SELECT_LIST), SelectSublist.Consume(parser));
        }
    }

    // select-sublist ::= expression
    public sealed class SelectSublist
    {
        public static Ast Consume(Parser parser)
        {
            return new Ast(new Operation(Operation.Type.SELECT_SUBLIST), Expression.Consume(parser));
        }
    }

    // expression ::= term | expression {+|–} term
    public sealed class Expression
    {
        public static Ast Consume(Parser parser)
        {
            return null;
            // return new Ast(new Operation(Operation.Type.EXPRESSION)
        }
    }

    public sealed class Parser
    {
        private readonly IList<Token> _tokens;
        private int _position;
        private int _subPosition;
        private Token _current;

        public Parser(IList<Token> tokens)
        {
            Contract.NotNull(tokens, nameof(tokens));
            _tokens = tokens;
            _position = -1;
            _subPosition = -1;
            MoveNext();
        }

        public IEnumerable<char> ConsumeLetterOrDigit() => ConsumeSingle(Char.IsLetterOrDigit);
        public IEnumerable<char> ConsumeLetter() => ConsumeSingle(Char.IsLetter);

        private IEnumerable<char> ConsumeSingle(Func<char, bool> predicate)
        {

            while (++_subPosition < _current.Value.Length)
            {
                char c = _current.Value[_subPosition];
                if (!predicate(c))
                {
                    throw new Exception();
                }

                yield return c;
            }

            _subPosition = -1;
            MoveNext();

            // signal end of token
            yield return default(char);
        }

        public Ast Parse()
        {
            return ParseStatement();
            // throw new Exception($"Unregonised token '{_current}'");
        }

        public void Take(string value)
        {
            if (!_current.Value.Equals(value))
            {
                throw new Exception();
            }

            MoveNext();
        }

        private Ast ParseStatement()
        {
            return InsertStatement.Consume(this);
            return SelectStatement.Consume(this);
        }

        private bool MoveNext()
        {
            if (_position + 1 < _tokens.Count())
            {
                _current = _tokens[++_position];
                return true;
            }

            return false;
        }
    }

    public class Ast
    {
        public Ast (Operation value, Ast left = null, Ast right = null)
        {
            Contract.NotNull(value, nameof(value));
            Value = value;
            Left = left;
            Right = right;
        }

        public Operation Value { get; }

        public Ast Left { get; }

        public Ast Right { get; }
    }

    public sealed class Operation
    {
        public Operation(Type type, string value = null)
        {
            T = type;
            Value = value;
        }

        public Type T { get; }

        public string Value { get; }

        public enum Type
        {
            INSERT_STATEMENT,
            SELECT_STATEMENT,
            SELECT_SUBLIST,
            EXPRESSION,
            TABLE_NAME,
            TABLE_IDENTIFIER,
            USER_DEFINED_NAME,
            SELECT_LIST
        }
    }

    public sealed class Lexer
    {
        // Order matters! Lex() will use the first matching pattern
        // so more general patterns should appear later
        private static readonly TokenExpression[] s_language = new TokenExpression[18]
        {
            new TokenExpression("\\s+", Token.Type.Discard),
            new TokenExpression("DELETE", Token.Type.Reserved),
            new TokenExpression("FROM", Token.Type.Reserved),
            new TokenExpression("INSERT", Token.Type.Reserved),
            new TokenExpression("INTO", Token.Type.Reserved),
            new TokenExpression("SELECT", Token.Type.Reserved),
            new TokenExpression("SET", Token.Type.Reserved),
            new TokenExpression("UPDATE", Token.Type.Reserved),
            new TokenExpression("WHERE", Token.Type.Reserved),
            new TokenExpression("VALUES", Token.Type.Reserved),
            new TokenExpression("\\(", Token.Type.Reserved),
            new TokenExpression("\\)", Token.Type.Reserved),
            new TokenExpression("\\[", Token.Type.Reserved),
            new TokenExpression("\\]", Token.Type.Reserved),
            new TokenExpression("=", Token.Type.Reserved),
            new TokenExpression("\\*", Token.Type.Id),
            new TokenExpression("[0-9]+", Token.Type.Int),
            new TokenExpression("[a-zA-z0-9]+", Token.Type.Id)
        };

        public static IEnumerable<Token> Lex(string queryText)
        {
            Contract.NotNull(queryText, nameof(queryText));
            ICollection<Token> tokens = new List<Token>();
            (bool isMatch, Token token, string remaining) =
                (false, null, queryText);
            do
            {
                isMatch = false;
                foreach (TokenExpression expr in s_language)
                {
                    (isMatch, token, remaining) = expr.Test(remaining);
                    if (isMatch)
                    {
                        if (token.TokenType != Token.Type.Discard)
                            tokens.Add(token);

                        // First matching expression wins
                        // Start again with remaining input
                        break;
                    }
                }

                if (!isMatch)
                    throw new Exception("Could not parse query text");
            } while (!string.IsNullOrEmpty(remaining));

            return tokens;
        }
    }

    public sealed class Token : IEquatable<Token>
    {
        public enum Type
        {
            Discard,
            Id,
            Int,
            Reserved
        }

        public Token(string value, Type tokenType)
        {
            Contract.NotNull(value, nameof(value));
            Value = value;
            TokenType = tokenType;
        }

        public string Value { get; }

        public Type TokenType { get; }

        public bool Equals(Token other)
        {
            if (other == null) return false;
            return TokenType == other.TokenType &&
                Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString() => $"{{ Type: {TokenType}, Value: {Value} }}";
    }
}