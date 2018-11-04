using System;
using System.Collections.Generic;
using System.Linq;

namespace Database.Parser
{
    using Lexing;
    using Utils;

    public sealed class Parser
    {
        // The token stream produced by the lexer
        private readonly IList<Token> _tokens;

        // Tracks the current position with the token stream
        private int _position;

        // Tracks individual characters in a token
        // Used for matching digit and letter symbols
        private int _subPosition;

        // The token at the current position in the token stream
        private Token _current;

        public Parser(IList<Token> tokens)
        {
            Contract.NotNull(tokens, nameof(tokens));
            _tokens = tokens;
            _position = -1;
            _subPosition = -1;

            // Moves to first token
            MoveNext();
        }

        // Move through current token one char at a time,
        // attempting to match common patterns
        public (bool match, bool end) ConsumeLetterOrDigit() => ConsumeSingle(Char.IsLetterOrDigit).Take(1).First();
        public (bool match, bool end) ConsumeLetter() => ConsumeSingle(Char.IsLetter).Take(1).First();
        public (bool match, bool end) ConsumeDigit() => ConsumeSingle(Char.IsDigit).Take(1).First();

        // When a production has several right-hand possibilities, pass them
        // all into @fns and if any match the result will be true.
        public bool Either(params Func<Parser, bool>[] fns)
        {
            (int, int, Token) checkPoint = Checkpoint();
            foreach (Func<Parser, bool> fn in fns)
            {
                if (fn(this)) return true;

                // we will have consumed tokens when trying to match
                // so we must go back to where we started when
                // attempting the other options.
                Restore(checkPoint);
            }

            // no matches
            return false;
        }

        // Entry point
        public bool Parse()
        {
            return Statement.Consume(this);
        }

        // Determines if the current token is a match to @value
        // If it is, _position and _current are mutated and the token
        // stream navigates to the next token
        public bool Terminal(string value)
        {
            if (!_current.Value.Equals(value))
            {
                return false;
            }

            MoveNext();
            return true;
        }

        // Like Terminal but will match any value in @values
        public bool Terminal(params string[] values)
        {
            if (!values.Any(_current.Value.Equals))
            {
                return false;
            }

            MoveNext();
            return true;
        }

        // Like terminal, will determine if current token matches byt
        // WON'T traverse the token stream
        public bool Peek(string value)
        {
            return _current.Value.Equals(value);
        }

        public bool Peek(params string[] values)
        {
            return values.Any(_current.Value.Equals);
        }

        // Consumes an individual character in the given token
        // Used for matching things that require either all digits, all letters
        // or a mixture of both by supplying @predicate
        private IEnumerable<(bool match, bool end)> ConsumeSingle(Func<char, bool> predicate)
        {
            while (++_subPosition < _current.Value.Length - 1)
            {
                char c = _current.Value[_subPosition];
                yield return (predicate(c), false);
            }

            bool finalChar = predicate(_current.Value[_subPosition]);
            _subPosition = -1;
            MoveNext();

            // signal end of token
            yield return (finalChar, true);
        }

        // Captures the current state within the token stream
        private (int position, int subPosition, Token token) Checkpoint() =>
            (_position, _subPosition, _current);

        private bool MoveNext()
        {
            if (_position + 1 < _tokens.Count())
            {
                _current = _tokens[++_position];
                return true;
            }
            return false;
        }

        // Puts the state of progress through the token stream back
        // according to the params. Use this in concert with Checkpoint()
        // when attempting several productions
        private void Restore((int, int, Token) values)
        {
            _position = values.Item1;
            _subPosition = values.Item2;
            _current = values.Item3;
        }
    }
}
