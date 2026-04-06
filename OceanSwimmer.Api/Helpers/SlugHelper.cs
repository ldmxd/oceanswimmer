using System.Text.RegularExpressions;

namespace OceanSwimmer.Api.Helpers
{
    public static class SlugHelper
    {
        public static string GenerateSlug(string input)
        {
            var slug = input.ToLower();

            slug = Regex.Replace(slug, @"\|", "");
            slug = Regex.Replace(slug, @"[^\w\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");

            return slug.Trim('-');
        }
    }
}