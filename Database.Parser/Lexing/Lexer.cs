using System;
using System.Collections.Generic;

namespace Database.Parser.Lexing
{
    using Utils;

    public sealed class Lexer
    {
        // Order matters! Lex() will use the first matching pattern
        // so more general patterns should appear later
        private static readonly TokenExpression[] s_language = new TokenExpression[22]
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
            new TokenExpression("\\+", Token.Type.Reserved),
            new TokenExpression("-", Token.Type.Reserved),
            new TokenExpression("=", Token.Type.Reserved),
            new TokenExpression(",", Token.Type.Reserved),
            new TokenExpression("\"", Token.Type.Reserved),
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
}
