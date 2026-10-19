using Microsoft.Maui.Media;
using Microsoft.Maui.Devices.Sensors;
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

	private async void OnFotoMakenClicked(object? sender, EventArgs e)
	{
		try
		{
			if (!MediaPicker.Default.IsCaptureSupported)
			{
				await DisplayAlertAsync(
					"Camera",
					"Dit apparaat ondersteunt geen camera.",
					"OK");
				return;
			}

			var foto = await MediaPicker.Default.CapturePhotoAsync();

			if (foto == null)
				return;

			string bestandspad = Path.Combine(
				FileSystem.AppDataDirectory,
				$"{Guid.NewGuid()}.jpg");

			using (var bron = await foto.OpenReadAsync())
			using (var bestemming = File.Create(bestandspad))
			{
				await bron.CopyToAsync(bestemming);
			}

			FotoPreview.Source = ImageSource.FromFile(bestandspad);
			FotoPreview.IsVisible = true;
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync(
				"Fout",
				$"Er kon geen foto worden gemaakt: {ex.Message}",
				"OK");
		}
	}


	private async void OnGpsClicked(object? sender, EventArgs e)
	{
		LocatieLabel.Text = "GPS-locatie wordt opgehaald...";

		try
		{
			var toestemming = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

			if (toestemming != PermissionStatus.Granted)
			{
				LocatieLabel.Text = "Geen toestemming voor GPS.";
				return;
			}

			var locatie = await Geolocation.Default.GetLocationAsync();

			if (locatie != null)
			{
				LocatieLabel.Text = $"Latitude: {locatie.Latitude}\nLongitude: {locatie.Longitude}";
			}
			else
			{
				LocatieLabel.Text = "Geen GPS-locatie gevonden.";
			}
		}
		catch (Exception ex)
		{
			LocatieLabel.Text = "Er is iets misgegaan met de GPS.";

			await DisplayAlertAsync(
				"GPS-fout",
				ex.Message,
				"OK");
		}
	}


}
