namespace Maui.FinancialCharts.TimeFrames;

public interface ITimeFrame {
	DateTime GetTimeBucket(DateTime timestamp);

	(string commonLabel, string significatedLabel, TimeSignificance significance)
		GetLabelWithSignificance(DateTime current, DateTime? previous);
}
