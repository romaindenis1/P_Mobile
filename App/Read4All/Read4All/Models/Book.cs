using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Read4All.Models
{
    public class Book
    {
        public int LivreId { get; set; }
        public string? Titre { get; set; }
        public string? ImageCouverturePath { get; set; }
        public int NbPage { get; set; }
        public int AnneeEdition { get; set; }
        public string? Resume { get; set; }
        public Author? Author{ get; set; }
        public Category? Categorie { get; set; }
    }
}
