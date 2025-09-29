using Maui.FinancialCharts.MarketData;
using Maui.FinancialCharts.TimeFrames;

namespace Maui.FinancialCharts.Controls;

public class ClusterChart : GraphicsView {
	public static readonly BindableProperty DataSourceProperty;
	public static readonly BindableProperty TickSizeProperty;
	public static readonly BindableProperty TimeFrameProperty;

	public static readonly BindableProperty VerticalHistogramProperty;
	public static readonly BindableProperty HorizontalHistogramProperty;

	public static readonly BindableProperty BackgroundColorFromProperty;
	public static readonly BindableProperty BackgroundColorToProperty;
	public static readonly BindableProperty ClusterMinColorProperty;
	public static readonly BindableProperty ClusterMaxColorProperty;
	public static readonly BindableProperty HistogramMinColorProperty;
	public static readonly BindableProperty HistogramMaxColorProperty;
	public static readonly BindableProperty PriceColorProperty;
	public static readonly BindableProperty TimeColorProperty;
	public static readonly BindableProperty MessageColorProperty;
	public static readonly BindableProperty VolumeTextColorProperty;
	public static readonly BindableProperty LabelTextColorProperty;
	public static readonly BindableProperty LinesColorProperty;

	static ClusterChart() {
		DataSourceProperty = BindableProperty.Create(
			nameof(DataSource),
			typeof(ITickDataProvider),
			typeof(ClusterChart),
			null,
			propertyChanged: (obj, oVal, nVal) => ((ClusterChart)obj).RebuildChart()
		);
		TickSizeProperty = BindableProperty.Create(
			nameof(TickSize),
			typeof(double),
			typeof(ClusterChart),
			0.05
		);
		TimeFrameProperty = BindableProperty.Create(
			nameof(TimeFrame),
			typeof(TimeFrame),
			typeof(ClusterChart),
			TimeFrame.Daily
		);
		VerticalHistogramProperty = BindableProperty.Create(
			nameof(VerticalHistogram),
			typeof(bool),
			typeof(ClusterChart),
			true,
			propertyChanged: (obj, oVal, nVal) => {
				var control = (ClusterChart)obj;
				control.renderer.IsVerticalVolumeVisible = (bool)nVal;
				control.Invalidate();
			}
		);
		HorizontalHistogramProperty = BindableProperty.Create(
			nameof(HorizontalHistogram),
			typeof(bool),
			typeof(ClusterChart),
			true,
			propertyChanged: (obj, oVal, nVal) => {
				var control = (ClusterChart)obj;
				control.renderer.IsHorizontalVolumeVisible = (bool)nVal;
				control.Invalidate();
			}
		);
		BackgroundColorFromProperty = BindableProperty.Create(
			nameof(BackgroundColorFrom),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: (obj, _, _) => {
				var control = (ClusterChart)obj;
				control.renderer.OnBackgroundColorChanged(control.BackgroundColorFrom, control.BackgroundColorTo);
				control.Invalidate();
			}
		);
		BackgroundColorToProperty = BindableProperty.Create(
			nameof(BackgroundColorTo),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: (obj, _, _) => {
				var control = (ClusterChart)obj;
				control.renderer.OnBackgroundColorChanged(control.BackgroundColorFrom, control.BackgroundColorTo);
				control.Invalidate();
			}
		);
		ClusterMinColorProperty = BindableProperty.Create(
			nameof(ClusterMinColor),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: async (obj, _, _) => {
				var control = (ClusterChart)obj;
				control.Invalidate();
				await control.renderer.OnClusterColorChanged(control.ClusterMinColor, control.ClusterMaxColor);
				control.Invalidate();
			}
		);
		ClusterMaxColorProperty = BindableProperty.Create(
			nameof(ClusterMaxColor),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: async (obj, _, _) => {
				var control = (ClusterChart)obj;
				control.Invalidate();
				await control.renderer.OnClusterColorChanged(control.ClusterMinColor, control.ClusterMaxColor);
				control.Invalidate();
			}
		);
		HistogramMinColorProperty = BindableProperty.Create(
			nameof(HistogramMinColor),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: async (obj, _, _) => {
				var control = (ClusterChart)obj;

				control.Invalidate();
				await control.renderer.OnHistogramColorChanged(control.HistogramMinColor, control.HistogramMaxColor);
				control.Invalidate();
			}
		);
		HistogramMaxColorProperty = BindableProperty.Create(
			nameof(HistogramMaxColor),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: async (obj, _, _) => {
				var control = (ClusterChart)obj;

				control.Invalidate();
				await control.renderer.OnHistogramColorChanged(control.HistogramMinColor, control.HistogramMaxColor);
				control.Invalidate();
			}
		);
		PriceColorProperty = BindableProperty.Create(
			nameof(PriceColor),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: (obj, _, nVal) => {
				var control = (ClusterChart)obj;
				control.renderer.PriceColor = (Color)nVal;
				control.Invalidate();
			}
		);
		TimeColorProperty = BindableProperty.Create(
			nameof(TimeColor),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: (obj, _, nVal) => {
				var control = (ClusterChart)obj;
				control.renderer.TimeColor = (Color)nVal;
				control.Invalidate();
			}
		);
		MessageColorProperty = BindableProperty.Create(
			nameof(MessageColor),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: (obj, _, nVal) => {
				var control = (ClusterChart)obj;
				control.renderer.MessageColor = (Color)nVal;
				control.Invalidate();
			}
		);
		VolumeTextColorProperty = BindableProperty.Create(
			nameof(VolumeTextColor),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: (obj, _, nVal) => {
				var control = (ClusterChart)obj;
				control.renderer.VolumeColor = (Color)nVal;
				control.Invalidate();
			}
		);
		LabelTextColorProperty = BindableProperty.Create(
			nameof(LabelTextColor),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: (obj, _, nVal) => {
				var control = (ClusterChart)obj;
				control.renderer.LabelTextColor = (Color)nVal;
				control.Invalidate();
			}
		);
		LinesColorProperty = BindableProperty.Create(
			nameof(LinesColor),
			typeof(Color),
			typeof(ClusterChart),
			null,
			propertyChanged: (obj, _, nVal) => {
				var control = (ClusterChart)obj;
				control.renderer.OnLinesColorChanged((Color)nVal);
				control.Invalidate();
			}
		);
	}

