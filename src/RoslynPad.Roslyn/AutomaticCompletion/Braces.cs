namespace RoslynPad.Roslyn.AutomaticCompletion
{
    internal static class Braces
    {
        public static class CurlyBrace
        {
            public const char OpenCharacter = '{';
            public const char CloseCharacter = '}';
        }

        public static class Parenthesis
        {
            public const char OpenCharacter = '(';
            public const char CloseCharacter = ')';
        }

        public static class Bracket
        {
            public const char OpenCharacter = '[';
            public const char CloseCharacter = ']';
        }

        public static class LessAndGreaterThan
        {
            public const char OpenCharacter = '<';
            public const char CloseCharacter = '>';
        }

        public static class DoubleQuote
        {
            public const char OpenCharacter = '"';
            public const char CloseCharacter = '"';
        }

        public static class SingleQuote
        {
            public const char OpenCharacter = '\'';
            public const char CloseCharacter = '\'';
        }
    }
}