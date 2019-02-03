using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Database.Parser
{
    using Lexing;
    using Utils;

    public class Ast
    {
        public enum NodeTypes
        {
            Root,
            Select,
            TableName,
            String,
            Int
        }

        public Ast(NodeTypes node, string value = "")
        {
            Node = node;
            Value = value;
            Children = new List<Ast>();
        }

        public Ast(NodeTypes node, IList<Ast> children, string value = "")
        {
            Contract.NotNull(children, nameof(children));
            Node = node;
            Value = value;
            Children = children;
        }

        public NodeTypes Node { get; }

        public string Value { get; }

        public IList<Ast> Children { get; }

        public string ToString(int indentLevel)
        {

            StringBuilder sb = new StringBuilder($"{Node} ({Value})");
            sb.AppendLine();
            foreach (Ast child in Children)
            {
                sb.AppendFormat("{0}-{1}", string.Join(string.Empty, Enumerable.Repeat("  ", indentLevel)), child.ToString(indentLevel + 1));
            }

            return sb.ToString();
        }

        public override string ToString() => ToString(0);
    }

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

        // The abstract syntax that is built as parsing takes place
        private Ast _ast;

        // Tracks the parent node of _ast (null if it's the root)
        private Ast _astParentNode;

        public Parser(IList<Token> tokens)
        {
            Contract.NotNull(tokens, nameof(tokens));
            _tokens = tokens;
            _position = -1;
            _subPosition = -1;

            // Moves to first token
            MoveNext();
            _ast = new Ast(Ast.NodeTypes.Root);
            _astParentNode = null;
        }

        public string CurrentValue => _current.Value;

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
        public (Ast, bool) Parse()
        {
            return (_ast, Statement.Consume(this));
        }

        public void AstUp()
        {
            if (_astParentNode == null)
                throw new InvalidOperationException();

            _ast = _astParentNode;
        }

        public void AstDown(Ast astNode)
        {
            _ast.Children.Add(astNode);
            _astParentNode = _ast;
            _ast = astNode;
        }

        public void AddNode(Ast astNode)
        {
            _ast.Children.Add(astNode);
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

        // Like terminal, will determine if current token matches but
        // WON'T traverse the token stream
        public bool Peek(string value)
        {
            // we know we're at the end of token stream so can't match anything
            if (_current.TokenType == Token.Type.EOF) return false;
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
                char c = _current.ValueAt(_subPosition);
                yield return (predicate(c), false);
            }

            bool finalChar = predicate(_current.ValueAt(_subPosition));
            _subPosition = -1;
            MoveNext();

            // signal end of token
            yield return (finalChar, true);
        }

        // Captures the current state within the token stream
        private (int position, int subPosition, Token token) Checkpoint() =>
            (_position, _subPosition, _current);

        private void MoveNext()
        {
            if (_position + 1 < _tokens.Count())
            {
                _current = _tokens[++_position];
                return;
            }

            _current = new Token(string.Empty, Token.Type.EOF);
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
