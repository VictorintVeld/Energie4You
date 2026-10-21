using Microsoft.Maui.Media;
using Microsoft.Maui.Devices.Sensors;
using Energie4You.Models;
using System.Text.Json;
namespace Energie4You;

public partial class MainPage : ContentPage
{
	private double? latitude;
	private double? longitude;
	private string? fotoPad;

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

		if (string.IsNullOrEmpty(fotoPad))
		{
			await DisplayAlertAsync(
				"Let op",
				"Maak eerst een foto van het defect.",
				"OK");
			return;
		}

		if (latitude == null || longitude == null)
		{
			await DisplayAlertAsync(
				"Let op",
				"Haal eerst de GPS-locatie op.",
				"OK");
			return;
		}

		DefectMelding melding = new DefectMelding();
		melding.NaamMonteur = naam;
		melding.Categorie = categorie;
		melding.Omschrijving = omschrijving;
		melding.Latitude = latitude;
		melding.Longitude = longitude;
		melding.FotoPad = fotoPad;

		string json = JsonSerializer.Serialize(melding);

		string bestandsnaam = $"{Guid.NewGuid()}.json";

		string opslagmap = Path.Combine(
			FileSystem.AppDataDirectory,
			"Meldingen");

		Directory.CreateDirectory(opslagmap);

		string volledigPad = Path.Combine(opslagmap, bestandsnaam);

		await File.WriteAllTextAsync(volledigPad, json);

		await DisplayAlertAsync(
			"Opgeslagen!",
			$"De defectmelding is succesvol opgeslagen.\n\nBestand: {bestandsnaam}",
			"OK");

		MonteurEntry.Text = "";
		CategoriePicker.SelectedIndex = -1;
		OmschrijvingEditor.Text = "";

		latitude = null;
		longitude = null;
		fotoPad = null;

		LocatieLabel.Text = "Nog geen locatie opgehaald";
		FotoPreview.Source = null;
		FotoPreview.IsVisible = false;

		await DisplayAlertAsync(
			"Controle",
			"De resetcode is uitgevoerd.",
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

			fotoPad = bestandspad;

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
		latitude = null;
		longitude = null;
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
				latitude = locatie.Latitude;
				longitude = locatie.Longitude;

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
