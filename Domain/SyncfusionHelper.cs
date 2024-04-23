using Syncfusion.EJ2.Base;

namespace Master.Domain;

public record Property<T>(IEnumerable<T> Result, int Count);

public static class SyncfusionHelper
{
    public static List<SearchFilter> MapFieldsToDbColumnNames(this List<SearchFilter> searchFilters)
    {
        foreach (var searchFilter in searchFilters)
        {
            List<string> fields = new List<string>();
            foreach (var field in searchFilter.Fields)
            {
                fields.Add(field.ToUpperFirstChar());
            }
            searchFilter.Fields = fields;
        }

        return searchFilters;
    }


    public static List<WhereFilter> MapFieldsToDbColumnNames(this List<WhereFilter> whereFilters)
    {
        foreach (var whereFilter0 in whereFilters)
        {
            whereFilter0.Field = whereFilter0.Field?.ToUpperFirstChar();

            if (whereFilter0.predicates != null && whereFilter0.predicates.Any())
            {
                foreach (var whereFilter1 in whereFilter0.predicates)
                {
                    whereFilter1.Field = whereFilter1.Field?.ToUpperFirstChar();

                    if (whereFilter1.predicates != null && whereFilter1.predicates.Any())
                    {
                        foreach (var whereFilter2 in whereFilter1.predicates)
                        {
                            whereFilter2.Field = whereFilter2.Field?.ToUpperFirstChar();

                            if (whereFilter2.predicates != null && whereFilter2.predicates.Any())
                            {
                                foreach (var whereFilter3 in whereFilter2.predicates)
                                {
                                    whereFilter3.Field = whereFilter3.Field?.ToUpperFirstChar();

                                    if (whereFilter3.predicates != null && whereFilter3.predicates.Any())
                                    {
                                        foreach (var whereFilter4 in whereFilter2.predicates)
                                        {
                                            whereFilter4.Field = whereFilter4.Field?.ToUpperFirstChar();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return whereFilters;
    }

    private static string ToUpperFirstChar(this string input)
    {
        switch (input)
        {
            case null:
                throw new ArgumentNullException(nameof(input));
            case "":
                throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
            default:
                var firstChar = input[0].ToString().ToUpper();
                var asSpan = input.AsSpan(1).ToString();

                return string.Concat(firstChar, asSpan);
        }
    }
}

