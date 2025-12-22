using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
		Results.Clear();
		IsCalculating = true;
		Progress = 0;
		int stepCount = this.StepCount;
		IEnumerable<IEnumerable<double>> GetSub(Memory<Part> parts) {
			if(parts.Length == 1) {
				for(int n = 0; n < stepCount; n++) {
					yield return new[] { Min + StepSize * n };
				}
			}
			else {
				for(int n = 0; n < stepCount; n++) {
					var current = Min + StepSize * n;
					foreach(var value in GetSub(parts[1..])) {
						yield return value.Prepend(current);
					}
				}
			}
		}
		var arr = Parts.ToArray();
		var bestV = double.PositiveInfinity;
		var totalRuns = (int)Math.Pow(StepCount, Parts.Count);
		var currentRun = 0;
		void PreviewUpdater_Elapsed(object? sender, ElapsedEventArgs e) {
			Progress = (double)currentRun / (double)totalRuns;
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
				var fixedPrecision = combo.Select(x => Math.Round(x,2));
				var variant = fixedPrecision.Select((x, i) => arr[i].ActualPrice * x).Select(x => Math.Round(x,2));
				var sum = variant.Sum();
				var distance = target - sum;
				var absDistance = Math.Abs(distance);
				if(absDistance < bestV && absDistance < 1) {
					bestV = absDistance;
					Dispatcher.UIThread.Post(() => Results.Add(new Result([.. fixedPrecision], [.. variant], sum, distance)));
				}
			}
		});
		previewUpdater.Stop();
		previewUpdater.Elapsed -= PreviewUpdater_Elapsed;
		PreviewUpdater_Elapsed(null, null!);
		IsCalculating = false;
		Dispatcher.UIThread.Post(() =>{
			if (Results.Count > 0){
                SelectedResult = Results.Last();
                VisualizeResult(Results.Last());
			}
		});

    }
	[RelayCommand]
	private void VisualizeResult(Result bestResult) {
		for(var i = 0; i < Parts.Count; i++) {
			var part = Parts[i];
			part.Factor = bestResult.Factors[i];
		}
	}
}
