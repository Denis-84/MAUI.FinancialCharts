using Maui.FinancialCharts.MarketData;
using Maui.FinancialCharts.TimeFrames;
using Microsoft.Maui.Animations;
using System.Numerics;
using Font = Microsoft.Maui.Graphics.Font;

namespace Maui.FinancialCharts.Controls;

public class ClusterChartRenderer :
	IDrawable {

	public bool IsVerticalVolumeVisible = true;
	public bool IsHorizontalVolumeVisible = true;

	public Color MessageColor = new(0.6f, 0.9f, 1.0f);
	public Color TimeColor = new(0.6f, 0.9f, 1.0f);
	public Color PriceColor = new(0.6f, 0.9f, 1.0f);
	public Color VolumeColor = Color.FromHsla(0.5, 0, 0.5);
	public Color LabelTextColor = new(0.1f, 0.0f, 0.2f);

	public ClusterChartRenderer() {
		backgroundColor = new LinearGradientBrush {
			GradientStops = {
				new GradientStop(backgroundFromColor, 0),
				new GradientStop(backgroundToColor, 1)
			},
			StartPoint = new Point(0.5f, 0),
			EndPoint = new Point(0.5f, 1)
		};
		scaleColor = new LinearGradientBrush {
			GradientStops = {
				new GradientStop(backgroundFromColor.WithAlpha(0.8f), 0),
				new GradientStop(backgroundToColor.WithAlpha(0.8f), 1)
			},
			StartPoint = new Point(0.5f, 0),
			EndPoint = new Point(0.5f, 1)
		};
		horizontalLineColor = new LinearGradientBrush {
			GradientStops = {
				new GradientStop(lineColor.WithAlpha(0.6f), 0),
				new GradientStop(lineColor.WithAlpha(0.3f), 1)
			},
			StartPoint = new Point(1, 0.5f),
			EndPoint = new Point(0, 0.5f)
		};
		verticalLineColor = new LinearGradientBrush {
			GradientStops = {
				new GradientStop(lineColor.WithAlpha(0.6f), 0),
				new GradientStop(lineColor.WithAlpha(0.3f), 1)
			},
			StartPoint = new Point(0.5f, 1),
			EndPoint = new Point(0.5f, 0)
		};
	}

	/// <summary>
	/// Loads tick data from provider asynchronously and accumulates tick volumes by time bars and price levels in sparce matrix,
	/// then distributes accumulated data from the matrix by clusters.
	/// </summary>
	/// <param name="provider">The source of tick data that implements <see cref="ITickDataProvider"/></param>
	/// <param name="tickSize">Minimum step of price scale</param>
	/// <param name="timeFrame">Minimum step of time scale</param>
	/// <returns></returns>
	public async Task LoadDataAsync(ITickDataProvider provider, double tickSize, ITimeFrame timeFrame) {
		if (state is ChartState.Loading) return;

		state = ChartState.Loading;

		var minPrice = double.MaxValue;
		var maxPrice = double.MinValue;
		var lastPrice = 0.0;

		var cts = new CancellationTokenSource();
		var matrix = new Dictionary<DateTime, Dictionary<double, int>>();

		await foreach (var tick in provider.GetTicksAsync()
										   .WithCancellation(cts.Token)) {
			lastPrice = Math.Round(tick.Price / tickSize) * tickSize;

			if (maxPrice < lastPrice)
				maxPrice = lastPrice;
			if (minPrice > lastPrice)
				minPrice = lastPrice;

			var col = timeFrame.GetTimeBucket(tick.Time);
			var row = lastPrice;

			matrix[col] = matrix.GetValueOrDefault(col) ?? [];
			matrix[col][row] = matrix[col].GetValueOrDefault(row) + tick.Volume;
		}

		InitializeTimes(matrix, timeFrame);
		InitializePrices(tickSize, maxPrice, minPrice, lastPrice);
		InitializeClusters(matrix, maxPrice, tickSize);
		InitializeVerticalVolumes();
		InitializeHorizontalVolumes();

		state = ChartState.Loaded;

		ResetTransform();
	}

	// Event Handlers

	public void OnSizeChanged(float width, float height) {
		SetBounds(in width, in height);
	}

	public void OnClicked(in PointF point) {
		if (state != ChartState.Loaded) return;

		if (regionPrice.Contains(point))
			HandlePriceClick(in point);
		else if (regionTime.Contains(point))
			HandleTimeClick(in point);
	}
	public void OnHoverUpdated(in PointF point) {
		if (state != ChartState.Loaded) return;

		if (isDraggingPrice) {
			if (!regionPrice.Contains(point)) {
				isDraggingPrice = false;
				return;
			}
			draggingPriceLineIdx = Math.Clamp(~Array.BinarySearch(pricePosY!, point.Y) - 1, 0, priceCount - 1);
			return;
		}
		if (isDraggingTime) {
			if (!regionTime.Contains(point)) {
				isDraggingTime = false;
				return;
			}
			draggingTimeLineIdx = Math.Clamp(~Array.BinarySearch(timePosX!, point.X) - 1, 0, timeCount - 1);
			return;
		}
	}

	public void OnTouchStarted(in PointF point) {
		if (state != ChartState.Loaded) return;

		if (isOffset = regionChart.Contains(point)) return;
		if (isScaledY = regionPrice.Contains(point)) return;
		if (isScaledX = regionTime.Contains(point)) return;
	}
	public void OnTouchUpdated(in SizeF offset) {
		if (state != ChartState.Loaded) return;

		if (isOffset) SetOffset(in offset);
		else if (isScaledX) SetScaleX(offset.Width);
		else if (isScaledY) SetScaleY(offset.Height);
	}
	public void OnTouchEnded() {
		if (state != ChartState.Loaded) return;

		isOffset = false;
		isScaledY = false;
		isScaledX = false;
	}

	public void OnBackgroundColorChanged(Color? from, Color? to) {
		backgroundFromColor = from ?? backgroundFromColor;
		backgroundToColor = to ?? backgroundToColor;

		var background = (LinearGradientBrush)backgroundColor;
		background.GradientStops[0].Color = backgroundFromColor;
		background.GradientStops[1].Color = backgroundToColor;

		var scale = (LinearGradientBrush)scaleColor;
		scale.GradientStops[0].Color = backgroundFromColor.WithAlpha(0.8f);
		scale.GradientStops[1].Color = backgroundToColor.WithAlpha(0.8f);
	}
	public void OnLinesColorChanged(Color? color) {
		lineColor = color?.WithAlpha(0.9f) ?? lineColor;

		var horizontal = (LinearGradientBrush)horizontalLineColor;
		horizontal.GradientStops[0].Color = lineColor.WithAlpha(0.6f);
		horizontal.GradientStops[1].Color = lineColor.WithAlpha(0.3f);

		var vertical = (LinearGradientBrush)verticalLineColor;
		vertical.GradientStops[0].Color = lineColor.WithAlpha(0.6f);
		vertical.GradientStops[1].Color = lineColor.WithAlpha(0.3f);
	}
	public async Task OnClusterColorChanged(Color? min, Color? max) {
		if (state != ChartState.Loaded) return;

		state = ChartState.Loading;

		clusterMinColor = min ?? clusterMinColor;
		clusterMaxColor = max ?? clusterMaxColor;

		await Task.Run(() => {
			for (int i = 0; i < clusterCount; i++)
				clusterColors![i] = clusterMinColor.Lerp(clusterMaxColor, clusterRatio![i]);
		});

		state = ChartState.Loaded;
	}
	public async Task OnHistogramColorChanged(Color? min, Color? max) {
		if (state != ChartState.Loaded) return;

		state = ChartState.Loading;

		histogramMinColor = min ?? histogramMinColor;
		histogramMaxColor = max ?? histogramMaxColor;

		await Task.Run(() => {
			for (int i = 0; i < timeCount; i++)
				timeVolumeColors![i] = histogramMinColor.Lerp(histogramMaxColor, timeVolumeRatio![i]);
			for (int i = 0; i < priceCount; i++)
				priceVolumeColors![i] = histogramMinColor.Lerp(histogramMaxColor, priceVolumeRatio![i]);
		});

		state = ChartState.Loaded;
	}

	private void HandlePriceClick(in PointF click) {
		if (isDraggingPrice) {
			fixedPriceLineIdxs.Add(draggingPriceLineIdx);
			isDraggingPrice = false;
			return;
		}
		else {
			draggingPriceLineIdx = Math.Clamp(~Array.BinarySearch(pricePosY!, click.Y) - 1, 0, priceCount - 1);
			fixedPriceLineIdxs.Remove(draggingPriceLineIdx);
			isDraggingPrice = true;
			return;
		}
	}
	private void HandleTimeClick(in PointF click) {
		if (isDraggingTime) {
			fixedTimeLineIdxs.Add(draggingTimeLineIdx);
			isDraggingTime = false;
			return;
		}
		else {
			draggingTimeLineIdx = Math.Clamp(~Array.BinarySearch(timePosX!, click.X) - 1, 0, timeCount - 1);
			fixedTimeLineIdxs.Remove(draggingTimeLineIdx);
			isDraggingTime = true;
			return;
		}
	}

	// Draw

	public void Draw(ICanvas canvas, RectF dirtyRect) {
		DrawBackground(canvas);

		switch (state) {
			case ChartState.Loading:
				DrawMessage("Loading...", canvas);
				break;
			case ChartState.Loaded:
				DrawClusters(canvas);
				DrawScale(canvas);
				DrawPrices(canvas);
				DrawTimes(canvas);

				if (IsVerticalVolumeVisible)
					DrawVerticalVolumes(canvas);
				if (IsHorizontalVolumeVisible)
					DrawHorizontalVolumes(canvas);

				DrawFixedPriceLines(canvas);
				DrawFixedTimeLines(canvas);

				if (isDraggingPrice)
					DrawPriceLine(canvas, draggingPriceLineIdx);
				if (isDraggingTime)
					DrawTimeLine(canvas, draggingTimeLineIdx);
				break;
			default:
				DrawMessage("No data", canvas);
				break;
		}
	}

	private void DrawTimes(ICanvas canvas) {
		var y = regionTime.Top;
		var height = regionTime.Height;

		canvas.SaveState();
		canvas.ClipRectangle(regionTime);

		canvas.FontSize = 18;
		canvas.FontColor = TimeColor;
		canvas.Font = Font.Default;

		for (int i = 0; i < timeCount; i++) {
			if (timeInBounds![i] && timeIsVisible![i]) {
				canvas.DrawString(
					timeLabels![i],
					timeLabelPosX![i],
					y,
					timeLabelWidth,
					height,
					HorizontalAlignment.Center,
					VerticalAlignment.Top
				);
			}
		}

		canvas.RestoreState();
	}
	private void DrawPrices(ICanvas canvas) {
		var x = regionPrice.Left;
		var width = regionPrice.Width;

		canvas.SaveState();
		canvas.ClipRectangle(regionPrice);
		canvas.FontSize = 18;
		canvas.FontColor = PriceColor;
		canvas.Font = Font.Default;

		for (int i = 0; i < priceCount; i++) {
			if (priceInBounds![i] && priceIsVisible![i])
				canvas.DrawString(
					priceLabels![i],
					x,
					priceLabelPosY![i],
					width,
					priceLabelHeight,
					HorizontalAlignment.Left,
					VerticalAlignment.Center
				);
		}
		canvas.RestoreState();
	}
	private void DrawClusters(ICanvas canvas) {
		for (int i = 0; i < clusterCount; i++) {
			if (timeInBounds![clusterPos![i].timeIdx] &&
				priceInBounds![clusterPos[i].priceIdx]) {

				canvas.FillColor = clusterColors![i];
				canvas.FillRectangle(
					timePosX![clusterPos![i].timeIdx],
					pricePosY![clusterPos![i].priceIdx],
					clusterWidth![i],
					priceHeight
				);
			}
		}
		if (scaleX > 3f && scaleY > 1.2f) {
			canvas.FontColor = VolumeColor;
			canvas.FontSize = 16f;
			canvas.Font = Font.DefaultBold;

			for (int i = 0; i < clusterCount; i++) {
				if (timeInBounds![clusterPos![i].timeIdx] &&
					priceInBounds![clusterPos[i].priceIdx]) {
					canvas.DrawString(
						clusterLabels![i],
						timePosX![clusterPos![i].timeIdx],
						pricePosY![clusterPos![i].priceIdx],
						timeWidth,
						priceHeight,
						HorizontalAlignment.Left,
						VerticalAlignment.Center
					);
				}
			}
		}
	}
	private void DrawVerticalVolumes(ICanvas canvas) {
		canvas.SaveState();
		canvas.ClipRectangle(regionVerticalHist);

		var bottom = regionVerticalHist.Bottom;

		for (int i = 0; i < timeCount; i++) {
			if (timeInBounds![i]) {
				canvas.FillColor = timeVolumeColors![i];
				canvas.FillRectangle(
					timePosX![i],
					bottom - timeVolumeSize![i],
					timeWidth,
					timeVolumeSize![i]
				);
			}
		}

		canvas.RestoreState();
	}
	private void DrawHorizontalVolumes(ICanvas canvas) {
		canvas.SaveState();
		canvas.ClipRectangle(regionHorizontalHist);

		var x = regionHorizontalHist.Left;

		for (int i = 0; i < priceCount; i++) {
			if (priceInBounds![i]) {
				canvas.FillColor = priceVolumeColors![i];
				canvas.FillRectangle(
					x,
					pricePosY![i],
					priceVolumeSize![i],
					priceHeight
				);
			}
		}

		canvas.RestoreState();
	}
	private void DrawMessage(string message, ICanvas canvas) {
		canvas.FontSize = 48;
		canvas.FontColor = MessageColor;
		canvas.Font = Font.DefaultBold;

		canvas.DrawString(
			message,
			regionChart,
			HorizontalAlignment.Center,
			VerticalAlignment.Center
		);
	}
	private void DrawBackground(ICanvas canvas) {
		canvas.SetFillPaint(backgroundColor, viewport);
		canvas.FillRectangle(viewport);
	}
	private void DrawScale(ICanvas canvas) {
		canvas.SetFillPaint(scaleColor, viewport);
		canvas.FillRectangle(regionPrice);
		canvas.FillRectangle(regionTime);
	}
	private void DrawPriceLine(ICanvas canvas, int lineIndex) {
		var line = new RectF(
			0, pricePosY![lineIndex],
			regionChart.Width, priceHeight
		);
		var label = new RectF(
			regionPrice.Left, priceLabelPosY![lineIndex],
			regionPrice.Width, priceLabelHeight
		);

		canvas.SetFillPaint(horizontalLineColor, line);
		canvas.FillRectangle(line);

		canvas.FillColor = lineColor;
		canvas.FillRoundedRectangle(label, priceLabelHeight / 4);

		canvas.FontSize = 18;
		canvas.FontColor = LabelTextColor;
		canvas.Font = Font.DefaultBold;
		canvas.DrawString(
			priceLabels![lineIndex], label,
			HorizontalAlignment.Left, VerticalAlignment.Center
		);
	}
	private void DrawTimeLine(ICanvas canvas, int lineIndex) {
		var line = new RectF(
			timePosX![lineIndex], 0,
			timeWidth, regionChart.Height
		);
		var label = new RectF(
			timeLabelPosX![lineIndex], regionTime.Top,
			timeLabelWidth, regionTime.Height
		);

		canvas.SetFillPaint(verticalLineColor, line);
		canvas.FillRectangle(line);

		canvas.FillColor = lineColor;
		canvas.FillRoundedRectangle(label, regionTime.Height / 4);

		canvas.FontSize = 18;
		canvas.FontColor = LabelTextColor;
		canvas.Font = Font.DefaultBold;
		canvas.DrawString(
			timeBaseLabels![lineIndex], label,
			HorizontalAlignment.Center, VerticalAlignment.Center
		);
	}
	private void DrawFixedPriceLines(ICanvas canvas) {
		foreach (var idx in fixedPriceLineIdxs)
			DrawPriceLine(canvas, idx);
	}
	private void DrawFixedTimeLines(ICanvas canvas) {
		foreach (var idx in fixedTimeLineIdxs)
			DrawTimeLine(canvas, idx);
	}

	// Transform methods

	public void ResetTransform() {
		offsetX = regionChart.Width * 0.8f;
		offsetY = regionChart.Center.Y;

		scaleX = 1;
		scaleY = 1;

		RecalculateTimes();
		RecalculatePrices();
		RecalculateClusters();

		offsetXMin = regionChart.Width / 2;
		offsetXMax = timeWidth * timeCount + offsetXMin;
	}
	public void ResetOffset() {
		offsetX = regionChart.Width * 0.8f;
		offsetY = regionChart.Center.Y;

		offsetX += (viewport.Width * scaleX - viewport.Width) / 2;
		offsetY += (viewport.Height * scaleY - viewport.Height) / 2;

		RecalculateTimes();
		RecalculatePrices();
		RecalculateClusters();
	}
	public void ResetScale() {
		var oldScaleX = scaleX;
		var oldScaleY = scaleY;

		scaleX = 1;
		scaleY = 1;

		var ratioX = scaleX / oldScaleX;
		var ratioY = scaleY / oldScaleY;

		offsetX *= ratioX;
		offsetXMin *= ratioX;
		offsetXMax *= ratioX;

		offsetY *= ratioY;

		RecalculateTimes();
		RecalculatePrices();
		RecalculateClusters();
	}

	private void SetOffset(in SizeF offset) {
		offsetX = Math.Clamp(offsetX + offset.Width, offsetXMin, offsetXMax);
		offsetY += offset.Height;

		RecalculateTimes();
		RecalculatePrices();
		RecalculateClusters();
	}
	private void SetScaleX(float offset) {
		var oldScaleX = scaleX;
		var newScaleX = scaleX * (float)Math.Exp(offset * scaleRatio);

		scaleX = Math.Clamp(newScaleX, scaleMin, scaleMaxX);

		var ratio = scaleX / oldScaleX;

		offsetX *= ratio;
		offsetXMin *= ratio;
		offsetXMax *= ratio;

		RecalculateTimes();
		RecalculateClusters();
	}
	private void SetScaleY(float offset) {
		var oldScaleY = scaleY;
		var newScaleY = scaleY / (float)Math.Exp(offset * scaleRatio);

		scaleY = Math.Clamp(newScaleY, scaleMin, scaleMaxY);
		offsetY *= scaleY / oldScaleY;

		RecalculatePrices();
		RecalculateClusters();
	}
	private void SetBounds(in float width, in float height) {
		viewport = new RectF(0, 0, width, height);

		regionChart = new RectF(
			0, 0,
			viewport.Width - sizePriceScale, viewport.Height - sizeTimeScale
		);
		regionPrice = new RectF(
			regionChart.Right, 0,
			sizePriceScale, regionChart.Height
		);
		regionTime = new RectF(
			0, regionChart.Bottom,
			viewport.Width, sizeTimeScale
		);
		regionHorizontalHist = new RectF(
			0, 0,
			sizeHorizontalHist, regionChart.Height - sizeVerticalHist
		);
		regionVerticalHist = new RectF(
			0, regionChart.Height - sizeVerticalHist,
			regionChart.Width, sizeVerticalHist
		);

		offsetXMin = regionChart.Width / 2;
		offsetXMax = timeWidth * timeCount + offsetXMin;

		offsetXMin += (regionChart.Width * scaleX - regionChart.Width) / 2;
		offsetXMax += (regionChart.Width * scaleX - regionChart.Width) / 2;

		if (state == ChartState.Loaded) {
			RecalculateTimes();
			RecalculatePrices();
			RecalculateClusters();
		}
	}

	// Initializers

	private void InitializeTimes(Dictionary<DateTime, Dictionary<double, int>> matrix, ITimeFrame timeFrame) {
		timeCount = matrix.Keys.Count;

		timePosX = new float[timeCount];
		timeBasePosX = new float[timeCount];
		timeInBounds = new bool[timeCount];
		timeValues = [.. matrix.Keys];
		timeBaseLabels = new string[timeCount];
		timeLabels = new string[timeCount];
		timeIsVisible = new bool[timeCount];
		var timeSignificances = new TimeSignificance[timeCount];
		timeLabelPosX = new float[timeCount];
		timeMaxClusterVolumes = new int[timeCount];
		timeVolumes = new int[timeCount];
		timeVolumeRatio = new float[timeCount];
		timeVolumeSize = new float[timeCount];
		timeVolumeColors = new Color[timeCount];

		var timeOffsetLabelPosX = (timeLabelWidth - timeBaseWidth) / 2;
		for (int i = 0; i < timeCount; i++) {
			timeBasePosX[i] = timeBaseWidth * (i - timeCount + 1);
			timePosX[i] = timeBasePosX[i];
			timeLabelPosX[i] = timePosX[i] - timeOffsetLabelPosX;
			var (baseLabel, label, significance) = timeFrame.GetLabelWithSignificance(
				timeValues[i],
				i == 0 ? null : timeValues[i - 1]
			);
			timeBaseLabels[i] = baseLabel;
			timeLabels[i] = label;
			timeSignificances[i] = significance;
		}

		foreach (var isVisibleStep in timeIsVisibleSteps) {
			var tmp = new bool[timeCount];
			for (int i = 0; i < timeCount; i++)
				tmp[i] = timeSignificances[i] >= isVisibleStep;
			cachedTimeIsVisible[isVisibleStep] = tmp;
		}

		timeWidth = timeBaseWidth;
	}
	private void InitializePrices(double tickSize, double maxPrice, double minPrice, double lastPrice) {
		priceCount = (int)Math.Round((maxPrice - minPrice) / tickSize + 1);

		pricePosY = new float[priceCount];
		priceBasePosY = new float[priceCount];
		priceInBounds = new bool[priceCount];
		priceValues = new double[priceCount];
		var priceRoundedValues = new int[priceCount];
		priceLabels = new string[priceCount];
		priceIsVisible = new bool[priceCount];
		priceLabelPosY = new float[priceCount];
		priceVolumes = new int[priceCount];
		priceVolumeRatio = new float[priceCount];
		priceVolumeSize = new float[priceCount];
		priceVolumeColors = new Color[priceCount];

		var priceOffsetPosY = (int)Math.Round((maxPrice - lastPrice) / tickSize);
		var priceOffsetLabelPosY = (priceLabelHeight - priceHeight) / 2;
		for (int i = 0; i < priceCount; i++) {
			priceBasePosY[i] = priceBaseHeight * (i - priceOffsetPosY);
			pricePosY[i] = priceBasePosY[i];
			priceValues[i] = Math.Round(maxPrice - tickSize * i, 4);
			priceRoundedValues[i] = (int)Math.Round(priceValues[i] / tickSize);
			priceLabels[i] = priceValues[i].ToString();
			priceLabelPosY[i] = pricePosY[i] - priceOffsetLabelPosY;
		}

		foreach (var scaleStep in priceIsVisibleSteps) {
			var tmp = new bool[priceCount];
			for (int i = 0; i < priceCount; i++)
				tmp[i] = priceRoundedValues[i] % scaleStep == 0;
			cachedPriceIsVisible[scaleStep] = tmp;
		}
	}
	private void InitializeClusters(Dictionary<DateTime, Dictionary<double, int>> matrix, double maxPrice, double tickSize) {
		clusterCount = matrix.Values.Aggregate(0, (sum, i) => sum + i.Count);

		clusterPos = new (int timePosX, int pricePosY)[clusterCount];
		var clusterVolumes = new int[clusterCount];
		clusterRatio = new float[clusterCount];
		clusterLabels = new string[clusterCount];
		clusterWidth = new float[clusterCount];
		clusterColors = new Color[clusterCount];

		var clusterIdx = 0;
		foreach (var (timeIdx, (time, clusters)) in matrix.Index()) {
			foreach (var (price, volume) in clusters) {
				var priceIdx = (int)Math.Round((maxPrice - price) / tickSize);

				clusterPos[clusterIdx] = (timeIdx, priceIdx);
				clusterVolumes[clusterIdx] = volume;
				clusterLabels[clusterIdx] = volume.ToString();

				timeVolumes![timeIdx] += volume;
				priceVolumes![priceIdx] += volume;
				timeMaxClusterVolumes![timeIdx] = volume > timeMaxClusterVolumes[timeIdx]
												? volume
												: timeMaxClusterVolumes[timeIdx];
				clusterIdx++;
			}
			timeValues![timeIdx] = time;
		}

		for (int i = 0; i < clusterCount; i++) {
			clusterRatio[i] = (float)clusterVolumes[i] / timeMaxClusterVolumes![clusterPos[i].timeIdx];
			clusterWidth[i] = timeWidth * clusterRatio[i];
			clusterColors[i] = clusterMinColor.Lerp(clusterMaxColor, clusterRatio[i]);
		}
	}
	private void InitializeVerticalVolumes() {
		var maxVolume = timeVolumes!.Max();

		for (int i = 0; i < timeCount; i++) {
			timeVolumeRatio![i] = (float)timeVolumes![i] / maxVolume;
			timeVolumeSize![i] = sizeVerticalHist * timeVolumeRatio![i];
			timeVolumeColors![i] = histogramMinColor.Lerp(histogramMaxColor, timeVolumeRatio![i]);
		}
	}
	private void InitializeHorizontalVolumes() {
		var maxVolume = priceVolumes!.Max();

		for (int i = 0; i < priceCount; i++) {
			priceVolumeRatio![i] = (float)priceVolumes![i] / maxVolume;
			priceVolumeSize![i] = sizeHorizontalHist * priceVolumeRatio![i];
			priceVolumeColors![i] = histogramMinColor.Lerp(histogramMaxColor, priceVolumeRatio![i]);
		}
	}

	// Recalcutation

	private void RecalculateTimes() {
		timeWidth = timeBaseWidth * scaleX;

		var left = viewport.Left;
		var right = viewport.Right;
		var optTime = viewport.Center.X * (1 - scaleX) + offsetX;
		var timeOffsetLabelPosX = (timeLabelWidth - timeWidth) / 2;

		var vecSize = Vector<float>.Count;
		var vecTimeCount = vecSize - timeCount;

		var vecTimeWidth = new Vector<float>(timeWidth);
		var vecTimeOffsetLabelPosX = new Vector<float>(timeOffsetLabelPosX);
		var vecLeft = new Vector<float>(viewport.Left);
		var vecRight = new Vector<float>(viewport.Right);
		var vecOptTime = new Vector<float>(optTime);
		var vecScaleX = new Vector<float>(scaleX);

		var timeIdx = 0;

		for (; timeIdx < vecTimeCount; timeIdx += vecSize) {
			var vecTimePosX = new Vector<float>(timeBasePosX!, timeIdx) + vecScaleX + vecOptTime;
			var vecTimeLabelPosX = vecTimePosX - vecTimeOffsetLabelPosX;
			var inBounds = Vector.GreaterThanOrEqual(vecTimePosX + vecTimeWidth, vecLeft) &
						   Vector.LessThanOrEqual(vecTimePosX, vecRight);

			vecTimePosX.CopyTo(timePosX!, timeIdx);
			vecTimeLabelPosX.CopyTo(timeLabelPosX!, timeIdx);

			for (int i = 0; i < vecSize; i++)
				timeInBounds![timeIdx + i] = inBounds[i] != 0;
		}

		for (; timeIdx < timeCount; timeIdx++) {
			timePosX![timeIdx] = timeBasePosX![timeIdx] * scaleX + optTime;
			timeLabelPosX![timeIdx] = timePosX![timeIdx] - timeOffsetLabelPosX;
			timeInBounds![timeIdx] = left <= timePosX![timeIdx] + timeWidth && timePosX![timeIdx] <= right;
		}

		var scale = scaleX switch {
			< 0.1f => TimeSignificance.Critical,
			< 0.2f => TimeSignificance.Important,
			< 1.5f => TimeSignificance.Major,
			_ => TimeSignificance.Minor
		};
		timeIsVisible = cachedTimeIsVisible[scale];
	}
	private void RecalculatePrices() {
		priceHeight = priceBaseHeight * scaleY;

		var top = viewport.Top;
		var bottom = viewport.Bottom;
		var optPrice = viewport.Center.Y * (1 - scaleY) + offsetY;
		var priceOffsetLabelPosY = (priceLabelHeight - priceHeight) / 2;

		var vecSize = Vector<float>.Count;
		var vecPriceCount = vecSize - priceCount;

		var vecPriceHeight = new Vector<float>(priceHeight);
		var vecPriceOffsetLabelPosY = new Vector<float>(priceOffsetLabelPosY);
		var vecTop = new Vector<float>(viewport.Top);
		var vecBottom = new Vector<float>(viewport.Bottom);
		var vecOptPrice = new Vector<float>(optPrice);
		var vecScaleY = new Vector<float>(scaleY);

		var priceIdx = 0;

		for (; priceIdx < vecPriceCount; priceIdx += vecSize) {
			var vecPricePosY = new Vector<float>(priceBasePosY!, priceIdx) + vecScaleY + vecOptPrice;
			var vecPriceLabelPosY = vecPricePosY - vecPriceOffsetLabelPosY;
			var inBounds = Vector.GreaterThanOrEqual(vecPricePosY + vecPriceHeight, vecTop) &
						   Vector.LessThanOrEqual(vecPricePosY, vecBottom);

			vecPricePosY.CopyTo(pricePosY!, priceIdx);
			vecPriceLabelPosY.CopyTo(priceLabelPosY!, priceIdx);

			for (int i = 0; i < vecSize; i++)
				priceInBounds![priceIdx + i] = inBounds[i] != 0;
		}

		for (; priceIdx < priceCount; priceIdx++) {
			pricePosY![priceIdx] = priceBasePosY![priceIdx] * scaleY + optPrice;
			priceLabelPosY![priceIdx] = pricePosY![priceIdx] - priceOffsetLabelPosY;
			priceInBounds![priceIdx] = top <= pricePosY![priceIdx] + priceHeight && pricePosY![priceIdx] <= bottom;
		}

		var scale = scaleY switch {
			< 0.1f => priceIsVisibleSteps[3],
			< 0.25f => priceIsVisibleSteps[2],
			< 0.9f => priceIsVisibleSteps[1],
			_ => priceIsVisibleSteps[0]
		};
		priceIsVisible = cachedPriceIsVisible[scale];
	}
	private void RecalculateClusters() {
		var vecSize = Vector<float>.Count;
		var vecClusterCount = clusterCount - vecSize;

		var vecTimeWidth = new Vector<float>(timeWidth);

		var clusterIdx = 0;

		for (; clusterIdx < vecClusterCount; clusterIdx += vecSize) {
			var vecClusterWidth = new Vector<float>(clusterRatio!, clusterIdx) * vecTimeWidth;
			vecClusterWidth.CopyTo(clusterWidth!, clusterIdx);
		}

		for (; clusterIdx < clusterCount; clusterIdx++)
			clusterWidth![clusterIdx] = clusterRatio![clusterIdx] * timeWidth;
	}

	// States

	private ChartState state = ChartState.NoData;

	// Transform data

	private float offsetX = 0;
	private float offsetY = 0;
	private float offsetXMin;
	private float offsetXMax;

	private float scaleX = 1;
	private float scaleY = 1;
	private readonly float scaleMaxY = 2f;
	private readonly float scaleMaxX = 4f;
	private readonly float scaleMin = 0.02f;
	private readonly float scaleRatio = 0.01f;

	// Bounds

	private bool isDraggingTime;
	private bool isDraggingPrice;

	private bool isOffset;
	private bool isScaledY;
	private bool isScaledX;

	private readonly float sizePriceScale = 80;
	private readonly float sizeTimeScale = 50;
	private readonly float sizeVerticalHist = 100;
	private readonly float sizeHorizontalHist = 100;

	private RectF viewport;
	private RectF regionChart;
	private RectF regionPrice;
	private RectF regionTime;
	private RectF regionVerticalHist;
	private RectF regionHorizontalHist;

	// Clusters

	private int clusterCount;

	private (int timeIdx, int priceIdx)[]? clusterPos;
	private float[]? clusterRatio;
	private string[]? clusterLabels;
	private float[]? clusterWidth;
	private Color[]? clusterColors;

	// Times

	private int timeCount;
	private int draggingTimeLineIdx;

	private float[]? timePosX;
	private float[]? timeBasePosX;
	private float[]? timeLabelPosX;
	private DateTime[]? timeValues;
	private string[]? timeLabels;
	private string[]? timeBaseLabels;
	private bool[]? timeInBounds;
	private bool[]? timeIsVisible;
	private int[]? timeVolumes;
	private float[]? timeVolumeRatio;
	private Color[]? timeVolumeColors;
	private float[]? timeVolumeSize;

	private readonly HashSet<int> fixedTimeLineIdxs = [];

	private readonly Dictionary<TimeSignificance, bool[]> cachedTimeIsVisible = [];
	private readonly TimeSignificance[] timeIsVisibleSteps = [
		TimeSignificance.Critical,
		TimeSignificance.Important,
		TimeSignificance.Major,
		TimeSignificance.Minor
	];

	private int[]? timeMaxClusterVolumes;

	// Prices

	private int priceCount;
	private int draggingPriceLineIdx;

	private float[]? pricePosY;
	private float[]? priceBasePosY;
	private float[]? priceLabelPosY;
	private double[]? priceValues;
	private string[]? priceLabels;
	private bool[]? priceInBounds;
	private bool[]? priceIsVisible;
	private int[]? priceVolumes;
	private float[]? priceVolumeRatio;
	private Color[]? priceVolumeColors;
	private float[]? priceVolumeSize;

	private readonly HashSet<int> fixedPriceLineIdxs = [];

	private readonly Dictionary<int, bool[]> cachedPriceIsVisible = [];
	private readonly int[] priceIsVisibleSteps = [1, 5, 10, 50];

	// Sizes

	private float timeWidth = 20f;
	private readonly float timeBaseWidth = 20f;
	private readonly float timeLabelWidth = 100f;

	private float priceHeight = 20f;
	private readonly float priceBaseHeight = 20f;
	private readonly float priceLabelHeight = 20f;

	// Colors

	private Color clusterMaxColor = Color.FromHsla(0.16, 1.0, 0.7);
	private Color clusterMinColor = Color.FromHsla(0.0, 1.0, 0.5);

	private Color histogramMaxColor = new(0.3f, 0.8f, 1.0f, 1.0f);
	private Color histogramMinColor = new(0.1f, 0.0f, 0.3f, 0.8f);

	private Color backgroundFromColor = new(0.1f, 0.0f, 0.2f);
	private Color backgroundToColor = new(0.0f, 0.0f, 0.1f);

	private Color lineColor = Color.FromHsla(0.16, 1.0, 0.7, 0.9f);

	private readonly Brush backgroundColor;
	private readonly Brush scaleColor;
	private readonly Brush horizontalLineColor;
	private readonly Brush verticalLineColor;

	// Inner types

	private enum ChartState {
		Loading,
		Loaded,
		NoData
	}
}
