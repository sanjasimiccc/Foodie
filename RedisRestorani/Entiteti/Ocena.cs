namespace Entiteti;
public class Ocena
{
    public string? IdOcene { get; set; }
    public required string RestoranId { get; set; } 
    //public required Restoran Restoran { get; set; } 
    public required string KorisnikId { get; set; } 
   // public required Korisnik Korisnik { get; set; } 
    public required double Vrednost { get; set; } // Brojčana vrednost ocene (npr. 1-5)
}