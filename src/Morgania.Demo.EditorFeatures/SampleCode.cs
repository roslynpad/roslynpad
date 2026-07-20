namespace Morgania.Demo.EditorFeatures;

internal static class SampleCode
{
    public const string Text = """
        using System;
        using System.Collections.Generic;
        using System.Linq;

        namespace Demo;

        /// <summary>A small playground exercising Roslyn language services.</summary>
        public class Greeter(string name)
        {
            private const int Repeat = 3;

            public string Name { get; } = name;

            public void Greet()
            {
                for (var i = 0; i < Repeat; i++)
                {
                    Console.WriteLine($"Hello, {Name}! ({i + 1} of {Repeat})");
                }

                var report = new
                {
                    Item = Name,
                };
                Console.WriteLine(report);
            }

            public static IEnumerable<int> Evens(IEnumerable<int> numbers) =>
                numbers.Where(static n => n % 2 == 0).OrderBy(static n => n);
        }
        """;
}
