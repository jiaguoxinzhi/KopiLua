using System;

namespace Linyee
{
	public struct LinyeeTag
	{
		public LinyeeTag (object tag): this ()
		{
			this.Tag = tag;
		}

		public object Tag { get; set; }
	}
}

