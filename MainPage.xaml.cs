using Microsoft.Maui.Media;
using Microsoft.Maui.Devices.Sensors;
using Energie4You.Models;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel.Communication;
namespace Energie4You;

public partial class MainPage : ContentPage
{
	private double? latitude;
	private double? longitude;
	private string? fotoPad;
	private DefectMelding? laatstOpgeslagenMelding;


	public MainPage()
	{
		InitializeComponent();

		SizeChanged += OnPageSizeChanged;
	}

	private void OnPageSizeChanged(object? sender, EventArgs e)
	{
		bool landscape = Width > Height;

		if (Width <= 0 || Height <= 0)
			return;

		if (landscape)
		{
			FormulierGrid.ColumnDefinitions =
				new ColumnDefinitionCollection
				{
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Star)
				};

			FormulierGrid.RowDefinitions =
				new RowDefinitionCollection
				{
				new RowDefinition(GridLength.Auto)
				};

			Grid.SetRow(FotoGpsKolom, 0);
			Grid.SetColumn(FotoGpsKolom, 1);
		}
		else
		{
			FormulierGrid.ColumnDefinitions =
				new ColumnDefinitionCollection
				{
				new ColumnDefinition(GridLength.Star)
				};

			FormulierGrid.RowDefinitions =
				new RowDefinitionCollection
				{
				new RowDefinition(GridLength.Auto),
				new RowDefinition(GridLength.Auto)
				};

			Grid.SetRow(FotoGpsKolom, 1);
			Grid.SetColumn(FotoGpsKolom, 0);
		}
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

		laatstOpgeslagenMelding = melding;
		EmailButton.IsEnabled = true;

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



	private async void OnEmailClicked(object? sender, EventArgs e)
	{
		if (laatstOpgeslagenMelding == null)
		{
			await DisplayAlertAsync(
				"Let op",
				"Er is nog geen melding opgeslagen.",
				"OK");
			return;
		}

		try
		{
			if (!Email.Default.IsComposeSupported)
			{
				await DisplayAlertAsync(
					"E-mail",
					"Er is geen geschikte e-mailapp beschikbaar.",
					"OK");
				return;
			}

			var melding = laatstOpgeslagenMelding;

			if (melding == null)
			{
				return;
			}

			string onderwerp = $"Defectmelding Energie4You - {melding.Categorie}";

			string bericht =
				$"Monteur: {melding.NaamMonteur}\n" +
				$"Categorie: {melding.Categorie}\n" +
				$"Omschrijving: {melding.Omschrijving}\n\n" +
				$"GPS-locatie:\n" +
				$"Latitude: {melding.Latitude}\n" +
				$"Longitude: {melding.Longitude}";

			var email = new EmailMessage
			{
				Subject = onderwerp,
				Body = bericht
			};

			if (!File.Exists(melding.FotoPad))
			{
				await DisplayAlertAsync(
					"Foto ontbreekt",
					"De opgeslagen foto is niet gevonden.",
					"OK");
				return;
			}

			email.Attachments ??= new List<EmailAttachment>();

			email.Attachments.Add(
				new EmailAttachment(melding.FotoPad));

			await Email.Default.ComposeAsync(email);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync(
				"E-mailfout",
				$"De e-mailapp kon niet worden geopend: {ex.Message}",
				"OK");
		}
	}
}
