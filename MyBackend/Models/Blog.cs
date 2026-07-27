using System;
using System.ComponentModel.DataAnnotations;

namespace MyBackend.Models
{
    public class Blog
    {
        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public int AuthorId { get; set; }
        public User Author { get; set; } = null!;
    }
}
