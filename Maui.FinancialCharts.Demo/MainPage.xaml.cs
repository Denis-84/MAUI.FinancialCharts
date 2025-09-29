using Maui.FinancialCharts.TimeFrames;

namespace Maui.FinancialCharts.Demo;

public partial class MainPage : ContentPage {
	public MainPage() {
		InitializeComponent();

		tickSize.ItemsSource = new List<double> { 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0 };
		tickSize.SelectedItem = clusterChart.TickSize;
		
		timeFrame.ItemsSource = Enum.GetValues<TimeFrame>();
		timeFrame.SelectedItem = clusterChart.TimeFrame;
	}

	private void OnReloaded(object sender, EventArgs e) {
		clusterChart.TickSize = (double)tickSize.SelectedItem;
		clusterChart.TimeFrame = (TimeFrame)timeFrame.SelectedItem;

		clusterChart.RebuildChart();
	}

	private void OnVolumeVisibleToggled(object sender, EventArgs e) =>
		volume.IsVisible = !volume.IsVisible;


	private void OnScaleReset(object sender, EventArgs e) =>
		clusterChart.ResetScale();


	private void OnOffsetReset(object sender, EventArgs e) =>
		clusterChart.ResetOffset();

	private void OnTransformReset(object sender, EventArgs e) =>
		clusterChart.ResetTransform();
}
