namespace VisionInspection.Inspection
{
	internal class NeighborChecker
	{
		private int idx;

		private int upIdx;

		private int downIdx;

		private int leftIdx;

		private int rightIdx;

		public int Index
		{
			get
			{
				return this.idx;
			}
			set
			{
				this.idx = value;
			}
		}

		public int UpIdx
		{
			get
			{
				return this.upIdx;
			}
			set
			{
				this.upIdx = value;
			}
		}

		public int DownIdx
		{
			get
			{
				return this.downIdx;
			}
			set
			{
				this.downIdx = value;
			}
		}

		public int LeftIdx
		{
			get
			{
				return this.leftIdx;
			}
			set
			{
				this.leftIdx = value;
			}
		}

		public int RightIdx
		{
			get
			{
				return this.rightIdx;
			}
			set
			{
				this.rightIdx = value;
			}
		}

		public NeighborChecker()
		{
			this.idx = -1;
			this.upIdx = -1;
			this.downIdx = -1;
			this.leftIdx = -1;
			this.rightIdx = -1;
		}

		~NeighborChecker()
		{
		}
	}
}
