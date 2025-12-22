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
					SupportingText = $"Der Zielpreis ist mit den angegebenen Preisen und dem maximalen Faktor nicht erreichbar.\nDer höchste erreichbare Wert ist {Parts.Select(x => x.ActualPrice).Sum() * Max}€",
					DialogButtons = [
						new DialogButton() {
							Content="OK",
							IsPositive=true }]
				}).Show();
			return;
		}
		if(Parts.Select(x => x.ActualPrice).Sum() * Min < Target) {
			Results.Clear();
			await DialogHelper.CreateAlertDialog(
				new AlertDialogBuilderParams() {
					ContentHeader = "Zielpreis immer erreichbar",
					WindowTitle = "Berechnungsfehler",
					DialogHeaderIcon = Material.Dialog.Icons.DialogIconKind.Error,
					SupportingText = $"Der Zielpreis ist mit den angegebenen Preisen und dem minimalen Faktor immer erreichbar.\nDer niedrigste erreichbare Wert ist {Parts.Select(x => x.ActualPrice).Sum() * Min}€",
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
		var stepCount = StepCount;
		IEnumerable<IEnumerable<double>> GetSub(Memory<Part> parts) {
			if(parts.Length == 1) {
				for(var n = 0; n < stepCount; n++) {
					yield return new[] { Min + StepSize * n };
				}
			}
			else {
				for(var n = 0; n < stepCount; n++) {
					var current = Min + StepSize * n;
					foreach(var value in GetSub(parts[1..])) {
						yield return value.Prepend(current);
					}
				}
			}
		}
		var arr = Parts.ToArray();
		var closestDistance = double.PositiveInfinity;
		var totalRuns = (int)Math.Pow(StepCount, Parts.Count);
		var currentRun = 0;
		void PreviewUpdater_Elapsed(object? sender, ElapsedEventArgs e) {
			Progress = currentRun / (double)totalRuns;
			if(Results.Count == 0)
				return;
			SelectedResult = Results.Last();
			//VisualizeResult(bestResult);
		}
		previewUpdater.Elapsed += PreviewUpdater_Elapsed;
		previewUpdater.Start();
		var target = Target;
		await Task.Run(() => {
			foreach(var combo in GetSub(arr.AsMemory())) {
				currentRun++;
				var factors = combo.Select(x => Math.Round(x, 2));
				var adjustedPrices = factors.Select((x, i) => arr[i].ActualPrice * x).Select(x => Math.Round(x, 2));
				var sum = adjustedPrices.Sum();
				var distance = target - sum;
				var absDistance = Math.Abs(distance);
				if(absDistance < closestDistance && absDistance < maximumDistance || absDistance < 0.01) {
					closestDistance = absDistance;
					Dispatcher.UIThread.Post(() => Results.Add(new Result([.. factors], [.. adjustedPrices], sum, distance)));
				}
			}
		});
		if(closestDistance < 0.01) {
			var selection = Results.Where(x => x.Error > 0.01).ToArray();
			Dispatcher.UIThread.Invoke(() => {
				foreach(var toremove in selection) {
					Results.Remove(toremove);
				}
			});
		}
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
					SupportingText = $"Es wurde keine exakte Lösung gefunden\nMit erhöhter Fehlertoleranz ist eventuell eine akzeptable Lösung möglich",
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
