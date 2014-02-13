using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Engine {
	public static class MathHelper {
		public static float LerpF(float start, float finish, float amount) {
			return start + amount * (finish - start);
		}

		public static DrawingRectangleF ScaleRectangle(ref DrawingRectangleF rect, ref float amount) {
			return new DrawingRectangleF(
				rect.X + rect.Width * (1 - amount) / 2,
				rect.Y + rect.Height * (1 - amount) / 2,
				rect.Width * amount,
				rect.Height * amount);
		}

		public static Matrix3x2 Invert(Matrix3x2 matrix) {
			Matrix3x2 result;
			Invert(ref matrix, out result);
			return result;
		}

		public static void Invert(ref Matrix3x2 matrix, out Matrix3x2 result) {
			float determinant = matrix.Determinant();

			if (MathUtil.WithinEpsilon(determinant, 0.0f)) {
				result = Matrix3x2.Identity;
				return;
			}

			float invdet = 1.0f / determinant;
			float _offsetX = matrix.M31;
			float _offsetY = matrix.M32;

			result = new Matrix3x2(
				matrix.M22 * invdet,
				-matrix.M12 * invdet,
				-matrix.M21 * invdet,
				matrix.M11 * invdet,
				(matrix.M21 * _offsetY - _offsetX * matrix.M22) * invdet,
				(_offsetX * matrix.M12 - matrix.M11 * _offsetY) * invdet);
		}
	}
}
