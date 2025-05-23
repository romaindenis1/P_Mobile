using System.Text.Json.Serialization;

namespace Read4All.Models;

//modele representant une categorie (tag) dans l'application
public class Categorie
{
    //identifiant unique de la categorie
    public int Id { get; set; }

    //nom de la categorie
    public string Libelle { get; set; }
} 