using Godot;
using System;
using System.Reflection;

namespace Musikspieler.Scripts.development
{
	public partial class Plot : Node2D
	{
		private readonly int plotLength;
		private float[] plotBuffer;
		private int plotIndex = 0;
		private string plotTitle;
		private int plotX;
		private int plotY;
		private float plotScaleX;
		private float plotScaleY;

		public Plot(string title, int x, int y, float scaleX = 2.0f, float scaleY = 1.0f, int length = 200)
		{
			plotLength = length;
			plotBuffer = new float[plotLength];
			plotTitle = title;
			plotX = x;
			plotY = y;
			plotScaleX = scaleX;
			plotScaleY = scaleY;
		}

		public void AddValue(float value)
		{
			plotBuffer[plotIndex] = value;
            plotIndex = (plotIndex + 1) % plotLength;
		}

		public override void _PhysicsProcess(double delta)
		{
			// Nur noch Visualisierung/Debugging!
			QueueRedraw();
		}

		private Font _defaultFont = ThemeDB.FallbackFont;
		public override void _Draw()
		{
			Vector2 p1;
			Vector2 p2;

			for (int i = 0; i < plotLength - 1; i++)
			{
				int idx1 = (plotIndex + i) % plotLength;
				int idx2 = (plotIndex + i + 1) % plotLength;
				float y1 = plotY - plotBuffer[idx1] * plotScaleY;
				float y2 = plotY - plotBuffer[idx2] * plotScaleY;
				p1 = new Vector2(plotX + i * plotScaleX, y1);
				p2 = new Vector2(plotX + (i + 1) * plotScaleX, y2);
				DrawLine(p1, p2, new Color(0, 1, 0, 0.2f), 3);
			}

			p1 = new Vector2(plotX + plotScaleX, plotY);
			p2 = new Vector2(plotX + plotLength * plotScaleX, plotY);
			DrawLine(p1, p2, new Color(1, 1, 1, 0.2f), 2);

			string info = $"{plotTitle}: {plotBuffer[plotIndex]}";
			DrawString(_defaultFont, new Vector2(plotX, plotY + plotScaleY * 50), info, HorizontalAlignment.Center);
		}
	}
}