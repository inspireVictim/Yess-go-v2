using System;

namespace YessGoFront.Models;

/// <summary>
/// Модель отзыва о партнёре
/// </summary>
public class ReviewDto
{
    public int Id { get; set; }
    
    public int PartnerId { get; set; }
    
    /// <summary>
    /// Имя автора отзыва (опционально)
    /// </summary>
    public string? AuthorName { get; set; }
    
    /// <summary>
    /// Рейтинг от 1 до 5
    /// </summary>
    public int Rating { get; set; }
    
    /// <summary>
    /// Текст отзыва
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Дата создания отзыва
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

