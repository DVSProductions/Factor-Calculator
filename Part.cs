using CommunityToolkit.Mvvm.ComponentModel;

namespace FactorCalculator;

public partial class Part : ObservableObject {
	[ObservableProperty]
	private double actualPrice;
	[ObservableProperty]
	private double factor = 1;
}
