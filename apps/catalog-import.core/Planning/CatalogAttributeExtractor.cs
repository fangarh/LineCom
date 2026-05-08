using System.Text.RegularExpressions;

namespace LineCom.CatalogImport.Core.Planning;

internal static class CatalogAttributeExtractor
{
    private static readonly RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    public static IReadOnlyList<CatalogProductAttributeImportRow> Extract(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return [];
        }

        var attributes = new List<CatalogProductAttributeImportRow>();

        AddIfMatched(attributes, productName, "application", "Применение", 10, [
            Option("Наружная прокладка", "outdoor", 10, @"\b(outdoor|out)\b|наружн|улич|для\s+наружн"),
            Option("Внутренняя прокладка", "indoor", 20, @"\b(indoor|in)\b|внутрен")
        ]);

        AddIfMatched(attributes, productName, "construction", "Конструкция", 20, [
            Option("F/UTP", "f-utp", 10, @"\bf\s*/\s*utp\b|\bftp\d*\b"),
            Option("U/UTP", "u-utp", 20, @"\bu\s*/\s*utp\b|\butp\d*\b"),
            Option("Simplex", "simplex", 30, @"\bsimplex\b|симплекс"),
            Option("Duplex", "duplex", 40, @"\bduplex\b|дуплекс"),
            Option("RG-6", "rg-6", 50, @"\brg\s*-?\s*6\b"),
            Option("RG-59", "rg-59", 60, @"\brg\s*-?\s*59\b")
        ]);

        AddIfMatched(attributes, productName, "support-element", "Несущий элемент", 25, [
            Option("С тросом", "with-messenger", 10, @"трос|messenger")
        ]);

        AddIfMatched(attributes, productName, "conductor-material", "Материал проводника", 30, [
            Option("CU", "cu", 10, @"\bcu\b|медн|медь"),
            Option("CCA", "cca", 20, @"\bcca\b|омедненн\w*\s+алюм"),
            Option("CCS", "ccs", 30, @"\bccs\b|омедненн\w*\s+стал"),
            Option("AL", "al", 40, @"\bal\b|алюмин")
        ]);

        AddIfMatched(attributes, productName, "cable-category", "Категория кабеля", 40, [
            Option("Cat 5e", "cat-5e", 10, @"cat\.?\s*5e|cat\.?\s*5е"),
            Option("Cat 5", "cat-5", 20, @"cat\.?\s*5(?!\s*[eе])"),
            Option("Cat 6A", "cat-6a", 40, @"cat\.?\s*6a"),
            Option("Cat 6", "cat-6", 30, @"cat\.?\s*6")
        ]);

        AddIfMatched(attributes, productName, "jacket-material", "Материал оболочки", 50, [
            Option("нг(А)-LSLTx", "ng-a-lsltx", 10, @"нг\s*\(?а\)?\s*-\s*lsltx"),
            Option("LSLTx", "lsltx", 20, @"\blsltx\b"),
            Option("LSZH", "lszh", 30, @"\blszh\b"),
            Option("PVC", "pvc", 40, @"\bpvc\b"),
            Option("PE", "pe", 50, @"\bpe\b")
        ]);

        AddIfMatched(attributes, productName, "connector-type", "Тип разъема", 60, [
            Option("SC/APC", "sc-apc", 10, @"\bsc\s*/\s*apc\b"),
            Option("SC/UPC", "sc-upc", 20, @"\bsc\s*/\s*upc\b"),
            Option("LC/APC", "lc-apc", 30, @"\blc\s*/\s*apc\b"),
            Option("LC/UPC", "lc-upc", 40, @"\blc\s*/\s*upc\b"),
            Option("SC", "sc", 50, @"\bsc\b"),
            Option("LC", "lc", 60, @"\blc\b"),
            Option("FC", "fc", 70, @"\bfc\b"),
            Option("RJ-45", "rj-45", 80, @"\brj\s*-?\s*45\b|\b8p8c\b"),
            Option("RJ-12", "rj-12", 90, @"\brj\s*-?\s*12\b|\b6p6c\b"),
            Option("RJ-11", "rj-11", 100, @"\brj\s*-?\s*11\b"),
            Option("BNC", "bnc", 110, @"\bbnc\b"),
            Option("IEC C14", "iec-c14", 120, @"\biec\s*c14\b|\bc14\b")
        ]);

        AddIfMatched(attributes, productName, "fiber-type", "Тип волокна", 70, [
            Option("SM", "sm", 10, @"\bsm\b|single\s*mode|одномод"),
            Option("MM", "mm", 20, @"\bmm\b|multi\s*mode|многомод"),
            Option("OS2", "os2", 30, @"\bos2\b"),
            Option("OM3", "om3", 40, @"\bom3\b"),
            Option("OM4", "om4", 50, @"\bom4\b")
        ]);

        AddIfMatched(attributes, productName, "form-factor", "Форм-фактор", 80, [
            Option("SFP+", "sfp-plus", 10, @"\bsfp\+"),
            Option("SFP", "sfp", 20, @"\bsfp\b"),
            Option("DAC", "dac", 30, @"\bdac\b"),
            Option("WDM", "wdm", 40, @"\bwdm\b"),
            Option("1U", "1u", 50, @"\b1u\b"),
            Option("2U", "2u", 60, @"\b2u\b"),
            Option("3U", "3u", 70, @"\b3u\b"),
            Option("4U", "4u", 80, @"\b4u\b")
        ]);

        AddIfMatched(attributes, productName, "color", "Цвет", 90, [
            Option("Черный", "black", 10, @"черн|black"),
            Option("Серый", "gray", 20, @"сер|gray|grey"),
            Option("Белый", "white", 30, @"бел|white")
        ]);

        return attributes;
    }

    private static void AddIfMatched(
        List<CatalogProductAttributeImportRow> attributes,
        string productName,
        string code,
        string name,
        int sortOrder,
        IReadOnlyList<AttributeOptionPattern> options)
    {
        foreach (var option in options)
        {
            if (!Regex.IsMatch(productName, option.Pattern, Options))
            {
                continue;
            }

            attributes.Add(new CatalogProductAttributeImportRow(
                code,
                name,
                option.Value,
                option.Slug,
                option.Slug,
                sortOrder,
                option.SortOrder,
                IsSeoImportant: code is "application" or "conductor-material" or "cable-category",
                IsUsedInGeneratedName: code is "application" or "conductor-material" or "construction" or "cable-category"));
            return;
        }
    }

    private static AttributeOptionPattern Option(string value, string slug, int sortOrder, string pattern)
    {
        return new AttributeOptionPattern(value, slug, sortOrder, pattern);
    }

    private sealed record AttributeOptionPattern(string Value, string Slug, int SortOrder, string Pattern);
}
