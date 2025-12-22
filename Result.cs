using CommunityToolkit.Mvvm.ComponentModel;

namespace FactorCalculator;

public partial class Result : ObservableObject {
	[ObservableProperty]
	private double[] factors;
	[ObservableProperty]
	private double[] results;
	[ObservableProperty]
	private double error;
	[ObservableProperty]
	private double total;

	public Result(double[] factors, double[] results, double total, double error) {
		this.factors = factors;
		this.results = results;
		Total = total;
		Error = error;
	}
}
