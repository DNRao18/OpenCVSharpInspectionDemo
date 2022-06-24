using System.Collections.Generic;
using System.IO;
using System.Numerics;
using OpenCvSharp;
using VisionInspection.Inspection;

namespace VisionInspection
{
	internal class InspCrosshair
	{
		private Mat binaryImg;

		private Mat img_labels;

		private Mat stats;

		private Mat centroids;

		private int row;

		private int col;

		private float realPitchRow;

		private float realPitchCol;

		private int pxlPitchRow;

		private int pxlPitchCol;

		private int dpi;

		private float pxlRes;

		private int crosshairWidth;

		private int crosshairHeight;

		private List<Vector2> tmpPoints;

		public InspCrosshair(int dpi, int row, int col, float pitchRow, float pitchCol, float objWidth, float objHeight)
		{
			this.binaryImg = new Mat();
			this.img_labels = new Mat();
			this.stats = new Mat();
			this.centroids = new Mat();
			this.dpi = dpi;
			this.row = row;
			this.col = col;
			this.realPitchRow = pitchRow;
			this.realPitchCol = pitchCol;
			this.pxlRes = 25.4f / (float)dpi;
			this.crosshairWidth = (int)(objWidth / this.pxlRes);
			this.crosshairHeight = (int)(objHeight / this.pxlRes);
			this.pxlPitchRow = (int)(pitchRow / this.pxlRes);
			this.pxlPitchCol = (int)(pitchCol / this.pxlRes);
		}

		~InspCrosshair()
		{
			this.binaryImg.Dispose();
			this.img_labels.Dispose();
			this.stats.Dispose();
			this.centroids.Dispose();
		}
		/// <summary>
		/// Remove noise component.
		/// The size of the deletion is limited to a maximum of 20 pixels.
		/// </summary>
		/// <param name="binary"></param>
		/// <param name="limit"></param>
		private void filterWithPixel(ref Mat binary, int limit)
		{
			if (limit > 20)
			{
				limit = 20;
			}
			int num = Cv2.ConnectedComponentsWithStats(binary, this.img_labels, this.stats, this.centroids, PixelConnectivity.Connectivity8, 4);
			for (int i = 1; i < num; i++)
			{
				int num2 = this.stats.At<int>(i, 4);
				int num3 = this.stats.At<int>(i, 0);
				int num4 = this.stats.At<int>(i, 1);
				int num5 = this.stats.At<int>(i, 2);
				int num6 = this.stats.At<int>(i, 3);
				if (num2 >= limit)
				{
					continue;
				}
				for (int j = num4; j < num4 + num6; j++)
				{
					for (int k = num3; k < num3 + num5; k++)
					{
						if (this.img_labels.At<int>(j, k) == i)
						{
							binary.Set(j, k, (byte)0);
						}
					}
				}
			}
		}
		/// <summary>
		/// A size smaller than the set cross mark size is deleted from the image.
		/// </summary>
		/// <param name="binary"></param>
		/// <param name="minSizeX"></param>
		/// <param name="minSizeY"></param>
		private void filterWithSize(ref Mat binary, int minSizeX, int minSizeY, int maxSizeX, int maxSizeY)
		{
			int num = Cv2.ConnectedComponentsWithStats(this.binaryImg, this.img_labels, this.stats, this.centroids, PixelConnectivity.Connectivity8, 4);
			for (int i = 1; i < num; i++)
			{
				int reference =  this.stats.At<int>(i, 4);
				int num2 = this.stats.At<int>(i, 0);
				int num3 = this.stats.At<int>(i, 1);
				int num4 = this.stats.At<int>(i, 2);
				int num5 = this.stats.At<int>(i, 3);
				if (num4 >= minSizeX && num5 >= minSizeY && num4 <= maxSizeX && num5 <= maxSizeY)
				{
					continue;
				}
				for (int j = num3; j < num3 + num5; j++)
				{
					for (int k = num2; k < num2 + num4; k++)
					{
						if (this.img_labels.At<int>(j, k) == i)
						{
							this.binaryImg.Set(j, k, (byte)0);
						}
					}
				}
			}
		}

