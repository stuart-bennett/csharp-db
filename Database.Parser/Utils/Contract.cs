using System;

namespace Database.Parser.Utils
{
    static class Contract
    {
        const string NullContractError = "Must not be null";
        const string PositiveNumberContractError = "Must a positive number";
        public static void NotNull(object value, string argName)
        {
            if (value == null)
                throw new ArgumentException(NullContractError, argName);
        }

        public static void IsPositive(int value, string argName)
        {
            if (value < 0)
                throw new ArgumentException(PositiveNumberContractError, argName);
        }
    }
}
