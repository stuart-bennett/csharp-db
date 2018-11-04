using System;

namespace Database.Parser.Utils
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
}
