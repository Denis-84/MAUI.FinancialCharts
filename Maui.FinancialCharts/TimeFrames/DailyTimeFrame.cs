namespace Maui.FinancialCharts.TimeFrames;

public class DailyTimeFrame :
	ITimeFrame {

	public DateTime GetTimeBucket(DateTime timestamp) =>
		timestamp.Date;

	public (string commonLabel, string significatedLabel, TimeSignificance significance)
	GetLabelWithSignificance(DateTime current, DateTime? previous) {
		var commonLabel = current.ToString("d");

		if (!previous.HasValue) {
			return (commonLabel, current.ToString("dd\nyyyy"), TimeSignificance.Critical);
		}
		else {
			var significance = TimeSignificance.None;
			var significatedLabel = string.Empty;

			var curr = current;
			var prev = previous.Value;

			if (curr.Year != prev.Year)
				significance = TimeSignificance.Critical;
			else if (curr.Month != prev.Month && (curr.Month - 1) % 3 == 0)
				significance = TimeSignificance.Important;
			else if (curr.Month != prev.Month)
				significance = TimeSignificance.Major;
			else
				significance = TimeSignificance.Minor;

			significatedLabel = significance switch {
				TimeSignificance.Critical => curr.ToString("dd\nyyyy"),
				TimeSignificance.Important => curr.ToString("dd\nMMM"),
				TimeSignificance.Major => curr.ToString("dd\nMMM"),
				TimeSignificance.Minor => curr.ToString("dd"),

				_ => curr.ToString()
			};

			return (commonLabel, significatedLabel, significance);
		}
	}
}
