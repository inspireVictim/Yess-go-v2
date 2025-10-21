// File: YessGoFront/Models/PartnersData.cs

using System;
using System.Collections.ObjectModel;

namespace YessGoFront.Models
{
    /// <summary>
    /// Модель элемента: логотип + (по желанию) имя и ссылка.
    /// Если тебе нужны только логотипы, можешь не заполнять Name/Url.
    /// </summary>
    public class PartnerLogoModel
    {
        public string Name { get; set; } = string.Empty;  // Например: "SIERRA Coffee"
        public string Logo { get; set; } = string.Empty;  // URL или локальный ресурс (resm:)
        public string Url { get; set; } = string.Empty;  // Сайт или ссылка на карты
    }

    /// <summary>
    /// Источник данных для трёх рядов логотипов.
    /// Привяжи PartnersRow1/2/3 к трем CollectionView в XAML.
    /// </summary>
    public class PartnersData
    {
        // Коллекции для биндинга в три ряда
        public ObservableCollection<PartnerLogoModel> PartnersRow1 { get; } = new();
        public ObservableCollection<PartnerLogoModel> PartnersRow2 { get; } = new();
        public ObservableCollection<PartnerLogoModel> PartnersRow3 { get; } = new();

        /// <summary>
        /// Заполняет три ряда популярными заведениями Бишкека.
        /// Логотипы сейчас — заглушки. Пришли свои PNG/SVG или пути к ресурсам — подставлю.
        /// </summary>
        public void LoadBishkekPartners()
        {
            // Row1 — Кафе
            PartnersRow1.Clear();
            PartnersRow1.Add(new() { Name = "SIERRA Coffee", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/sierra.png" });
            PartnersRow1.Add(new() { Name = "Ants", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/ants.png" });
            PartnersRow1.Add(new() { Name = "Bublik Cafe", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/bublik.png" });
            PartnersRow1.Add(new() { Name = "Flask Coffee", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/flask.png" });
            PartnersRow1.Add(new() { Name = "Biscuit Coffeeshop", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/biscuit.png" });
            PartnersRow1.Add(new() { Name = "Secret Garden", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/secretgarden.png" });

            // Row2 — Рестораны
            PartnersRow2.Clear();
            PartnersRow2.Add(new() { Name = "Supara (Ethno-Complex)", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/supara.png" });
            PartnersRow2.Add(new() { Name = "Navat", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/navat.png" });
            PartnersRow2.Add(new() { Name = "Faiza", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/faiza.png" });
            PartnersRow2.Add(new() { Name = "Chicken Star", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/chickenstar.png" });
            PartnersRow2.Add(new() { Name = "Furusato", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/furusato.png" });
            PartnersRow2.Add(new() { Name = "IWA Roof (Sheraton)", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/iwa.png" });

            // Row3 — Бары/пабы/клубы
            PartnersRow3.Clear();
            PartnersRow3.Add(new() { Name = "Save The Ales", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/savetheales.png" });
            PartnersRow3.Add(new() { Name = "Metro Pub", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/metro.png" });
            PartnersRow3.Add(new() { Name = "Promzona", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/promzona.png" });
            PartnersRow3.Add(new() { Name = "Teplo Bar", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/teplo.png" });
            PartnersRow3.Add(new() { Name = "Bar 12", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/bar12.png" });
            PartnersRow3.Add(new() { Name = "KladOFFka", Url = "https://maps.app.goo.gl/", Logo = "https://yourcdn/img/kladoffka.png" });
        }
    }
}
