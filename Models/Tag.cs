using System.ComponentModel.DataAnnotations;

namespace mist.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa tagu jest wymagana")]
        [StringLength(50, ErrorMessage = "Nazwa tagu może mieć maksymalnie 50 znaków")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        // Relacja wiele-do-wielu z Game
        public virtual ICollection<GameTag> GameTags { get; set; } = new List<GameTag>();
    }

    // Tabela łącząca dla relacji wiele-do-wielu
    public class GameTag
    {
        public int GameId { get; set; }
        public virtual Game Game { get; set; }

        public int TagId { get; set; }
        public virtual Tag Tag { get; set; }
    }
}
