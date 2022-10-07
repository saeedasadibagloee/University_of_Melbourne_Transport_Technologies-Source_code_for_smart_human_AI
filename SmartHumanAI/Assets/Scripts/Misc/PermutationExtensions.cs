using System;
using System.Linq;

internal static class PermutationExtensions
{
    public static int[] GetPermutations(this int self)
    {
        var digits = self.ToString().Select(digit => digit.ToString());
        int count = digits.Count();
        var permutations = digits;
        while (0 < --count)
        {
            permutations = permutations.Join(digits, permutation => true, digit => true, (permutation, digit) => permutation.Contains(digit) ? null : string.Concat(permutation, digit)).Where(permutation => permutation != null);
        }
        return permutations.Select(permutation => Convert.ToInt32(permutation)).OrderBy(permutation => permutation).ToArray();
    }

    public static int[] FindPermutations(this int[] self)
    {
        if (self == null)
        {
            return null;
        }

        return self.SelectMany(number =>
        {
            var permutations = self.Intersect(number.GetPermutations());
            if (permutations.Count() > 1)
            {
                return permutations;
            }
            return Enumerable.Empty<int>();
        }).Distinct().ToArray();
    }
}
