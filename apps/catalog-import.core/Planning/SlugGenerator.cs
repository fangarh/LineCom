using System.Globalization;
using System.Text;

namespace LineCom.CatalogImport.Core.Planning;

public static class SlugGenerator
{
    private static readonly IReadOnlyDictionary<char, string> RussianTransliteration = new Dictionary<char, string>
    {
        ['а'] = "a",
        ['б'] = "b",
        ['в'] = "v",
        ['г'] = "g",
        ['д'] = "d",
        ['е'] = "e",
        ['ё'] = "e",
        ['ж'] = "zh",
        ['з'] = "z",
        ['и'] = "i",
        ['й'] = "y",
        ['к'] = "k",
        ['л'] = "l",
        ['м'] = "m",
        ['н'] = "n",
        ['о'] = "o",
        ['п'] = "p",
        ['р'] = "r",
        ['с'] = "s",
        ['т'] = "t",
        ['у'] = "u",
        ['ф'] = "f",
        ['х'] = "h",
        ['ц'] = "ts",
        ['ч'] = "ch",
        ['ш'] = "sh",
        ['щ'] = "sch",
        ['ъ'] = "",
        ['ы'] = "y",
        ['ь'] = "",
        ['э'] = "e",
        ['ю'] = "yu",
        ['я'] = "ya"
    };

    public static string CreateUniqueSlug(string value, ISet<string> usedSlugs)
    {
        ArgumentNullException.ThrowIfNull(usedSlugs);

        var baseSlug = CreateSlug(value);
        var slug = baseSlug;
        var suffix = 2;

        while (usedSlugs.Contains(slug))
        {
            slug = string.Create(CultureInfo.InvariantCulture, $"{baseSlug}-{suffix}");
            suffix++;
        }

        usedSlugs.Add(slug);
        return slug;
    }

    private static string CreateSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "item";
        }

        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = true;

        foreach (var original in value.Trim().ToLowerInvariant())
        {
            if (TryAppendAsciiToken(original, builder, ref previousWasSeparator))
            {
                continue;
            }

            if (RussianTransliteration.TryGetValue(original, out var transliterated))
            {
                builder.Append(transliterated);
                previousWasSeparator = transliterated.Length == 0 && previousWasSeparator;
                continue;
            }

            AppendSeparator(builder, ref previousWasSeparator);
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? "item" : slug;
    }

    private static bool TryAppendAsciiToken(char value, StringBuilder builder, ref bool previousWasSeparator)
    {
        if (value is >= 'a' and <= 'z' or >= '0' and <= '9')
        {
            builder.Append(value);
            previousWasSeparator = false;
            return true;
        }

        if (value <= sbyte.MaxValue)
        {
            AppendSeparator(builder, ref previousWasSeparator);
            return true;
        }

        return false;
    }

    private static void AppendSeparator(StringBuilder builder, ref bool previousWasSeparator)
    {
        if (!previousWasSeparator && builder.Length > 0)
        {
            builder.Append('-');
            previousWasSeparator = true;
        }
    }
}