	/// <summary>
	/// The source of tick data that implements <see cref="ITickDataProvider"/>
	/// </summary>
	public ITickDataProvider? DataSource {
		get => (ITickDataProvider?)GetValue(DataSourceProperty);
		set => SetValue(DataSourceProperty, value);
	}
	/// <summary>
	/// Minimum step of price scale
	/// </summary>
	public double TickSize {
		get => (double)GetValue(TickSizeProperty);
		set => SetValue(TickSizeProperty, value);
	}
	/// <summary>
	/// Minimum step of time scale
	/// </summary>
	public TimeFrame TimeFrame {
		get => (TimeFrame)GetValue(TimeFrameProperty);
		set => SetValue(TimeFrameProperty, value);
	}

	/// <summary>
	/// Defines whether vertical volume histogram is visible
	/// </summary>
	public bool VerticalHistogram {
		get => (bool)GetValue(VerticalHistogramProperty);
		set => SetValue(VerticalHistogramProperty, value);
	}
	/// <summary>
	/// Defines whether horizontal volume histogram is visible
	/// </summary>
	public bool HorizontalHistogram {
		get => (bool)GetValue(HorizontalHistogramProperty);
		set => SetValue(HorizontalHistogramProperty, value);
	}

	/// <summary>
	/// Defines upper color of background gradient
	/// </summary>
	public Color BackgroundColorFrom {
		get => (Color)GetValue(BackgroundColorFromProperty);
		set => SetValue(BackgroundColorFromProperty, value);
	}
	/// <summary>
	/// Defines lower color of background gradient
	/// </summary>
	public Color BackgroundColorTo {
		get => (Color)GetValue(BackgroundColorToProperty);
		set => SetValue(BackgroundColorToProperty, value);
	}
	/// <summary>
	/// Defines the color of the cluster with the minumum volume
	/// </summary>
	public Color ClusterMinColor {
		get => (Color)GetValue(ClusterMinColorProperty);
		set => SetValue(ClusterMinColorProperty, value);
	}
	/// <summary>
	/// Defiens the color of the cluster with the maximum volume
	/// </summary>
	public Color ClusterMaxColor {
		get => (Color)GetValue(ClusterMaxColorProperty);
		set => SetValue(ClusterMaxColorProperty, value);
	}
	/// <summary>
	/// Defines the color of the histogram bar with the minimum volume
	/// </summary>
	public Color HistogramMinColor {
		get => (Color)GetValue(HistogramMinColorProperty);
		set => SetValue(HistogramMinColorProperty, value);
	}
	/// <summary>
	/// Defines the color of the histogram bar with the maximum volume
	/// </summary>
	public Color HistogramMaxColor {
		get => (Color)GetValue(HistogramMaxColorProperty);
		set => SetValue(HistogramMaxColorProperty, value);
	}
	/// <summary>
	/// Defines the color of the price scale text
	/// </summary>
	public Color PriceColor {
		get => (Color)GetValue(PriceColorProperty);
		set => SetValue(PriceColorProperty, value);
	}
	/// <summary>
	/// Defines the color of the time scale text
	/// </summary>
	public Color TimeColor {
		get => (Color)GetValue(TimeColorProperty);
		set => SetValue(TimeColorProperty, value);
	}
	/// <summary>
	/// Defines the color of state messages
	/// </summary>
	public Color MessageColor {
		get => (Color)GetValue(MessageColorProperty);
		set => SetValue(MessageColorProperty, value);
	}
	/// <summary>
	/// Defines the color of cluster volume text
	/// </summary>
	public Color VolumeTextColor {
		get => (Color)GetValue(VolumeTextColorProperty);
		set => SetValue(VolumeTextColorProperty, value);
	}
	/// <summary>
	/// Defines the color of line labels
	/// </summary>
	public Color LabelTextColor {
		get => (Color)GetValue(LabelTextColorProperty);
		set => SetValue(LabelTextColorProperty, value);
	}
	/// <summary>
	/// Defines the color of price and time lines
	/// </summary>
	public Color LinesColor {
		get => (Color)GetValue(LinesColorProperty);
		set => SetValue(LinesColorProperty, value);
	}

