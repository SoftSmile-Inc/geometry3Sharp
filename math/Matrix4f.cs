using System;

namespace g3
{
    public readonly struct Matrix4f
    {
        public static readonly Matrix4f Identity = Diagonal(Vector4f.One);

        public static readonly Matrix4f Zero = new Matrix4f(
            Vector4f.Zero,
            Vector4f.Zero,
            Vector4f.Zero,
            Vector4f.Zero,
            rowVectors: true);

        public readonly Vector4f Row0;
        public readonly Vector4f Row1;
        public readonly Vector4f Row2;
        public readonly Vector4f Row3;

        public Matrix4f(float[,] rowMajorArray)
        {
            if (rowMajorArray.GetLength(0) != 4 || rowMajorArray.GetLength(1) != 4)
            {
                throw new ArgumentException($"Array must have shape 4x4 but was {rowMajorArray.GetLength(0)}x{rowMajorArray.GetLength(1)}", nameof(rowMajorArray));
            }

            Row0 = new Vector4f(rowMajorArray[0, 0], rowMajorArray[0, 1], rowMajorArray[0, 2], rowMajorArray[0, 3]);
            Row1 = new Vector4f(rowMajorArray[1, 0], rowMajorArray[1, 1], rowMajorArray[1, 2], rowMajorArray[1, 3]);
            Row2 = new Vector4f(rowMajorArray[2, 0], rowMajorArray[2, 1], rowMajorArray[2, 2], rowMajorArray[2, 3]);
            Row3 = new Vector4f(rowMajorArray[3, 0], rowMajorArray[3, 1], rowMajorArray[3, 2], rowMajorArray[3, 3]);
        }

        public Matrix4f(float[] rowMajorArray)
        {
            if (rowMajorArray.Length != 16)
            {
                throw new ArgumentException($"Array must contain 16 elements but was of length {rowMajorArray.Length}", nameof(rowMajorArray));
            }

            Row0 = new Vector4f(rowMajorArray[0], rowMajorArray[1], rowMajorArray[2], rowMajorArray[3]);
            Row1 = new Vector4f(rowMajorArray[4], rowMajorArray[5], rowMajorArray[6], rowMajorArray[7]);
            Row2 = new Vector4f(rowMajorArray[8], rowMajorArray[9], rowMajorArray[10], rowMajorArray[11]);
            Row3 = new Vector4f(rowMajorArray[12], rowMajorArray[13], rowMajorArray[14], rowMajorArray[15]);
        }

        public Matrix4f(Vector4f v0, Vector4f v1, Vector4f v2, Vector4f v3, bool rowVectors)
        {
            if (rowVectors)
            {
                Row0 = v0;
                Row1 = v1;
                Row2 = v2;
                Row3 = v3;
            }
            else
            {
                Row0 = new Vector4f(v0.x, v1.x, v2.x, v3.x);
                Row1 = new Vector4f(v0.y, v1.y, v2.y, v3.y);
                Row2 = new Vector4f(v0.z, v1.z, v2.z, v3.z);
                Row3 = new Vector4f(v0.w, v1.w, v2.w, v3.w);
            }
        }

        public Matrix4f Transpose() => new Matrix4f(Row0, Row1, Row2, Row3, rowVectors: false);

        public static Matrix4f Diagonal(Vector4f mainDiagonal)
        {
            return new Matrix4f(
                new Vector4f(mainDiagonal.x, 0, 0, 0),
                new Vector4f(0, mainDiagonal.y, 0, 0),
                new Vector4f(0, 0, mainDiagonal.z, 0),
                new Vector4f(0, 0, 0, mainDiagonal.w),
                rowVectors: true);
        }

        public static Matrix4f Translation(Vector3f translation)
        {
            return new Matrix4f(
                new[,]
                {
                    { 1, 0, 0, translation.x },
                    { 0, 1, 0, translation.y },
                    { 0, 0, 1, translation.z },
                    { 0, 0, 0, 1 },
                });
        }

        public static Matrix4f Rotation(Quaternionf rotation)
        {
            Matrix3f m = rotation.ToRotationMatrix();
            return new Matrix4f(
                m.Row0.ExpandDimension(3, 0),
                m.Row1.ExpandDimension(3, 0),
                m.Row2.ExpandDimension(3, 0),
                new Vector4f(0, 0, 0, 1),
                rowVectors: true
            );
        }

        public static Matrix4f Scale(Vector3f scale) => Diagonal(scale.ExpandDimension(3, 1));

        /// <summary>
        /// TRS is a common abbreviation for combined transformation matrix.
        /// Returns single matrix combining translation, rotation, scaling.
        /// As with all matrices, this means that when you apply it to actual vectors the order is TRS(x) = {Translate * [Rotate * (Scale * x)]}
        /// </summary>
        /// <param name="frame">frame</param>
        /// <param name="scale">scale</param>
        /// <returns>Translation * Rotation * Scaling matrix</returns>
        public static Matrix4f TRS(Frame3f frame, Vector3f scale) => Translation(frame.Origin) * Rotation(frame.Rotation) * Scale(scale);

        public static Matrix4f operator *(Matrix4f a, Matrix4f b)
        {
            Matrix4f bT = b.Transpose();
            return new Matrix4f(
                new Vector4f(a.Row0.Dot(bT.Row0), a.Row0.Dot(bT.Row1), a.Row0.Dot(bT.Row2), a.Row0.Dot(bT.Row3)),
                new Vector4f(a.Row1.Dot(bT.Row0), a.Row1.Dot(bT.Row1), a.Row1.Dot(bT.Row2), a.Row1.Dot(bT.Row3)),
                new Vector4f(a.Row2.Dot(bT.Row0), a.Row2.Dot(bT.Row1), a.Row2.Dot(bT.Row2), a.Row2.Dot(bT.Row3)),
                new Vector4f(a.Row3.Dot(bT.Row0), a.Row3.Dot(bT.Row1), a.Row3.Dot(bT.Row2), a.Row3.Dot(bT.Row3)),
                rowVectors: true
            );
        }

        public static Vector4f operator *(Matrix4f a, Vector3f vector)
        {
            Vector4f expanded = vector.ExpandDimension(3, 1);
            return new Vector4f(
                a.Row0.Dot(expanded),
                a.Row1.Dot(expanded),
                a.Row2.Dot(expanded),
                a.Row3.Dot(expanded)
            );
        }

        public float[] ToBuffer() =>
            new float[]
            {
                Row0.x, Row0.y, Row0.z, Row0.w,
                Row1.x, Row1.y, Row1.z, Row1.w,
                Row2.x, Row2.y, Row2.z, Row2.w,
                Row3.x, Row3.y, Row3.z, Row3.w,
            };

        public override string ToString() => $"[{Row0}] [{Row1}] [{Row2}] [{Row3}]";

        public string ToString(string fmt) => $"[{Row0.ToString(fmt)}] [{Row1.ToString(fmt)}] [{Row2.ToString(fmt)}] [{Row3.ToString(fmt)}]";
    }
}