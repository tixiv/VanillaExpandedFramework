﻿using Verse;

namespace VEF.Graphics
{
  
    public class ThingWithFloorGraphic : ThingWithComps
    {
		public Graphic graphicIntOverride;
		public Graphic FloorGraphic(FloorGraphicExtension floorGraphicExtension)
		{
			if (graphicIntOverride == null)
			{
				if (floorGraphicExtension.graphicData == null)
				{
					return BaseContent.BadGraphic;
				}
				graphicIntOverride = floorGraphicExtension.graphicData.GraphicColoredFor(this);
			}
			return graphicIntOverride;
		}
		public override Graphic Graphic
		{
			get
			{
				if (this.ParentHolder is Map)
                {
					var extension = this.def.GetModExtension<FloorGraphicExtension>();
					if (extension != null)
					{
						return FloorGraphic(extension);
					}
				}
				return base.Graphic;
			}
		}
	}
}
