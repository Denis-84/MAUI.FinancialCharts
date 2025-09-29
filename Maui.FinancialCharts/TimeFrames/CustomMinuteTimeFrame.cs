namespace Maui.FinancialCharts.TimeFrames;

public class CustomMinuteTimeFrame :
	ITimeFrame {

	public CustomMinuteTimeFrame(int minutes) =>
		this.minutes = minutes;

	public DateTime GetTimeBucket(DateTime timestamp) {
		var totalMinutes = timestamp.Hour * 60 + timestamp.Minute;
		var bucketMinutes = totalMinutes / minutes * minutes;

		return new DateTime(
			timestamp.Year,
			timestamp.Month,
			timestamp.Day,
			bucketMinutes / 60,
			bucketMinutes % 60,
			0
		);
	}

	public (string commonLabel, string significatedLabel, TimeSignificance significance)
	GetLabelWithSignificance(DateTime current, DateTime? previous) {
		var commonLabel = current.ToString("HH:mm");

		if (!previous.HasValue) {
			return (commonLabel, current.ToString("dd\nyyyy"), TimeSignificance.Critical);
		}
		else {
			var significance = TimeSignificance.None;
			var significatedLabel = string.Empty;

			var curr = current;
			var prev = previous.Value;

			if (curr.Month != prev.Month)
				significance = TimeSignificance.Critical;
			else if (curr.Day != prev.Day)
				significance = TimeSignificance.Important;
			else if (curr.Hour != prev.Hour)
				significance = TimeSignificance.Major;
			else
				significance = TimeSignificance.Minor;

			significatedLabel = significance switch {
				TimeSignificance.Critical => curr.ToString("ddd\nMMM"),
				TimeSignificance.Important => curr.ToString("ddd\ndd"),
				TimeSignificance.Major => curr.ToString("mm\nHH"),
				TimeSignificance.Minor => curr.ToString("mm"),

				_ => curr.ToString()
			};

			return (commonLabel, significatedLabel, significance);
		}
	}

	private readonly int minutes;
}
