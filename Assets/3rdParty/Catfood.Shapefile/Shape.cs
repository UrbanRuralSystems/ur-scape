/* ------------------------------------------------------------------------
 * (c)copyright 2009-2019 Robert Ellison and contributors - https://github.com/abfo/shapefile
 * Provided under the ms-PL license, see LICENSE.txt
 * ------------------------------------------------------------------------ */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;  //! TEMP
using PointD = Coordinate;
using RectangleD = AreaBounds;

namespace Catfood.Shapefile
{
    /// <summary>
    /// Base Shapefile shape - contains only the shape type and metadata plus helper methods.
    /// An instance of Shape is the Null ShapeType. If the Type field is not ShapeType.Null then
    /// cast to the more specific shape (i.e. ShapePolygon).
    /// </summary>
    public class Shape
    {
        public ShapeType Type { internal set; get; }

        /// <summary>
        /// Base Shapefile shape - contains only the shape type and metadata plus helper methods.
        /// An instance of Shape is the Null ShapeType. If the Type field is not ShapeType.Null then
        /// cast to the more specific shape (i.e. ShapePolygon).
        /// </summary>
        /// <param name="shapeType">The ShapeType of the shape</param>
        /// <param name="metadata">Metadata about the shape (optional)</param>
        /// <param name="dataRecord">IDataRecord associated with the shape</param>
        protected internal Shape(ShapeType shapeType)
        {
            Type = shapeType;
            Debug.Log($"Type: {Type}");
        }

        /// <summary>
        /// Extracts a double precision rectangle (RectangleD) from a byte array - assumes that
        /// the called has validated that there is enough space in the byte array for four
        /// doubles (32 bytes)
        /// </summary>
        /// <param name="value">byte array</param>
        /// <param name="startIndex">start index in the array</param>
        /// <param name="order">byte order of the doubles to be extracted</param>
        /// <returns>The RectangleD</returns>
        protected RectangleD ParseBoundingBox(byte[] value, int startIndex, ProvidedOrder order)
        {
            return new RectangleD(EndianBitConverter.ToDouble(value, startIndex, order),
                EndianBitConverter.ToDouble(value, startIndex + 16, order),
                EndianBitConverter.ToDouble(value, startIndex + 8, order),
                EndianBitConverter.ToDouble(value, startIndex + 24, order));
        }

        /// <summary>
        /// The PolyLine and Polygon shapes share the same structure, this method parses the bounding box
        /// and list of parts for both
        /// </summary>
        /// <param name="shapeData">The shape record as a byte array</param>
        /// <param name="boundingBox">Returns the bounding box</param>
        /// <param name="parts">Returns the list of parts</param>
        protected void ParsePolyLineOrPolygon(byte[] shapeData, out RectangleD boundingBox, out List<PointD[]> parts)
        {
            boundingBox = new RectangleD();
            parts = null;

            // metadata is validated by the base class
            if (shapeData == null)
            {
                throw new ArgumentNullException("shapeData");
            }

            // Note, shapeData includes an 8 byte header so positions below are +8
            // Position     Field       Value       Type        Number      Order
            // Byte 0       Shape Type  3 or 5      Integer     1           Little
            // Byte 4       Box         Box         Double      4           Little
            // Byte 36      NumParts    NumParts    Integer     1           Little
            // Byte 40      NumPoints   NumPoints   Integer     1           Little
            // Byte 44      Parts       Parts       Integer     NumParts    Little
            // Byte X       Points      Points      Point       NumPoints   Little
            //
            // Note: X = 44 + 4 * NumParts

            // validation step 1 - must have at least 8 + 4 + (4*8) + 4 + 4 bytes = 52
            if (shapeData.Length < 44)
            {
                throw new InvalidOperationException("Invalid shape data");
            }

            // extract bounding box, number of parts and number of points
            boundingBox = ParseBoundingBox(shapeData, 12, ProvidedOrder.Little);
            Debug.Log($"Extents: {boundingBox.west}, {boundingBox.east}, {boundingBox.north}, {boundingBox.south}");
            int numParts = EndianBitConverter.ToInt32(shapeData, 44, ProvidedOrder.Little);
            Debug.Log($"numParts: {numParts}");
            int numPoints = EndianBitConverter.ToInt32(shapeData, 48, ProvidedOrder.Little);
            Debug.Log($"numPoints: {numPoints}");

            // validation step 2 - we're expecting 4 * numParts + 16 * numPoints + 52 bytes total
            if (shapeData.Length != 52 + (4 * numParts) + (16 * numPoints))
            {
                throw new InvalidOperationException("Invalid shape data");
            }

            // now extract the parts
            int partsOffset = 52 + (4 * numParts);
            parts = new List<PointD[]>(numParts);
            for (int part = 0; part < numParts; part++)
            {
                // this is the index of the start of the part in the points array
                int startPart = (EndianBitConverter.ToInt32(shapeData, 52 + (4 * part), ProvidedOrder.Little) * 16) + partsOffset;

                int numBytes;
                if (part == numParts - 1)
                {
                    // it's the last part so we go to the end of the point array
                    numBytes = shapeData.Length - startPart;
                }
                else
                {
                    // we need to get the next part
                    int nextPart = (EndianBitConverter.ToInt32(shapeData, 52 + (4 * (part + 1)), ProvidedOrder.Little) * 16) + partsOffset;
                    numBytes = nextPart - startPart;
                }

                // the number of 16-byte points to read for this segment
                int numPointsInPart = numBytes / 16;

                PointD[] points = new PointD[numPointsInPart];
                for (int point = 0; point < numPointsInPart; point++)
                {
                    points[point] = new PointD(EndianBitConverter.ToDouble(shapeData, startPart + (16 * point), ProvidedOrder.Little),
                        EndianBitConverter.ToDouble(shapeData, startPart + 8 + (16 * point), ProvidedOrder.Little));
                }

                parts.Add(points);
            }
        }
    }
}
