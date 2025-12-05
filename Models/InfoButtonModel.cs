using System;

namespace YessGoFront.Models;

public class InfoButtonModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty; // Эмодзи или иконка
    public string ActionType { get; set; } = string.Empty; // Тип действия: "help", "topup", "transfer", "about", etc.
    public string? Route { get; set; } // Маршрут для навигации (опционально)
}

