using Maui.FinancialCharts.Demo.Models;
using Maui.FinancialCharts.MarketData;

namespace Maui.FinancialCharts.Demo.ViewModels;

public class MarketViewModel : BaseViewModel {
	public ITickDataProvider? TickDataProvider {
		get => tickDataProvider;
		set {
			tickDataProvider = value;
			OnPropertyChanged();
		}
	}

	public MarketViewModel() {
		TickDataProvider = new FakeTickDataProvider();
	}

	private ITickDataProvider? tickDataProvider;
}
