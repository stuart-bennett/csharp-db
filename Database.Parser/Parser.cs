using System;
using System.Collections.Generic;
using System.Linq;

namespace Database.Parser
{
    using Lexing;
    using Utils;

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

        public (bool match, bool end) ConsumeLetterOrDigit() => ConsumeSingle(Char.IsLetterOrDigit).Take(1).First();
        public (bool match, bool end) ConsumeLetter() => ConsumeSingle(Char.IsLetter).Take(1).First();
        public (bool match, bool end) ConsumeDigit() => ConsumeSingle(Char.IsDigit).Take(1).First();
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

        public bool Either(params Func<Parser, bool>[] fns)
        {
            (int, int, Token) checkPoint = Checkpoint();
            foreach (Func<Parser, bool> fn in fns)
            {
                if (fn(this)) return true;
                Restore(checkPoint);
            }

            // no matches
            return false;
        }

        private (int position, int subPosition, Token token) Checkpoint() =>
            (_position, _subPosition, _current);


        private void Restore((int, int, Token) values)
        {
            _position = values.Item1;
            _subPosition = values.Item2;
            _current = values.Item3;
        }

        public bool Parse()
        {
            return Statement.Consume(this);
        }

        public bool Terminal(string value)
        {
            if (!_current.Value.Equals(value))
            {
                return false;
            }

            MoveNext();
            return true;
        }

        public bool Terminal(params string[] values)
        {
            if (!values.Any(_current.Value.Equals))
            {
                return false;
            }

            MoveNext();
            return true;
        }

        public bool Peek(string value)
        {
            return _current.Value.Equals(value);
        }

        public bool Peek(params string[] values)
        {
            return values.Any(_current.Value.Equals);
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
}
