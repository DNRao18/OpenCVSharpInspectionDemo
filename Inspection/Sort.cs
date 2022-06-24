namespace VisionInspection.Inspection
{
	internal class Sort
	{
		public bool Run(ref int[] data, int size, sortType type = sortType.typeBubble)
		{
			switch (type)
			{
			case sortType.typeBubble:
				this.BubbleSort(ref data, size);
				break;
			case sortType.typeHeap:
				this.HeapSort(ref data, size);
				break;
			case sortType.typeInsertion:
				this.InsertionSort(ref data, size);
				break;
			case sortType.typeMerge:
				this.MergeSort(ref data, size);
				break;
			case sortType.typeQuick:
				this.QuickSort(ref data, size);
				break;
			case sortType.typeSelection:
				this.SelectionSort(ref data, size);
				break;
			case sortType.typeShell:
				this.ShellSort(ref data, size);
				break;
			}
			return true;
		}

		private bool BubbleSort(ref int[] data, int size)
		{
			for (int num = size - 1; num >= 0; num--)
			{
				for (int i = 1; i <= num; i++)
				{
					if (data[i - 1] > data[i])
					{
						int num2 = data[i - 1];
						data[i - 1] = data[i];
						data[i] = num2;
					}
				}
			}
			return true;
		}

		private void shfitDown(ref int[] data, int root, int bottom)
		{
			bool flag = false;
			while (root * 2 <= bottom && !flag)
			{
				int num = ((root * 2 == bottom) ? (root * 2) : ((data[root * 2] <= data[root * 2 + 1]) ? (root * 2 + 1) : (root * 2)));
				if (data[root] < data[num])
				{
					int num2 = data[root];
					data[root] = data[num];
					data[num] = num2;
					root = num;
				}
				else
				{
					flag = true;
				}
			}
		}

		private bool HeapSort(ref int[] data, int size)
		{
			for (int num = size / 2 - 1; num >= 0; num--)
			{
				this.shfitDown(ref data, num, size);
			}
			for (int num2 = size - 1; num2 >= 1; num2--)
			{
				int num3 = data[0];
				data[0] = data[num2];
				data[num2] = num3;
				this.shfitDown(ref data, 0, num2 - 1);
			}
			return true;
		}

		private bool InsertionSort(ref int[] data, int size)
		{
			for (int i = 1; i < size; i++)
			{
				int num = data[i];
				int num2 = i;
				while (num2 > 0 && data[num2 - 1] > num)
				{
					data[num2] = data[num2 - 1];
					num2--;
				}
				data[num2] = num;
			}
			return true;
		}

		private void mergeSort(ref int[] data, ref int[] temp, int left, int right)
		{
			if (right > left)
			{
				int num = (right + left) / 2;
				this.mergeSort(ref data, ref temp, left, num);
				this.mergeSort(ref data, ref temp, num + 1, right);
				this.merge(ref data, ref temp, left, num, right);
			}
		}

		private void merge(ref int[] data, ref int[] temp, int left, int mid, int right)
		{
			int num = mid - 1;
			int num2 = left;
			int num3 = right - left - 1;
			while (left <= num && mid <= right)
			{
				if (data[left] <= data[mid])
				{
					temp[num2] = data[left];
					num2++;
					left++;
				}
				else
				{
					temp[num2] = data[mid];
					num2++;
					mid++;
				}
			}
			while (left <= num)
			{
				temp[num2] = data[left];
				left++;
				num2++;
			}
			while (mid <= right)
			{
				temp[num2] = data[mid];
				mid++;
				num2++;
			}
			for (int i = 0; i < num3; i++)
			{
				data[right] = temp[right];
				right--;
			}
		}

		private bool MergeSort(ref int[] data, int size)
		{
			int[] temp = new int[size];
			this.mergeSort(ref data, ref temp, 0, size - 1);
			return true;
		}

		public void quickSort(ref int[] data, int left, int right)
		{
			int num = left;
			int num2 = right;
			int num3 = data[left];
			while (left < right)
			{
				while (data[right] >= num3 && left < right)
				{
					right--;
				}
				if (left != right)
				{
					data[left] = data[right];
					left++;
				}
				while (data[left] <= num3 && left < right)
				{
					left++;
				}
				if (left != right)
				{
					data[right] = data[left];
					right--;
				}
			}
			data[left] = num3;
			num3 = left;
			left = num;
			right = num2;
			if (left < num3)
			{
				this.quickSort(ref data, left, num3 - 1);
			}
			if (right > num3)
			{
				this.quickSort(ref data, num3 + 1, right);
			}
		}

		private bool QuickSort(ref int[] data, int size)
		{
			this.quickSort(ref data, 0, size - 1);
			return true;
		}

		private bool SelectionSort(ref int[] data, int size)
		{
			for (int i = 0; i < size - 1; i++)
			{
				int num = i;
				for (int j = i + 1; j < size; j++)
				{
					if (data[j] < data[num])
					{
						num = j;
					}
				}
				int num2 = data[i];
				data[i] = data[num];
				data[num] = num2;
			}
			return true;
		}

		private bool ShellSort(ref int[] data, int size)
		{
			for (int num = 3; num > 0; num = ((num / 2 == 0) ? ((num != 1) ? 1 : 0) : (num / 2)))
			{
				for (int i = 0; i < size; i++)
				{
					int num2 = i;
					int num3 = data[i];
					while (num2 >= num && data[num2 - num] > num3)
					{
						data[num2] = data[num2 - num];
						num2 -= num;
					}
					data[num2] = num3;
				}
			}
			return true;
		}
	}
}
