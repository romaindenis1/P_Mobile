using System.Text.Json.Serialization;

namespace Read4All.Models
{
    public class Book
    {
        [JsonPropertyName("livre_id")]
        public int Id { get; set; }

        [JsonPropertyName("titre")]
        public string Title { get; set; }

        [JsonPropertyName("imageCouverturePath")]
        public string CoverImagePath { get; set; }

        [JsonPropertyName("nbPage")]
        public int NbPage { get; set; }

        [JsonPropertyName("anneeEdition")]
        public int AnneeEdition { get; set; }

        [JsonPropertyName("resume")]
        public string Resume { get; set; }

        [JsonPropertyName("auteur")]
        public Auteur Auteur { get; set; }

        [JsonPropertyName("categorie")]
        public Categorie Categorie { get; set; }

        [JsonPropertyName("utilisateur")]
        public Utilisateur Utilisateur { get; set; }
        public string CustomTag { get; set; }

        public string EffectiveTag => CustomTag ?? Categorie?.Libelle;
    }

    public class Auteur
    {
        [JsonPropertyName("auteur_id")]
        public int Id { get; set; }

        [JsonPropertyName("nom")]
        public string Nom { get; set; }
    }

    public class Categorie
    {
        [JsonPropertyName("categorie_id")]
        public int Id { get; set; }

        [JsonPropertyName("libelle")]
        public string Libelle { get; set; }
    }

    public class Utilisateur
    {
        [JsonPropertyName("utilisateur_id")]
        public int Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }
    }

    public class BooksResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public List<Book> Data { get; set; }
    }
}
