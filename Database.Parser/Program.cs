using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Database.Parser
{
    static class Contract
    {
        const string NullContractError = "Must not be null";
        public static void NotNull(string value, string argName)
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
            _compiledRegEx =new Lazy<Regex>(
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
    }
}