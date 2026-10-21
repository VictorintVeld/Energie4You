namespace Energie4You.Models;

public class DefectMelding
{
    public string NaamMonteur { get; set; } = "";
    public string Categorie { get; set; } = "";
    public string Omschrijving { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string FotoPad { get; set; } = "";
    public DateTime Datum { get; set; } = DateTime.Now;
}