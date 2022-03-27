using MathNet.Numerics.LinearAlgebra;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityMeshDecimation.Internal {
	public class Quadric {

		public Matrix<double> a;
		public Vector<double> b;
		public double c;

		public Quadric(Quadric src) {
			a = Matrix<double>.Build.Dense(src.a.RowCount, src.a.ColumnCount);
			src.a.CopyTo(a);
			b = Vector<double>.Build.Dense(src.b.Count);
			src.b.CopyTo(b);
			c = src.c;
		}

		public Quadric(int size) {
			a = Matrix<double>.Build.Dense(size, size);
			b = Vector<double>.Build.Dense(size);
			c = -1;
		}

		public int Size => b.Count;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IsValid() { return c >= 0; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void SetInvalid() { c = -1; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CopyTo(Quadric dst) {
			a.CopyTo(dst.a);
			b.CopyTo(dst.b);
			dst.c = c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Zero() {
			a.Clear();
			b.Clear();
			c = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddtoQ3(Quadric q3) {
			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 3; j++) {
					q3.a[i, j] += a[i, j];
				}
			}

			q3.b[0] += b[0];
			q3.b[1] += b[1];
			q3.b[2] += b[2];

			q3.c += c;
		}

		public void ByFace(Face face, Quadric q1, Quadric q2, Quadric q3, bool qualityQuadric, double borderWeight, VertexProperty property) {
			float q = face.GetQuality();
			if (q > 0) {
				this.ByFace(face, true, property);
				this.AddtoQ3(q1);
				this.AddtoQ3(q2);
				this.AddtoQ3(q3);
				this.ByFace(face, false, property);
				for (int i = 0; i < Face.VERTEX_COUNT; i++) {
					if (face.IsBorder(i) || qualityQuadric) {
						Quadric temp = new Quadric(this.Size);
						Vector3 newPos = (face.P0(i) + face.P1(i)) / 2 + face.faceNormal * Vector3.Distance(face.P0(i), face.P1(i));
						Vector<float> newAttr = (face.GetPropertyS(property, (i + 0) % 3) + face.GetPropertyS(property, (i + 1) % 3)) / 2;
						Vector3 oldPos = face.P2(i);
						Vector<float> oldAttr = face.GetPropertyS(property, (i + 2) % 3);

						face.V2(i).pos = newPos;
						face.SetPropertyS(property, (i + 2) % 3, newAttr);

						temp.ByFace(face, false, property);
						temp.Scale(face.IsBorder(i) ? borderWeight : 0.05);
						this.Add(temp);

						face.V2(i).pos = oldPos;
						face.SetPropertyS(property, (i + 2) % 3, oldAttr);
					}
				}
			}
			else {
				var attr0 = face.GetPropertyS(property, 0);
				var attr1 = face.GetPropertyS(property, 1);
				var attr2 = face.GetPropertyS(property, 2);

				var a = (attr0 - attr1).L2Norm();
				var b = (attr1 - attr2).L2Norm();
				var c = (attr2 - attr0).L2Norm();

				if (!(a + b == c || a + c == b || b + c == a)) {
					this.ByFace(face, false, property);
				}
				else {
					this.Zero();
				}
			}
		}

		public void ByFace(Face face, bool onlyGeo, VertexProperty property) {
			property |= VertexProperty.Position;
			var p = face.GetPropertyD(property, 0);
			var q = face.GetPropertyD(property, 1);
			var r = face.GetPropertyD(property, 2);

			if (onlyGeo) {
				for (int i = 3; i < this.Size; i++) {
					p[i] = 0;
					q[i] = 0;
					r[i] = 0;
				}
			}

			Vector<double> e1 = Vector<double>.Build.Dense(this.Size);
			Vector<double> e2 = Vector<double>.Build.Dense(this.Size);
			this.ComputeE1E2(p, q, r, e1, e2);
			this.ComputeQuadricFromE1E2(e1, e2, p);

			if (IsValid()) {
				return;
			}

			double minerror = double.MaxValue;
			int minerrorIndex = 0;
			Vector<double> tmp;
			for (int i = 0; i < 7; i++) {
				switch (i) {
					case 0:
						break;
					case 1:
					case 3:
					case 5:
						tmp = q;
						q = r;
						r = tmp;
						break;
					case 2:
					case 4:
						tmp = p;
						p = r;
						r = tmp;
						break;
					case 6: // every swap has loss of precision
						tmp = p;
						p = r;
						r = tmp;
						for (int j = 0; j <= minerrorIndex; j++) {
							switch (j) {
								case 0:
									break;
								case 1:
								case 3:
								case 5:
									tmp = q;
									q = r;
									r = tmp;
									break;
								case 2:
								case 4:
									tmp = p;
									p = r;
									r = tmp;
									break;
							}
						}
						minerrorIndex = -1;
						break;
				}

				this.ComputeE1E2(p, q, r, e1, e2);
				this.ComputeQuadricFromE1E2(e1, e2, p);

				if (IsValid()) {
					return;
				}
				else if (minerrorIndex == -1) {
					break;
				}
				else if (-c < minerror) {
					minerror = -c;
					minerrorIndex = i;
				}
			}
			c = 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ComputeE1E2(Vector<double> p, Vector<double> q, Vector<double> r, Vector<double> e1, Vector<double> e2) {
			(q - p).Normalize(2).CopyTo(e1);

			var diffe = r - p;
			(diffe - (e1.OuterProduct(diffe) * e1)).Normalize(2).CopyTo(e2);
		}

		private void ComputeQuadricFromE1E2(Vector<double> e1, Vector<double> e2, Vector<double> p) {
			a = Matrix<double>.Build.DenseIdentity(this.Size);

			var t1 = e1.OuterProduct(e1);
			a -= t1;
			var t2 = e2.OuterProduct(e2);
			a -= t2;

			var pe1 = p.DotProduct(e1);
			var pe2 = p.DotProduct(e2);

			for (int i = 0; i < b.Count; i++) {
				b[i] = pe1 * e1[i] + pe2 * e2[i];
			}
			b -= p;

			c = p.DotProduct(p) - pe1 * pe1 - pe2 * pe2;
		}

		public bool MinimumWithGeoContraints(Vector<double> x, Vector<double> geo) {
			Matrix<double> m = a.SubMatrix(3, Size - 3, 3, Size - 3);
			Vector<double> r = Vector<double>.Build.Dense(Size - 3);

			for (int i = 0; i < r.Count; i++) {
				r[i] = b[i + 3];
				for (int j = 0; j < 3; j++) {
					r[i] += a[i + 3, j] * geo[j];
				}
			}

			x[0] = geo[0];
			x[1] = geo[1];
			x[2] = geo[2];

			if (m.Determinant() != 0) {
				var result = -r * m.Inverse();
				for (int i = 0; i < result.Count; i++) {
					x[i + 3] = result[i];
				}
				return true;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Minimum(Vector<double> x) {
			if (a.Determinant() != 0) {
				(-b * a.Inverse()).CopyTo(x);
				return true;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Add(Quadric q) {
			for(int i = 0; i < a.RowCount; i++) {
				for(int j = 0; j < a.ColumnCount; j++) {
					a[i, j] += q.a[i, j];
				}
			}
			for (int i = 0; i < b.Count; i++) {
				b[i] += q.b[i];
			}
			c += q.c;
		}

		public void Sum3(Quadric q3, Vector<float> props) {
			if (q3.Size != 3) {
				return;
			}

			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 3; j++) {
					a[i, j] += q3.a[i, j];
				}
			}
			for (int i = 3; i < this.Size; i++) {
				a[i, i] += 1;
			}

			b[0] += q3.b[0];
			b[1] += q3.b[1];
			b[2] += q3.b[2];
			for (int i = 3; i < this.Size; i++) {
				b[i] -= props[i - 3];
			}

			c += q3.c + props.DotProduct(props);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Scale(double val) {
			for (int i = 0; i < a.RowCount; i++) {
				for (int j = 0; j < a.ColumnCount; j++) {
					a[i, j] *= val;
				}
			}
			for (int i = 0; i < b.Count; i++) {
				b[i] *= val;
			}
			c *= val;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double Apply(Vector<double> v) {
			return (a * v).DotProduct(v) + 2 * b.DotProduct(v) + c;
		}
	}
}
