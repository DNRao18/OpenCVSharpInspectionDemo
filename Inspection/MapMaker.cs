using System;
using System.Collections.Generic;
using System.Numerics;
using OpenCvSharp;

namespace VisionInspection.Inspection
{
	internal class MapMaker
	{
		private NeighborChecker[] neighbors;

		private List<Vector2> points;

		private int ptSize;

		private int row;

		private int col;

		private int pitchRow;

		private int pitchCol;

		/// <summary>
		///
		/// </summary>
		/// <param name="data"> central data</param>
		/// <param name="size">Data size</param>
		/// <param name="pitchRow">Pointer Y-direction pixel distance</param>
		/// <param name="pitchCol">Pointer X-direction pixel distance</param>
		/// <param name="row">Number of pointer Y direction data</param>
		/// <param name="col">Number of data in the X direction of the pointer</param>
		public MapMaker(List<Vector2> data, int size, int row, int col, int pitchRow, int pitchCol)
		{
			this.points = data;
			this.ptSize = size;
			this.neighbors = new NeighborChecker[size];
			this.row = row;
			this.col = col;
			this.pitchRow = pitchRow;
			this.pitchCol = pitchCol;
		}

		public Vector2[,] Run()
		{
			this.searchNeighborPt();
			int num = this.col * 3;
			int num2 = this.row * 3;
			bool[] array = new bool[this.ptSize];
			array.Initialize();
			int[,] array2 = new int[num2, num];
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < num; j++)
				{
					array2[i, j] = -1;
				}
			}
			int num3 = this.col;
			int num4 = this.row;
			array2[num4, num3] = this.neighbors[0].Index;
			if (this.neighbors[0].UpIdx != -1)
			{
				array2[num4 - 1, num3] = this.neighbors[0].UpIdx;
			}
			if (this.neighbors[0].DownIdx != -1)
			{
				array2[num4 + 1, num3] = this.neighbors[0].DownIdx;
			}
			if (this.neighbors[0].LeftIdx != -1)
			{
				array2[num4, num3 - 1] = this.neighbors[0].LeftIdx;
			}
			if (this.neighbors[0].RightIdx != -1)
			{
				array2[num4, num3 + 1] = this.neighbors[0].RightIdx;
			}
			array[0] = true;
			int num5 = 0;
			bool flag = false;
			do
			{
				flag = false;
				for (int k = 0; k < this.ptSize; k++)
				{
					if (array[k])
					{
						continue;
					}
					for (int l = 0; l < num2; l++)
					{
						for (int m = 0; m < num; m++)
						{
							if (array2[l, m] == this.neighbors[k].Index)
							{
								this.checkAndUpdateMap(ref array2[l - 1, m], this.neighbors[k].UpIdx);
								this.checkAndUpdateMap(ref array2[l + 1, m], this.neighbors[k].DownIdx);
								this.checkAndUpdateMap(ref array2[l, m - 1], this.neighbors[k].LeftIdx);
								this.checkAndUpdateMap(ref array2[l, m + 1], this.neighbors[k].RightIdx);
								array[k] = true;
								flag = true;
								break;
							}
						}
						if (array[k])
						{
							break;
						}
					}
				}
				num5++;
			}
			while (num5 <= this.col * this.row && flag);
			int num6 = int.MaxValue;
			int num7 = int.MaxValue;
			int num8 = int.MinValue;
			int num9 = int.MinValue;
			for (int n = 0; n < num2; n++)
			{
				for (int num10 = 0; num10 < num; num10++)
				{
					if (array2[n, num10] != -1)
					{
						if (num10 < num6)
						{
							num6 = num10;
						}
						if (num10 > num8)
						{
							num8 = num10;
						}
						if (n < num7)
						{
							num7 = n;
						}
						if (n > num9)
						{
							num9 = n;
						}
					}
				}
			}
			Vector2[,] array3 = new Vector2[num9 - num7 + 1, num8 - num6 + 1];
			int num11 = num7;
			int num12 = 0;
			while (num11 <= num9)
			{
				int num13 = num6;
				int num14 = 0;
				while (num13 <= num8)
				{
					int num15 = array2[num11, num13];
					if (num15 != -1)
					{
						array3[num12, num14] = this.points[num15];
					}
					num13++;
					num14++;
				}
				num11++;
				num12++;
			}
			return array3;
		}

		private void searchNeighborPt()
		{
			float num = this.pitchRow;
			float num2 = this.pitchCol;
			List<float> list = new List<float>();
			List<float> list2 = new List<float>();
			for (int i = 0; i < this.ptSize; i++)
			{
				float num3 = float.MaxValue;
				float num4 = float.MaxValue;
				Vector2 vector = this.points[i];
				int num5 = -1;
				int num6 = -1;
				for (int j = 0; j < this.ptSize; j++)
				{
					if (i == j)
					{
						continue;
					}
					float num7 = Math.Abs(vector.X - this.points[j].X);
					float num8 = Math.Abs(vector.Y - this.points[j].Y);
					float num9 = (float)Math.Sqrt(num7 * num7 + num8 * num8);
					if (num7 > num8)
					{
						if (num3 > num9)
						{
							num3 = num9;
							num5 = j;
						}
					}
					else if (num4 > num9)
					{
						num4 = num9;
						num6 = j;
					}
				}
				if (num5 != -1)
				{
					list.Add(num3);
				}
				if (num6 != -1)
				{
					list2.Add(num4);
				}
			}
			float num10 = 0f;
			float num11 = 0f;
			float num12 = -1f;
			float num13 = -1f;
			for (int k = 0; k < list.Count; k++)
			{
				num10 += list[k];
			}
			num12 = num10 / (float)list.Count;
			for (int l = 0; l < list2.Count; l++)
			{
				num11 += list2[l];
			}
			num13 = num11 / (float)list2.Count;
			float num14 = 0f;
			float num15 = 0f;
			for (int m = 0; m < list.Count; m++)
			{
				num14 += (list[m] - num12) * (list[m] - num12);
			}
			float num16 = (float)Math.Sqrt(num14 / (float)list.Count);
			for (int n = 0; n < list2.Count; n++)
			{
				num15 += (list2[n] - num13) * (list2[n] - num13);
			}
			float num17 = (float)Math.Sqrt(num15 / (float)list2.Count);
			if ((double)num16 < 5.0 && (double)num17 < 5.0)
			{
				if ((double)Math.Abs(num - num12) > 5.0)
				{
					num = num12;
				}
				if ((double)Math.Abs(num2 - num13) > 5.0)
				{
					num2 = num13;
				}
			}
			for (int num18 = 0; num18 < this.ptSize; num18++)
			{
				NeighborChecker neighborChecker = new NeighborChecker();
				neighborChecker.Index = num18;
				Vector2 vector2 = this.points[num18];
				float num19 = float.MaxValue;
				int num20 = -1;
				for (int num21 = 0; num21 < this.ptSize; num21++)
				{
					if (num18 != num21)
					{
						float num22 = this.points[num21].X - vector2.X;
						float num23 = this.points[num21].Y - vector2.Y;
						float num24 = Cv2.FastAtan2(num23, num22);
						float num25 = (float)Math.Sqrt(num22 * num22 + num23 * num23);
						if (vector2.Y > this.points[num21].Y && num24 > 240f && num24 < 300f && num19 > num25 && this.checkInrange(num25, num2, 0.15f))
						{
							num19 = num25;
							num20 = num21;
						}
					}
				}
				if (num20 != -1)
				{
					neighborChecker.UpIdx = num20;
				}
				num19 = 999999f;
				num20 = -1;
				for (int num26 = 0; num26 < this.ptSize; num26++)
				{
					if (num18 != num26)
					{
						float num22 = this.points[num26].X - vector2.X;
						float num23 = this.points[num26].Y - vector2.Y;
						float num24 = Cv2.FastAtan2(num23, num22);
						float num25 = (float)Math.Sqrt(num22 * num22 + num23 * num23);
						if (vector2.Y < this.points[num26].Y && (double)num24 > 60.0 && (double)num24 < 120.0 && num19 > num25 && this.checkInrange(num25, num2, 0.15f))
						{
							num19 = num25;
							num20 = num26;
						}
					}
				}
				if (num20 != -1)
				{
					neighborChecker.DownIdx = num20;
				}
				num19 = 999999f;
				num20 = -1;
				for (int num27 = 0; num27 < this.ptSize; num27++)
				{
					if (num18 != num27)
					{
						float num22 = this.points[num27].X - vector2.X;
						float num23 = this.points[num27].Y - vector2.Y;
						float num24 = Cv2.FastAtan2(num23, num22);
						float num25 = (float)Math.Sqrt(num22 * num22 + num23 * num23);
						if (vector2.X > this.points[num27].X && (double)num24 > 150.0 && (double)num24 < 210.0 && num19 > num25 && this.checkInrange(num25, num, 0.15f))
						{
							num19 = num25;
							num20 = num27;
						}
					}
				}
				if (num20 != -1)
				{
					neighborChecker.LeftIdx = num20;
				}
				num19 = 999999f;
				num20 = -1;
				for (int num28 = 0; num28 < this.ptSize; num28++)
				{
					if (num18 != num28)
					{
						float num22 = this.points[num28].X - vector2.X;
						float num23 = this.points[num28].Y - vector2.Y;
						float num24 = Cv2.FastAtan2(num23, num22);
						float num25 = (float)Math.Sqrt(num22 * num22 + num23 * num23);
						if (vector2.X < this.points[num28].X && ((double)num24 > 330.0 || (double)num24 < 30.0) && num19 > num25 && this.checkInrange(num25, num, 0.15f))
						{
							num19 = num25;
							num20 = num28;
						}
					}
				}
				if (num20 != -1)
				{
					neighborChecker.RightIdx = num20;
				}
				this.neighbors[num18] = neighborChecker;
			}
		}

		private bool checkAndUpdateMap(ref int mapData, int data)
		{
			if (mapData == -1)
			{
				mapData = data;
			}
			else if (mapData != data)
			{
				return false;
			}
			return true;
		}

		private bool checkInrange(float data, float refDist, float range)
		{
			bool result = true;
			float num = refDist * (1f + range);
			float num2 = refDist * (1f - range);
			if (data > num || data < num2)
			{
				result = false;
			}
			return result;
		}
	}
}
