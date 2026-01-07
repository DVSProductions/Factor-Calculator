using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Dialog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace FactorCalculator;

public partial class MainViewModel : ObservableObject {
	[ObservableProperty]
	private ObservableCollection<Part> parts = [new() { ActualPrice = 2.28 }, new() { ActualPrice = 2 }, new() { ActualPrice = 11.66 }];


	[ObservableProperty]
	private ObservableCollection<Result> results = [];
	[ObservableProperty]
	double target = 35;
	[ObservableProperty]
	double min = 0.2;
	[ObservableProperty]
	double max = 3.0;
	[ObservableProperty]
	double stepSize = 0.01;
	int StepCount => (int)Math.Ceiling((Max - Min) / StepSize);

	[ObservableProperty]
	int updateProgress;

	[ObservableProperty]
	bool updating;
	[ObservableProperty]
	double maximumDistance = 1.0;

	[ObservableProperty]
	CalculationAlgorithm selectedAlgorithm = CalculationAlgorithm.MeetInTheMiddle;

	public CalculationAlgorithm[] AlgorithmOptions => Enum.GetValues<CalculationAlgorithm>();

	Timer previewUpdater = new() {
		Interval = 100,
		AutoReset = true,
	};

	[ObservableProperty]
	Result? selectedResult;
	[RelayCommand]
	private void AddComponent() {
		Parts.Add(new());
	}
	[RelayCommand]
	private void RemoveComponent() {
		Parts.RemoveAt(Parts.Count - 1);
	}

	public MainViewModel() {
	}

	[ObservableProperty]
	double progress;
	[ObservableProperty]
	bool isCalculating;
	[ObservableProperty]
	bool hasDownloadState;

	[RelayCommand]
	private async Task Calculate() {
		if(Parts.Select(x => x.ActualPrice).Sum() * Max < Target) {
			Results.Clear();
			await DialogHelper.CreateAlertDialog(
				new AlertDialogBuilderParams() {
					ContentHeader = "Zielpreis nicht erreichbar",
					WindowTitle = "Berechnungsfehler",
					DialogHeaderIcon = Material.Dialog.Icons.DialogIconKind.Error,
					SupportingText = $"Der Zielpreis ist mit den angegebenen Preisen und dem maximalen Faktor nicht erreichbar.\nDer h�chste erreichbare Wert ist {Parts.Select(x => x.ActualPrice).Sum() * Max}�",
					DialogButtons = [
						new DialogButton() {
							Content="OK",
							IsPositive=true }]
				}).Show();
			return;
		}
		if(Parts.Select(x => x.ActualPrice).Sum() * Min > Target) {
			Results.Clear();
			await DialogHelper.CreateAlertDialog(
				new AlertDialogBuilderParams() {
					ContentHeader = "Zielpreis immer erreichbar",
					WindowTitle = "Berechnungsfehler",
					DialogHeaderIcon = Material.Dialog.Icons.DialogIconKind.Error,
					SupportingText = $"Der Zielpreis ist mit den angegebenen Preisen und dem minimalen Faktor nicht erreichbar.\nDer niedrigste erreichbare Wert ist {Parts.Select(x => x.ActualPrice).Sum() * Min}�",
					DialogButtons = [
						new DialogButton() {
							Content="OK",
							IsPositive=true }]
				}).Show();
			return;
		}

		Results.Clear();
		IsCalculating = true;
		Progress = 0;

		var config = new FactorCalculationService.CalculationConfig(
			MinFactor: Min,
			MaxFactor: Max,
			StepSize: StepSize,
			Target: Target,
			MaximumDistance: MaximumDistance,
			Algorithm: SelectedAlgorithm
		);
		var prices = Parts.Select(x => x.ActualPrice).ToArray();

		void PreviewUpdater_Elapsed(object? sender, ElapsedEventArgs e) {
			if(Results.Count == 0)
				return;
			SelectedResult = Results.Last();
		}
		previewUpdater.Elapsed += PreviewUpdater_Elapsed;
		previewUpdater.Start();

		List<FactorCalculationService.CalculationResult>? serviceResults = null;
		await Task.Run(() => {
			serviceResults = FactorCalculationService.FindBestResults(
				prices,
				config,
				onProgress: progress => Dispatcher.UIThread.Post(() => Progress = progress),
				onResultFound: result => Dispatcher.UIThread.Post(() =>
					Results.Add(new Result([.. result.Factors], [.. result.AdjustedPrices], result.Total, result.Error))
				)
			);
		});

		previewUpdater.Stop();
		previewUpdater.Elapsed -= PreviewUpdater_Elapsed;
		PreviewUpdater_Elapsed(null, null!);
		IsCalculating = false;
		Dispatcher.UIThread.Post(() => {
			if(Results.Count > 0) {
				SelectedResult = Results.Last();
				VisualizeResult(Results.Last());
			}
		});
		if(Results.Count == 0)
			await DialogHelper.CreateAlertDialog(
				new AlertDialogBuilderParams() {
					ContentHeader = "Keine Ergebnisse",
					DialogHeaderIcon = Material.Dialog.Icons.DialogIconKind.Error,
					SupportingText = $"Es wurde keine exakte L�sung gefunden\nMit erh�hter Fehlertoleranz ist eventuell eine akzeptable L�sung m�glich",
					DialogButtons = [
						new DialogButton() {
							Content="OK",
							IsPositive=true }]
				}).Show();

	}
	[RelayCommand]
	private void VisualizeResult(Result bestResult) {
		for(var i = 0; i < Parts.Count; i++) {
			var part = Parts[i];
			part.Factor = bestResult.Factors[i];
		}
	}
}
