namespace Seminar_1.Models.Entities
{
    public class Product
    {
        public Product()
        {
            Name = string.Empty;
            Description = string.Empty;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public string? ImagePath { get; set; }
    }
}
