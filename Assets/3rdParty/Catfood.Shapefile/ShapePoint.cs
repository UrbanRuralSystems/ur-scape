/* ------------------------------------------------------------------------
 * (c)copyright 2009-2019 Robert Ellison and contributors - https://github.com/abfo/shapefile
 * Provided under the ms-PL license, see LICENSE.txt
 * ------------------------------------------------------------------------ */

using System;
using System.Collections.Generic;
using PointD = Coordinate;

namespace Catfood.Shapefile
{
    /// <summary>
    /// A Shapefile Point Shape
    /// </summary>
    public class ShapePoint : Shape
    {
        private PointD _point;

        /// <summary>
        /// A Shapefile Point Shape
        /// </summary>
        /// <param name="shapeData">The shape record as a byte array</param>
        /// <exception cref="ArgumentNullException">Thrown if shapeData is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if an error occurs parsing shapeData</exception>
        protected internal ShapePoint(byte[] shapeData)
            : base(ShapeType.Point)
        {
            // metadata is validated by the base class
            if (shapeData == null)
            {
                throw new ArgumentNullException("shapeData");
            }

            // Note, shapeData includes an 8 byte header so positions below are +8
            // Position     Field       Value   Type        Number  Order
            // Byte 0       Shape Type  1       Integer     1       Little
            // Byte 4       X           X       Double      1       Little
            // Byte 12      Y           Y       Double      1       Little

            // validation - shapedata should be 8 + 4 + 8 + 8 = 28 bytes long
            if (shapeData.Length != 28)
            {
                throw new InvalidOperationException("Invalid shape data");
            }

            _point = new PointD(EndianBitConverter.ToDouble(shapeData, 12, ProvidedOrder.Little),
                EndianBitConverter.ToDouble(shapeData, 20, ProvidedOrder.Little));
        }

        /// <summary>
        /// Gets the point
        /// </summary>
        public PointD Point
        {
            get { return _point; }
        }
    }
}
