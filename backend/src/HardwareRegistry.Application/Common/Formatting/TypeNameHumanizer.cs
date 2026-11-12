using Humanizer;

namespace HardwareRegistry.Application.Common.Formatting
{
    public static class TypeNameHumanizer
    {
        extension(Type type)
        {
            public string Humanize(LetterCasing letterCasing = LetterCasing.Sentence)
                => type.Name
                    .Replace("Entity", "")
                    .Replace("Dto", "")
                    .Replace("Request", "")
                    .Replace("Response", "")
                    .Humanize(letterCasing);

            public string HumanizePlural(LetterCasing letterCasing = LetterCasing.Sentence)
                => type.Humanize(letterCasing).Pluralize();
        }
    }
}