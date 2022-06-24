using System.Collections.Generic;
using System.Numerics;
using OpenCvSharp;
using VisionInspection.Inspection;

namespace VisionInspection
{
	internal class InspCircle
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

		public InspCircle(int dpi, int row, int col, float pitchRow, float pitchCol)
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
			this.pxlPitchRow = (int)(pitchRow / this.pxlRes);
			this.pxlPitchCol = (int)(pitchCol / this.pxlRes);
		}

		~InspCircle()
		{
			this.binaryImg.Dispose();
			this.img_labels.Dispose();
			this.stats.Dispose();
			this.centroids.Dispose();
		}

		public bool Inspection(Mat inImg, bool isBright, int th, ref Vector2[,] realMap, ref Vector2[,] pxlMap)
		{
			bool result = true;
			if (!isBright)
			{
				Cv2.Threshold(inImg, this.binaryImg, th, 255.0, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);
			}
			else
			{
				Cv2.Threshold(inImg, this.binaryImg, th, 255.0, ThresholdTypes.Otsu);
			}
			int num = Cv2.ConnectedComponentsWithStats(this.binaryImg, this.img_labels, this.stats, this.centroids, PixelConnectivity.Connectivity8, 4);
			int[] data = new int[num - 1];
			for (int i = 0; i < num - 1; i++)
			{
				data[i] = this.stats.At<int>(i + 1, 4);
			}
			new Sort().Run(ref data, num - 1);
			double num2 = (double)data[(num - 1) / 2] * 0.8;
			double num3 = (double)data[(num - 1) / 2] * 1.2;
			List<Vector2> list = new List<Vector2>();
			for (int j = 1; j < num; j++)
			{
				int num4 = this.stats.At<int>(j, 4);
				if (!((double)num4 < num2) && !((double)num4 > num3))
				{
					Vector2 item = default(Vector2);
					item.X = (float)this.centroids.At<double>(j, 0);
					item.Y = (float)this.centroids.At<double>(j, 1);
					list.Add(item);
				}
			}
			pxlMap = new MapMaker(list, list.Count, this.row, this.col, this.pxlPitchRow, this.pxlPitchCol).Run();
			int length = pxlMap.GetLength(0);
			int length2 = pxlMap.GetLength(1);
			Vector2[,] array = new Vector2[length, length2];
			for (int k = 0; k < length; k++)
			{
				for (int l = 0; l < length2; l++)
				{
					array[k, l].X = pxlMap[k, l].X * this.pxlRes;
					array[k, l].Y = pxlMap[k, l].Y * this.pxlRes;
				}
			}
			realMap = array;
			return result;
		}
	}
}
