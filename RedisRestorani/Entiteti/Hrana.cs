namespace Entiteti;
public class Hrana
{
    //da li redis ima posebnu oznaku kljuca
    public string? IdHrane { get; set; } 
    public required string Naziv { get; set; }
    public required double Cena { get; set; } 
    public required string Opis { get; set; } //sastojci ili List<string>
    public required string Slika { get; set; }
    public required string Kategorija { get; set; } //burger, pica, dessert....
    //kad id, a kad restoran?
    public required string RestoranId { get; set; } // ID restorana kojem pripada
    //public required Restoran Restoran { get; set; }

    public required int Kolicina { get; set; } 

}