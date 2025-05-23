using System.Text.Json.Serialization;

namespace Read4All.Models
{
    //modele representant un livre dans l'application
    public class Book
    {
        [JsonPropertyName("livre_id")]
        //identifiant unique du livre
        public int Id { get; set; }

        [JsonPropertyName("titre")]
        //titre du livre
        public string Title { get; set; }

        [JsonPropertyName("imageCouverturePath")]
        //chemin vers l'image de couverture
        public string ImageCouverturePath { get; set; }

        [JsonPropertyName("livre")]
        //chemin vers le fichier EPUB
        public string Livre { get; set; }

        [JsonPropertyName("nbPage")]
        //nombre de pages du livre
        public int NbPage { get; set; }

        [JsonPropertyName("anneeEdition")]
        //annee d'edition du livre
        public int AnneeEdition { get; set; }

        [JsonPropertyName("resume")]
        //resume du livre
        public string Resume { get; set; }

        [JsonPropertyName("auteur")]
        //informations sur l'auteur
        public Auteur Auteur { get; set; }

        [JsonPropertyName("categorie")]
        //categorie (tag) du livre
        public Categorie Categorie { get; set; }

        [JsonPropertyName("utilisateur")]
        //utilisateur qui a ajoute le livre
        public Utilisateur Utilisateur { get; set; }

        //tag personnalise (non persiste)
        public string CustomTag { get; set; }

        //tag effectif (personnalise ou categorie)
        public string EffectiveTag => CustomTag ?? Categorie?.Libelle;
    }

    //modele representant un auteur
    public class Auteur
    {
        [JsonPropertyName("auteur_id")]
        //identifiant unique de l'auteur
        public int Id { get; set; }

        [JsonPropertyName("nom")]
        //nom de l'auteur
        public string Nom { get; set; }
    }

    //modele representant un utilisateur
    public class Utilisateur
    {
        [JsonPropertyName("utilisateur_id")]
        //identifiant unique de l'utilisateur
        public int Id { get; set; }

        [JsonPropertyName("username")]
        //nom d'utilisateur
        public string Username { get; set; }
    }

    //modele representant la reponse de l'api pour la liste des livres
    public class BooksResponse
    {
        [JsonPropertyName("message")]
        //message de la reponse
        public string Message { get; set; }

        [JsonPropertyName("data")]
        //liste des livres
        public List<Book> Data { get; set; }
    }
}
