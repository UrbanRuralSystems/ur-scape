/* ------------------------------------------------------------------------
 * (c)copyright 2009-2019 Robert Ellison and contributors - https://github.com/abfo/shapefile
 * Provided under the ms-PL license, see LICENSE.txt
 * ------------------------------------------------------------------------ */

using System;
using System.Collections.Generic;
using PointD = Coordinate;
using RectangleD = AreaBounds;

namespace Catfood.Shapefile
{
    /// <summary>
    /// A Shapefile Polygon Shape
    /// </summary>
    public class ShapePolygon : Shape
    {
        private RectangleD _boundingBox;
        private List<PointD[]> _parts;

        /// <summary>
        /// A Shapefile Polygon Shape
        /// </summary>
        /// <param name="shapeData">The shape record as a byte array</param>
        /// <exception cref="ArgumentNullException">Thrown if shapeData is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if an error occurs parsing shapeData</exception>
        protected internal ShapePolygon(byte[] shapeData)
            : base(ShapeType.Polygon)
        {
            ParsePolyLineOrPolygon(shapeData, out _boundingBox, out _parts);
        }

        /// <summary>
        /// Gets the bounding box
        /// </summary>
        public RectangleD BoundingBox
        {
            get { return _boundingBox; }
        }
        
        /// <summary>
        /// Gets a list of parts (segments) for the PolyLine. Each part
        /// is an array of double precision points
        /// </summary>
        public List<PointD[]> Parts
        {
            get { return _parts; }
        }
    }
}
