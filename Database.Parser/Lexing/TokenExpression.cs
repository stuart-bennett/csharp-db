using System;
using System.Text.RegularExpressions;

namespace Database.Parser.Lexing
{
    using Utils;

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
}
