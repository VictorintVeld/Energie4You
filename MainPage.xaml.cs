
namespace Energie4You;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnOpslaanClicked(object? sender, EventArgs e)
    {
        string naam = MonteurEntry.Text ?? "";
        string categorie = CategoriePicker.SelectedItem?.ToString() ?? "";
        string omschrijving = OmschrijvingEditor.Text ?? "";

        if (string.IsNullOrWhiteSpace(naam) ||
            string.IsNullOrWhiteSpace(categorie) ||
            string.IsNullOrWhiteSpace(omschrijving))
        {
            await DisplayAlertAsync(
                "Let op",
                "Vul alle velden in!",
                "OK");
            return;
        }

        await DisplayAlertAsync(
            "Melding",
            $"Monteur: {naam}\nCategorie: {categorie}\nOmschrijving: {omschrijving}",
            "OK");
    }
}
