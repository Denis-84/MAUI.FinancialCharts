namespace Maui.FinancialCharts.TimeFrames;

public class WeeklyTimeFrame :
	ITimeFrame {

	public DateTime GetTimeBucket(DateTime timestamp) =>
		timestamp.AddDays((7 + timestamp.DayOfWeek - DayOfWeek.Monday) % 7 * -1).Date;

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

			if (curr.Year != prev.Year) {
				if (curr.Year % 5 == 0)
					significance = TimeSignificance.Critical;
				else
					significance = TimeSignificance.Important;
			}
			else if (curr.Month != prev.Month && (curr.Month - 1) % 3 == 0)
				significance = TimeSignificance.Major;
			else
				significance = TimeSignificance.Minor;

			significatedLabel = significance switch {
				TimeSignificance.Critical => curr.ToString("dd\nyyyy"),
				TimeSignificance.Important => curr.ToString("dd\nyyyy"),
				TimeSignificance.Major => curr.ToString("dd\nMMM"),
				TimeSignificance.Minor => curr.ToString("dd"),

				_ => curr.ToString()
			};

			return (commonLabel, significatedLabel, significance);
		}
	}
}
