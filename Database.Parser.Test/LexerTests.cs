using System.Linq;
using Xunit;

namespace Database.Parser.Test
{
    public class LexerTests
    {
        [Fact]
        public void IdentifiesTokenTypesInSelectExpression()
        {
            const string QueryText = "SELECT * FROM TestTable";
            const int ExpectedTokenCount = 4;
            Token[] result = Lexer.Lex(QueryText).ToArray();
            Assert.Equal(ExpectedTokenCount, result.Length);
            Assert.Equal(TokenHelpers.Reserved("SELECT"), result[0]);
            Assert.Equal(TokenHelpers.Id("*"), result[1]);
        }

        [Fact]
        public void IdentifiesTokenTypesInInsertExpression()
        {
            const string QueryText = "INSERT INTO TestTable (Id) VALUES (1)";
            const int ExpectedTokenCount = 10;
            Token[] result = Lexer.Lex(QueryText).ToArray();
            Assert.Equal(ExpectedTokenCount, result.Length);
            Assert.Equal(TokenHelpers.Reserved("INSERT"), result[0]);
            Assert.Equal(TokenHelpers.Reserved("INTO"), result[1]);
            Assert.Equal(TokenHelpers.Id("TestTable"), result[2]);
            Assert.Equal(TokenHelpers.Reserved("("), result[3]);
            Assert.Equal(TokenHelpers.Id("Id"), result[4]);
            Assert.Equal(TokenHelpers.Reserved(")"), result[5]);
            Assert.Equal(TokenHelpers.Reserved("VALUES"), result[6]); 
            Assert.Equal(TokenHelpers.Reserved("("), result[7]); 
            Assert.Equal(TokenHelpers.Int("1"), result[8]); 
            Assert.Equal(TokenHelpers.Reserved(")"), result[9]); 
        }

        [Fact]
        public void IdentifiesTokenTypesInDeleteExpression()
        {
            const string QueryText = "DELETE FROM TestTable";
            const int ExpectedTokenCount = 3;
            Token[] result = Lexer.Lex(QueryText).ToArray();
            Assert.Equal(ExpectedTokenCount, result.Length);
            Assert.Equal(TokenHelpers.Reserved("DELETE"), result[0]);
            Assert.Equal(TokenHelpers.Reserved("FROM"), result[1]);
            Assert.Equal(TokenHelpers.Id("TestTable"), result[2]);
        }

        [Fact]
        public void IdentifiesTokenTypesInUpdateExpression()
        {
            const string QueryText = "UPDATE TestTable SET Id = 101 WHERE Id = 1";
            const int ExpectedTokenCount = 10;
            Token[] result = Lexer.Lex(QueryText).ToArray();
            Assert.Equal(ExpectedTokenCount, result.Length);
            Assert.Equal(TokenHelpers.Reserved("UPDATE"), result[0]);
            Assert.Equal(TokenHelpers.Id("TestTable"), result[1]);
            Assert.Equal(TokenHelpers.Reserved("SET"), result[2]);
            Assert.Equal(TokenHelpers.Id("Id"), result[3]);
            Assert.Equal(TokenHelpers.Reserved("="), result[4]);
            Assert.Equal(TokenHelpers.Int("101"), result[5]);
            Assert.Equal(TokenHelpers.Reserved("WHERE"), result[6]);
            Assert.Equal(TokenHelpers.Id("Id"), result[7]);
            Assert.Equal(TokenHelpers.Reserved("="), result[8]);
            Assert.Equal(TokenHelpers.Int("1"), result[9]);
        }
    }

    public static class TokenHelpers
    {
        public static Token Reserved(string value) =>
            new Token(value, Token.Type.Reserved);

        public static Token Id(string value) =>
            new Token(value, Token.Type.Id);

        public static Token Int(string value) =>
            new Token(value, Token.Type.Int);
    }
}
