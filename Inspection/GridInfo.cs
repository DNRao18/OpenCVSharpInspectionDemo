using System.Numerics;

namespace VisionInspection.Inspection
{
	internal class GridInfo
	{
		private float pitchRow;

		private float pitchCol;

		private int gridRow;

		private int gridCol;

		public Vector2[,] PixelMap;

		public Vector2[,] RealMap;

		public Vector2[,] RefMap;

		public float PitchRow
		{
			get
			{
				return this.pitchRow;
			}
			set
			{
				this.pitchRow = value;
				this.SetPitch();
			}
		}

		public float PitchCol
		{
			get
			{
				return this.pitchCol;
			}
			set
			{
				this.pitchCol = value;
				this.SetPitch();
			}
		}

		public int GridRow
		{
			get
			{
				return this.gridRow;
			}
			set
			{
				this.gridRow = value;
				this.SetGrid();
				this.SetPitch();
			}
		}

		public int GridCol
		{
			get
			{
				return this.gridCol;
			}
			set
			{
				this.gridCol = value;
				this.SetGrid();
				this.SetPitch();
			}
		}

		public GridInfo()
		{
			this.PixelMap = new Vector2[11, 11];
			this.RealMap = new Vector2[11, 11];
			this.RefMap = new Vector2[11, 11];
			this.GridCol = 11;
			this.GridRow = 11;
			this.PitchRow = 5f;
			this.PitchCol = 5f;
		}

		private void SetGrid()
		{
			this.PixelMap = new Vector2[this.GridRow, this.GridCol];
			this.RealMap = new Vector2[this.GridRow, this.GridCol];
			this.RefMap = new Vector2[this.GridRow, this.GridCol];
		}

		private void SetPitch()
		{
			float num = (float)(-this.gridCol / 2) * this.pitchCol;
			int num2 = -this.gridRow / 2;
			float pitchRow2 = this.pitchRow;
			for (int i = 0; i < this.gridRow; i++)
			{
				for (int j = 0; j < this.gridCol; j++)
				{
					this.RefMap[i, j].X = num + (float)j * this.pitchCol;
					this.RefMap[i, j].Y = num + (float)i * this.pitchRow;
				}
			}
		}
		public void Reset()
		{
			for (int i = 0; i < this.gridRow; i++)
			{
				for (int j = 0; j < this.gridCol; j++)
				{
					this.RealMap[i, j].X = float.MinValue;
					this.RealMap[i, j].Y = float.MinValue;
					this.PixelMap[i, j].X = float.MinValue;
					this.PixelMap[i, j].Y = float.MinValue;
				}
			}
		}
	}
}