	public ClusterChart() {
		Drawable = this.renderer = new ClusterChartRenderer();

		MoveHoverInteraction += OnHoverUpdated;
		StartInteraction += OnTouchStarted;
		DragInteraction += OnTouchUpdated;
		EndInteraction += OnTouchEnded;
	}

	/// <summary>
	/// Reloads data from <see cref="DataSource"/> and 
	/// rebuilds the chart with <see cref="TickSize"/> and <see cref="TimeFrame"/>
	/// </summary>
	public async void RebuildChart() {
		var timeFrame = this.cachedTimeFrames.GetValueOrDefault(TimeFrame);

		if (DataSource != null &&
			timeFrame != null &&
			TickSize > 0) {

			Invalidate();
			await this.renderer.LoadDataAsync(DataSource, TickSize, timeFrame);
			Invalidate();
		}
	}
	/// <summary>
	/// Shifts the chart to the most recent price and reset both time and price scale to 1
	/// </summary>
	public void ResetTransform() {
		this.renderer.ResetTransform();
		Invalidate();
	}
	/// <summary>
	/// Shifts the chart to the most recent price
	/// </summary>
	public void ResetOffset() {
		this.renderer.ResetOffset();
		Invalidate();
	}
	/// <summary>
	/// Reset both time and price scale to 1
	/// </summary>
	public void ResetScale() {
		this.renderer.ResetScale();
		Invalidate();
	}

	protected override void OnSizeAllocated(double width, double height) {
		base.OnSizeAllocated(width, height);
		this.renderer.OnSizeChanged((float)width, (float)height);
	}
	private void OnHoverUpdated(object? sender, TouchEventArgs e) {
		this.renderer.OnHoverUpdated(e.Touches[0]);
		Invalidate();
	}
	private void OnTouchStarted(object? sender, TouchEventArgs e) {
		this.ptOld = this.ptClick = e.Touches[0];
		this.renderer.OnTouchStarted(in this.ptClick);
	}
	private void OnTouchUpdated(object? sender, TouchEventArgs e) {
		var delta = e.Touches[0] - this.ptOld;
		this.renderer.OnTouchUpdated(in delta);
		Invalidate();
		this.ptOld = e.Touches[0];
	}
	private void OnTouchEnded(object? sender, TouchEventArgs e) {
		if (this.ptClick == e.Touches[0]) {
			this.renderer.OnClicked(in this.ptClick);
			Invalidate();
		}
		renderer.OnTouchEnded();
	}

	private PointF ptClick;
	private PointF ptOld;

	private readonly ClusterChartRenderer renderer;
	private readonly Dictionary<TimeFrame, ITimeFrame> cachedTimeFrames = new() {
		[TimeFrame.Weekly] = new WeeklyTimeFrame(),
		[TimeFrame.Daily] = new DailyTimeFrame(),
		[TimeFrame.Hour4] = new CustomHourTimeFrame(4),
		[TimeFrame.Hour1] = new HourTimeFrame(),
		[TimeFrame.Minute15] = new CustomMinuteTimeFrame(15),
		[TimeFrame.Minute5] = new CustomMinuteTimeFrame(5),
		[TimeFrame.Minute1] = new MinuteTimeFrame()
	};
}
