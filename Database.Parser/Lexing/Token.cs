using System;

namespace Database.Parser.Lexing
{
    using Utils;

    public sealed class Token : IEquatable<Token>
    {
        public enum Type
        {
            Discard,
            Id,
            Int,
            Reserved,
            EOF
        }

        public Token(string value, Type tokenType)
        {
            Contract.NotNull(value, nameof(value));
            Value = value;
            TokenType = tokenType;
        }

        public string Value { get; }

        public Type TokenType { get; }

        public char ValueAt(int position)
        {
            Contract.IsPositive(position, nameof(position));
            if (TokenType == Type.EOF) return char.MinValue;
            return Value[position];
        }

        public bool Equals(Token other)
        {
            if (other == null) return false;
            return TokenType == other.TokenType &&
                Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString() => $"{{ Type: {TokenType}, Value: {Value} }}";
    }
}
