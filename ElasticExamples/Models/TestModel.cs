namespace ElasticExamples.Models
{
    public class TestModel
    {
        internal static string IndexName => "test-index";

        public int Id { get; set; }
        public string NameSuggestionField { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
