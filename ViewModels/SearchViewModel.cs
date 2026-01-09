using System.ComponentModel.DataAnnotations;

namespace mist.ViewModels
{
    public class GameSearchViewModel
    {
        public string SearchTerm { get; set; }
        public List<int> TagIds { get; set; } = new List<int>();
        public string Developer { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortBy { get; set; } = "newest"; // newest, price-asc, price-desc, name
        public bool OnlyWithPromotions { get; set; }
    }

    public class LibrarySearchViewModel
    {
        public string SearchTerm { get; set; }
        public List<int> TagIds { get; set; } = new List<int>();
        public string SortBy { get; set; } = "recent"; // recent, name, tag
    }
}