		public bool Inspection(Mat inImg, bool isBright, int th, eCrossLine lineType, ref Vector2[,] realMap, ref Vector2[,] pxlMap)
		{
			bool result = true;
			switch (lineType)
			{
			case eCrossLine.eThinLine:
				if (!isBright)
				{
					Cv2.Threshold(inImg, this.binaryImg, th, 255.0, ThresholdTypes.Triangle);
				}
				else
				{
					Cv2.Threshold(inImg, this.binaryImg, th, 255.0, ThresholdTypes.BinaryInv | ThresholdTypes.Triangle);
				}
				break;
			case eCrossLine.eHatchLine:
				if (!isBright)
				{
					Cv2.Threshold(inImg, this.binaryImg, th, 255.0, ThresholdTypes.Binary);
				}
				else
				{
					Cv2.Threshold(inImg, this.binaryImg, th, 255.0, ThresholdTypes.BinaryInv);
				}
				break;
			}
			Cv2.ImWrite(Path.GetTempPath() + "\\gray_image.bmp", inImg);
			Cv2.ImWrite(Path.GetTempPath() + "\\binary_image.bmp", this.binaryImg);
			this.filterWithPixel(limit: (int)(0.2f / this.pxlRes), binary: ref this.binaryImg);
			Cv2.ImWrite(Path.GetTempPath() + "\\binary_image_filtered_step1.bmp", this.binaryImg);
			Cv2.MorphologyEx(element: Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)), src: this.binaryImg, dst: this.binaryImg, op: MorphTypes.Close);
			Cv2.ImWrite(Path.GetTempPath() + "\\binary_image_filtered_step2.bmp", this.binaryImg);
			this.filterWithSize(minSizeX: (int)((double)this.crosshairWidth * 0.4), minSizeY: (int)((double)this.crosshairHeight * 0.4), maxSizeX: (int)((double)this.crosshairWidth * 1.5), maxSizeY: (int)((double)this.crosshairHeight * 1.5), binary: ref this.binaryImg);
			Cv2.ImWrite(Path.GetTempPath() + "\\binary_image_filter.bmp", this.binaryImg);
			switch (lineType)
			{
			case eCrossLine.eThinLine:
				this.findCrosshair(this.binaryImg);
				break;
			case eCrossLine.eHatchLine:
			{
				int num = Cv2.ConnectedComponentsWithStats(this.binaryImg, this.img_labels, this.stats, this.centroids, PixelConnectivity.Connectivity8, 4);
				int num2 = (int)((double)this.crosshairWidth * 0.8);
				int num3 = (int)((double)this.crosshairHeight * 0.8);
				int num4 = (int)((double)this.crosshairWidth * 1.2);
				int num5 = (int)((double)this.crosshairHeight * 1.2);
				this.tmpPoints?.Clear();
				this.tmpPoints = new List<Vector2>();
				for (int i = 1; i < num; i++)
				{
					int num6 = this.stats.At<int>(i, 2);
					int num7 = this.stats.At<int>(i, 3);
					if (num6 >= num2 && num6 <= num4 && num7 >= num3 && num7 <= num5)
					{
						Vector2 vector = default(Vector2);
						vector.X = (float)this.centroids.At<double>(i, 0);
						vector.Y = (float)this.centroids.At<double>(i, 1);
						Vector2 item = this.findCrosshairCenter(this.binaryImg, 4, vector);
						if (item.X != 0f && item.Y != 0f)
						{
							this.tmpPoints.Add(item);
						}
						else
						{
							this.tmpPoints.Add(vector);
						}
					}
				}
				break;
			}
			}
			Cv2.ImWrite(Path.GetTempPath() + "\\binary_image_removed.bmp", this.binaryImg);
			pxlMap = new MapMaker(this.tmpPoints, this.tmpPoints.Count, this.row, this.col, this.pxlPitchRow, this.pxlPitchCol).Run();
			int length = pxlMap.GetLength(0);
			int length2 = pxlMap.GetLength(1);
			if (length < this.row || length2 < this.col)
			{
				return false;
			}
			Vector2 vector2 = pxlMap[this.row / 2, this.col / 2];
			Vector2[,] array = new Vector2[length, length2];
			for (int j = 0; j < length; j++)
			{
				for (int k = 0; k < length2; k++)
				{
					array[j, k].X = (pxlMap[j, k].X - vector2.X) * this.pxlRes;
					array[j, k].Y = (pxlMap[j, k].Y - vector2.Y) * this.pxlRes;
				}
			}
			realMap = array;
			if (this.row * this.col > this.tmpPoints.Count)
			{
				return false;
			}
			return result;
		}

		private void findCrosshair(Mat srcImg)
		{
			int cols = srcImg.Cols;
			int rows = srcImg.Rows;
			int num = this.crosshairWidth / 2;
			int num2 = this.crosshairHeight / 2;
			int num3 = (int)((float)num * 0.5f);
			int num4 = (int)((float)num2 * 0.5f);
			int num5 = (int)((float)num * 1.3f);
			int num6 = (int)((float)num2 * 1.3f);
			int num7 = (int)((double)this.crosshairWidth * 0.1);
			int[] array = new int[4];
			List<Vector2> list = new List<Vector2>();
			this.tmpPoints?.Clear();
			this.tmpPoints = new List<Vector2>();
			Vector2 vector = default(Vector2);
			for (int i = num4; i < rows - num4; i++)
			{
				for (int j = num3; j < cols - num3; j++)
				{
					array.Initialize();
					if (srcImg.At<byte>(i, j) != byte.MaxValue)
					{
						continue;
					}
					array[0] += srcImg.At<byte>(i - num7, j - 1);
					array[0] += srcImg.At<byte>(i - num7, j);
					array[0] += srcImg.At<byte>(i - num7, j + 1);
					array[0] /= 255;
					array[1] += srcImg.At<byte>(i + num7, j - 1);
					array[1] += srcImg.At<byte>(i + num7, j);
					array[1] += srcImg.At<byte>(i + num7, j + 1);
					array[1] /= 255;
					array[2] += srcImg.At<byte>(i - 1, j - num7);
					array[2] += srcImg.At<byte>(i, j - num7);
					array[2] += srcImg.At<byte>(i + 1, j - num7);
					array[2] /= 255;
					array[3] += srcImg.At<byte>(i - 1, j + num7);
					array[3] += srcImg.At<byte>(i, j + num7);
					array[3] += srcImg.At<byte>(i + 1, j + num7);
					array[3] /= 255;
					int num8 = 0;
					bool flag = false;
					for (int k = 0; k < 4; k++)
					{
						if (array[k] > 0)
						{
							num8++;
						}
					}
					if (num8 == 4 || num8 == 3 || num8 == 2)
					{
						flag = true;
					}
					if (!flag)
					{
						continue;
					}
					switch (num8)
					{
					case 2:
						if (array[0] > 0 && array[2] > 0)
						{
							array.Initialize();
							array[0] += srcImg.At<byte>(i - num4, j - 1);
							array[0] += srcImg.At<byte>(i - num4, j);
							array[0] += srcImg.At<byte>(i - num4, j + 1);
							array[0] /= 255;
							array[2] += srcImg.At<byte>(i - 1, j - num3);
							array[2] += srcImg.At<byte>(i, j - num3);
							array[2] += srcImg.At<byte>(i + 1, j - num3);
							array[2] /= 255;
							if (array[0] == 0 || array[2] == 0)
							{
								flag = false;
							}
						}
						else if (array[0] > 0 && array[3] > 0)
						{
							array.Initialize();
							array[0] += srcImg.At<byte>(i - num4, j - 1);
							array[0] += srcImg.At<byte>(i - num4, j);
							array[0] += srcImg.At<byte>(i - num4, j + 1);
							array[0] /= 255;
							array[3] += srcImg.At<byte>(i - 1, j + num3);
							array[3] += srcImg.At<byte>(i, j + num3);
							array[3] += srcImg.At<byte>(i + 1, j + num3);
							array[3] /= 255;
							if (array[0] == 0 || array[3] == 0)
							{
								flag = false;
							}
						}
						else if (array[1] > 0 && array[2] > 0)
						{
							array.Initialize();
							array[1] += srcImg.At<byte>(i + num4, j - 1);
							array[1] += srcImg.At<byte>(i + num4, j);
							array[1] += srcImg.At<byte>(i + num4, j + 1);
							array[1] /= 255;
							array[2] += srcImg.At<byte>(i - 1, j - num3);
							array[2] += srcImg.At<byte>(i, j - num3);
							array[2] += srcImg.At<byte>(i + 1, j - num3);
							array[2] /= 255;
							if (array[1] == 0 || array[2] == 0)
							{
								flag = false;
							}
						}
						else if (array[1] > 0 && array[3] > 0)
						{
							array.Initialize();
							array[1] += srcImg.At<byte>(i + num4, j - 1);
							array[1] += srcImg.At<byte>(i + num4, j);
							array[1] += srcImg.At<byte>(i + num4, j + 1);
							array[1] /= 255;
							array[3] += srcImg.At<byte>(i - 1, j + num3);
							array[3] += srcImg.At<byte>(i, j + num3);
							array[3] += srcImg.At<byte>(i + 1, j + num3);
							array[3] /= 255;
							if (array[1] == 0 || array[3] == 0)
							{
								flag = false;
							}
						}
						else
						{
							flag = false;
						}
						break;
					case 3:
						if (array[0] > 0 && array[2] > 0 && array[3] > 0)
						{
							array.Initialize();
							array[0] += srcImg.At<byte>(i - num4, j - 1);
							array[0] += srcImg.At<byte>(i - num4, j);
							array[0] += srcImg.At<byte>(i - num4, j + 1);
							array[0] /= 255;
							array[2] += srcImg.At<byte>(i - 1, j - num3);
							array[2] += srcImg.At<byte>(i, j - num3);
							array[2] += srcImg.At<byte>(i + 1, j - num3);
							array[2] /= 255;
							array[3] += srcImg.At<byte>(i - 1, j + num3);
							array[3] += srcImg.At<byte>(i, j + num3);
							array[3] += srcImg.At<byte>(i + 1, j + num3);
							array[3] /= 255;
							if (array[0] == 0 || array[2] == 0 || array[3] == 0)
							{
								flag = false;
							}
						}
						else if (array[0] > 0 && array[1] > 0 && array[3] > 0)
						{
							array.Initialize();
							array[0] += srcImg.At<byte>(i - num4, j - 1);
							array[0] += srcImg.At<byte>(i - num4, j);
							array[0] += srcImg.At<byte>(i - num4, j + 1);
							array[0] /= 255;
							array[1] += srcImg.At<byte>(i + num4, j - 1);
							array[1] += srcImg.At<byte>(i + num4, j);
							array[1] += srcImg.At<byte>(i + num4, j + 1);
							array[1] /= 255;
							array[3] += srcImg.At<byte>(i - 1, j + num3);
							array[3] += srcImg.At<byte>(i, j + num3);
							array[3] += srcImg.At<byte>(i + 1, j + num3);
							array[3] /= 255;
							if (array[0] == 0 || array[1] == 0 || array[3] == 0)
							{
								flag = false;
							}
						}
						else if (array[1] > 0 && array[2] > 0 && array[3] > 0)
						{
							array.Initialize();
							array[1] += srcImg.At<byte>(i + num4, j - 1);
							array[1] += srcImg.At<byte>(i + num4, j);
							array[1] += srcImg.At<byte>(i + num4, j + 1);
							array[1] /= 255;
							array[2] += srcImg.At<byte>(i - 1, j - num3);
							array[2] += srcImg.At<byte>(i, j - num3);
							array[2] += srcImg.At<byte>(i + 1, j - num3);
							array[2] /= 255;
							array[3] += srcImg.At<byte>(i - 1, j + num3);
							array[3] += srcImg.At<byte>(i, j + num3);
							array[3] += srcImg.At<byte>(i + 1, j + num3);
							array[3] /= 255;
							if (array[1] == 0 || array[2] == 0 || array[3] == 0)
							{
								flag = false;
							}
						}
						else if (array[0] > 0 && array[1] > 0 && array[2] > 0)
						{
							array.Initialize();
							array[0] += srcImg.At<byte>(i - num4, j - 1);
							array[0] += srcImg.At<byte>(i - num4, j);
							array[0] += srcImg.At<byte>(i - num4, j + 1);
							array[0] /= 255;
							array[1] += srcImg.At<byte>(i + num4, j - 1);
							array[1] += srcImg.At<byte>(i + num4, j);
							array[1] += srcImg.At<byte>(i + num4, j + 1);
							array[1] /= 255;
							array[2] += srcImg.At<byte>(i - 1, j - num3);
							array[2] += srcImg.At<byte>(i, j - num3);
							array[2] += srcImg.At<byte>(i + 1, j - num3);
							array[2] /= 255;
							if (array[0] == 0 || array[1] == 0 || array[2] == 0)
							{
								flag = false;
							}
						}
						else
						{
							flag = false;
						}
						break;
					case 4:
						array[0] += srcImg.At<byte>(i - num4, j - 1);
						array[0] += srcImg.At<byte>(i - num4, j);
						array[0] += srcImg.At<byte>(i - num4, j + 1);
						array[0] /= 255;
						array[1] += srcImg.At<byte>(i + num4, j - 1);
						array[1] += srcImg.At<byte>(i + num4, j);
						array[1] += srcImg.At<byte>(i + num4, j + 1);
						array[1] /= 255;
						array[2] += srcImg.At<byte>(i - 1, j - num3);
						array[2] += srcImg.At<byte>(i, j - num3);
						array[2] += srcImg.At<byte>(i + 1, j - num3);
						array[2] /= 255;
						array[3] += srcImg.At<byte>(i - 1, j + num3);
						array[3] += srcImg.At<byte>(i, j + num3);
						array[3] += srcImg.At<byte>(i + 1, j + num3);
						array[3] /= 255;
						if (array[0] == 0 || array[1] == 0 || array[2] == 0 || array[3] == 0)
						{
							flag = false;
						}
						break;
					}
					if (!flag)
					{
						continue;
					}
					vector.X = j;
					vector.Y = i;
					vector = this.findCrosshairCenter(srcImg, num8, vector);
					int num9 = (int)vector.X;
					int num10 = (int)vector.Y;
					if (vector.X == 0f || vector.Y == 0f)
					{
						continue;
					}
					list.Add(vector);
					int num11 = num9 - num5;
					if (num11 < 0)
					{
						num11 = 0;
					}
					if (num11 >= srcImg.Cols)
					{
						num11 = srcImg.Cols - 1;
					}
					int num12 = num9 + num5;
					if (num12 < 0)
					{
						num12 = 0;
					}
					if (num12 >= srcImg.Cols)
					{
						num12 = srcImg.Cols - 1;
					}
					int num13 = num10 - num6;
					if (num13 < 0)
					{
						num13 = 0;
					}
					if (num13 >= srcImg.Rows)
					{
						num13 = srcImg.Rows - 1;
					}
					int num14 = num10 + num6;
					if (num14 < 0)
					{
						num14 = 0;
					}
					if (num14 >= srcImg.Rows)
					{
						num14 = srcImg.Rows - 1;
					}
					for (int l = num13; l <= num14; l++)
					{
						for (int m = num11; m <= num12; m++)
						{
							srcImg.Set(l, m, (byte)0);
						}
					}
				}
			}
			if (list.Count > 0)
			{
				for (int n = 0; n < list.Count; n++)
				{
					this.tmpPoints.Add(list[n]);
				}
			}
			list.Clear();
		}

		private Vector2 findCrosshairCenter(Mat img, int type, Vector2 pt)
		{
			double num = this.searchLineCenter(img, pt, -30, -20, 15, 0);
			double num2 = this.searchLineCenter(img, pt, 20, 30, 15, 0);
			double num3 = this.searchLineCenter(img, pt, -30, -20, 15, 1);
			double num4 = this.searchLineCenter(img, pt, 20, 30, 15, 1);
			Vector2 result = default(Vector2);
			result.X = (result.Y = 0f);
			int num5 = 0;
			int num6 = -1;
			if (num != -1.0)
			{
				num5++;
			}
			else
			{
				num6 = 0;
			}
			if (num2 != -1.0)
			{
				num5++;
			}
			else
			{
				num6 = 1;
			}
			if (num3 != -1.0)
			{
				num5++;
			}
			else
			{
				num6 = 2;
			}
			if (num4 != -1.0)
			{
				num5++;
			}
			else
			{
				num6 = 3;
			}
			switch (type)
			{
			case 2:
				if (num5 == 2)
				{
					if (num == -1.0 && num2 != -1.0)
					{
						result.X = (float)num2;
					}
					else if (num != -1.0 && num2 == -1.0)
					{
						result.X = (float)num;
					}
					if (num3 == -1.0 && num4 != -1.0)
					{
						result.Y = (float)num4;
					}
					else if (num3 != -1.0 && num4 == -1.0)
					{
						result.Y = (float)num3;
					}
				}
				break;
			case 3:
				if (num5 == 3)
				{
					switch (num6)
					{
					case 0:
						result.X = (float)num2;
						result.Y = (float)(num3 + num4) / 2f;
						break;
					case 1:
						result.X = (float)num;
						result.Y = (float)(num3 + num4) / 2f;
						break;
					case 2:
						result.X = (float)(num + num2) / 2f;
						result.Y = (float)num4;
						break;
					case 3:
						result.X = (float)(num + num2) / 2f;
						result.Y = (float)num3;
						break;
					}
				}
				break;
			case 4:
				if (num != -1.0 && num2 != -1.0 && num3 != -1.0 && num4 != -1.0)
				{
					result.X = (float)(num + num2) / 2f;
					result.Y = (float)(num3 + num4) / 2f;
				}
				break;
			}
			return result;
		}

		private double searchLineCenter(Mat img, Vector2 pt, int offsetStart, int offsetEnd, int searchLength, int dir)
		{
			int num = 0;
			int num2 = 0;
			int num3 = (int)pt.X;
			int num4 = (int)pt.Y;
			double result = -1.0;
			int width = img.Width;
			int height = img.Height;
			switch (dir)
			{
			case 0:
			{
				int num5 = 0;
				for (int m = offsetStart; m <= offsetEnd; m++)
				{
					for (int n = -searchLength; n <= searchLength; n++)
					{
						if (num4 + m >= 0 && num4 + m < height && num3 + n >= 0 && num3 + n <= width - 1 && img.At<byte>(num4 + m, num3 + n) == 0 && img.At<byte>(num4 + m, num3 + n + 1) == byte.MaxValue)
						{
							num += n + 1;
							num5++;
							break;
						}
					}
				}
				if (num5 != 0)
				{
					num /= num5;
				}
				if (num5 == 0)
				{
					break;
				}
				num5 = 0;
				for (int num6 = offsetStart; num6 <= offsetEnd; num6++)
				{
					for (int num7 = num; num7 <= searchLength; num7++)
					{
						if (num4 + num6 >= 0 && num4 + num6 < height && num3 + num7 >= 0 && num3 + num7 <= width - 1 && img.At<byte>(num4 + num6, num3 + num7) == byte.MaxValue && img.At<byte>(num4 + num6, num3 + num7 + 1) == 0)
						{
							num2 += num7;
							num5++;
							break;
						}
					}
				}
				if (num5 != 0)
				{
					num2 /= num5;
				}
				if (num5 != 0)
				{
					result = (double)(num + num2) / 2.0 + (double)pt.X;
				}
				break;
			}
			case 1:
			{
				int num5 = 0;
				for (int i = offsetStart; i <= offsetEnd; i++)
				{
					for (int j = -searchLength; j <= searchLength; j++)
					{
						if (num4 + i >= 0 && num4 + i < height && num3 + j >= 0 && num3 + j <= width - 1 && img.At<byte>(num4 + j, num3 + i) == 0 && img.At<byte>(num4 + j + 1, num3 + i) == byte.MaxValue)
						{
							num += j + 1;
							num5++;
							break;
						}
					}
				}
				if (num5 != 0)
				{
					num /= num5;
				}
				if (num5 == 0)
				{
					break;
				}
				num5 = 0;
				for (int k = offsetStart; k <= offsetEnd; k++)
				{
					for (int l = num; l <= searchLength; l++)
					{
						if (num4 + k >= 0 && num4 + k < height && num3 + l >= 0 && num3 + l <= width - 1 && img.At<byte>(num4 + l, num3 + k) == byte.MaxValue && img.At<byte>(num4 + l + 1, num3 + k) == 0)
						{
							num2 += l;
							num5++;
							break;
						}
					}
				}
				if (num5 != 0)
				{
					num2 /= num5;
				}
				if (num5 != 0)
				{
					result = (double)(num + num2) / 2.0 + (double)pt.Y;
				}
				break;
			}
			}
			return result;
		}
	}
}
