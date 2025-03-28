namespace Entiteti;
public class Korisnik
{
    //dodatna provera kroz kod npr username == "admin", tako nesto
    public string? IdKorisnika { get; set; } 
    public required string Ime { get; set; } //ili/i username
    public required string Email { get; set; }
    public required string Sifra { get; set; } //hash vrednost sifre
    public required string Telefon { get; set; }
    public required string Adresa { get; set; }
}