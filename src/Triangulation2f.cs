using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using element = System.Single;
using vector = Jk.Vector2f;
using range = Jk.Range2f;

namespace Jk {
	/// <summary>
	/// 三角形分割クラス
	/// </summary>
	/// <remarks>Carveライブラリの "triangulator.cpp" を基に作成</remarks>
	public class Triangulation2f {
		#region 内部クラス
		#region 汎用
		public delegate vector Project<T>(ref T vertex);

		public class Exception : System.Exception {
			public Exception(string message)
				: base(message) {
			}
		}

		struct Iterator<T> {
			public FList<T> List;
			public int Index;

			public ref T Value {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get {
					return ref List[Index];
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Iterator(FList<T> list, int index) {
				this.List = list;
				this.Index = index;
			}
		}

		struct LineSegment2 {
			public vector P1;
			public vector P2;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public LineSegment2(vector _v1, vector _v2) {
				P1 = _v1;
				P2 = _v2;
			}
		}

		class EarQueue {
			PriorityQueue<VertexInfo> Queue = new PriorityQueue<VertexInfo>(new VertexInfoComparer());

			public EarQueue() {
			}

			public int Count {
				get {
					return Queue.Count;
				}
			}

			public void Push(VertexInfo v) {
				Queue.Push(v);
			}

			public VertexInfo Pop() {
				return Queue.Pop();
			}

			public void Remove(VertexInfo v) {
				var score = v.Score;
				if (v != Queue.List[0]) {
					v.Score = Queue.List[0].Score + 1;
					Queue.UpdateHeap();
				}
				Queue.Pop();
				v.Score = score;
			}

			public void ChangeScore(VertexInfo v, element score) {
				if (v.Score != score) {
					v.Score = score;
					Queue.UpdateHeap();
				}
			}

			// 39% of execution time
			public void UpdateVertex(VertexInfo v) {
				var spre = v.Score;
				var qpre = v.IsCandidate();
				v.Recompute();
				var qpost = v.IsCandidate();
				var spost = v.Score;

				v.Score = spre;

				if (qpre) {
					if (qpost) {
						if (v.Score != spre) {
							ChangeScore(v, spost);
						}
					} else {
						Remove(v);
					}
				} else {
					if (qpost) {
						Push(v);
					}
				}
			}
		}

		/// <summary>
		/// Maintains a linked list of untriangulated vertices during a triangulation operation.
		/// </summary>
		public class VertexInfo {
			public VertexInfo Prev;
			public VertexInfo Next;
			public vector P;
			public int Idx;
			public element Score;
			public bool Convex;
			public bool Failed;

			public VertexInfo(vector p, int idx) {
				this.P = p;
				this.Idx = idx;
			}

			public static element TriScore(VertexInfo p, VertexInfo v, VertexInfo n) {
				// range: 0 - 1
				element a, b, c;

				bool convex = IsLeft(p, v, n);
				if (!convex) return -1e-5f;

				a = (n.P - v.P).Length;
				b = (p.P - n.P).Length;
				c = (v.P - p.P).Length;

				if (a < 1e-10 || b < 1e-10 || c < 1e-10)
					return 0;

				return Math.Max(Math.Min((a + b) / c, Math.Min((a + c) / b, (b + c) / a)) - 1, 0);
			}

			public element CalcScore() {
				var this_tri = TriScore(Prev, this, Next);
				var next_tri = TriScore(Prev, Next, Next.Next);
				var prev_tri = TriScore(Prev.Prev, Prev, Next);

				return this_tri + Math.Max(next_tri, prev_tri) * .2f;
			}

			public void Recompute() {
				Score = CalcScore();
				Convex = IsLeft(Prev, this, Next);
				Failed = false;
			}

			public bool IsCandidate() {
				return Convex && !Failed;
			}

			public void Remove() {
				Next.Prev = Prev;
				Prev.Next = Next;
			}

			public bool IsClipable() {
				for (VertexInfo v_test = Next.Next; v_test != Prev; v_test = v_test.Next) {
					if (v_test.Convex) {
						continue;
					}

					if (v_test.P == Prev.P ||
						v_test.P == Next.P) {
						continue;
					}

					if (v_test.P == P) {
						if (v_test.Next.P == Prev.P &&
							v_test.Prev.P == Next.P) {
							return false;
						}
						if (v_test.Next.P == Prev.P ||
							v_test.Prev.P == Next.P) {
							continue;
						}
					}

					if (PointInTriangle(Prev, this, Next, v_test)) {
						return false;
					}
				}
				return true;
			}

			public VertexInfo Clone() {
				return this.MemberwiseClone() as VertexInfo;
			}
		}
		#endregion

		#region 比較オブジェクト
		/// <summary>
		/// ベクトルを指定軸で比較する
		/// </summary>
		class VectorAxisComparer : IComparer<vector> {
			int _Axis;

			public VectorAxisComparer(int axis) {
				_Axis = axis;
			}

			public int Compare(vector a, vector b) {
				return AxisOrdering(a, b, _Axis);
			}
		}

		/// <summary>
		/// ベクトルイテレータを指定軸で比較する
		/// </summary>
		class VectorIteratorAxisComparer : IComparer<Iterator<vector>> {
			int _Axis;

			public VectorIteratorAxisComparer(int axis) {
				_Axis = axis;
			}

			public int Compare(Iterator<vector> a, Iterator<vector> b) {
				return AxisOrdering(a.Value, b.Value, _Axis);
			}
		}

		/// <summary>
		/// 指定された座標と座標列間の距離で比較する、距離が小さい方が大きいと判定される
		/// </summary>
		class DistanceComparer : IComparer<int> {
			FList<vector> _Loop;
			vector _P;
			int _Axis;

			/// <summary>
			/// 座標列、注目座標、距離が同じ場合に<see cref="AxisOrdering"/>で比較に使用する軸を指定して初期化する
			/// </summary>
			/// <param name="loop">頂点列</param>
			/// <param name="vert">注目座標</param>
			/// <param name="axis">軸インデックス</param>
			public DistanceComparer(FList<vector> loop, vector vert, int axis) {
				_Loop = loop;
				_P = vert;
				_Axis = axis;
			}

			public int Compare(int a, int b) {
				var pa = _Loop[a];
				var pb = _Loop[b];
				var da = (_P - pa).LengthSquare;
				var db = (_P - pb).LengthSquare;
				if (da > db) return -1;
				if (da < db) return 1;
				return AxisOrdering(pa, pb, _Axis);
			}
		}

		/// <summary>
		/// <see cref="VertexInfo.Score"/>での比較
		/// </summary>
		class VertexInfoComparer : IComparer<VertexInfo> {
			public int Compare(VertexInfo a, VertexInfo b) {
				return Math.Sign(a.Score - b.Score);
			}
		}

		/// <summary>
		/// コンストラクタで指定した距離と<see cref="VertexInfo.P"/>間の距離で比較する、距離が小さい方が大きいと判定される
		/// </summary>
		class VertexInfoDistanceComparer : IComparer<VertexInfo> {
			vector _P;

			public VertexInfoDistanceComparer(vector p) {
				_P = p;
			}

			public int Compare(VertexInfo a, VertexInfo b) {
				return Math.Sign((_P - b.P).LengthSquare - (_P - a.P).LengthSquare);
			}
		}
		#endregion
		#endregion

		#region 公開メソッド
		/// <summary>
		/// ループ配列を１つのポリゴン頂点座標配列へ結合する
		/// </summary>
		/// <typeparam name="T">頂点型</typeparam>
		/// <param name="project"><see cref="T"/>から座標ベクトルを取得するデリゲート</param>
		/// <param name="loops">ループ配列、添え字[0:外側、1...:穴]</param>
		/// <param name="resultPositions">結果の頂点座標が返る</param>
		public static void LoopsToPositions<T>(Project<T> project, FList<FList<T>> loops, FList<vector> resultPositions) {
			if (loops.Count == 0) {
				resultPositions.Clear();
				return;
			}
			if (loops.Count == 1) {
				var loop = loops[0];
				var loopCore = loop.Core;

				resultPositions.Clear();
				if (resultPositions.Capacity < loopCore.Count)
					resultPositions.Capacity = loopCore.Count;

				for (int i = 0; i < loopCore.Count; i++)
					resultPositions.Add(project(ref loopCore.Items[i]));
				return;
			}
			LoopsToPositions(project, loops[0], new FList<FList<T>>(loops, 1), resultPositions);
		}

		/// <summary>
		/// ループ配列を１つのポリゴン頂点座標配列へ結合する
		/// </summary>
		/// <typeparam name="T">頂点型</typeparam>
		/// <param name="project"><see cref="T"/>から座標ベクトルを取得するデリゲート</param>
		/// <param name="loops">ループ配列、添え字[0:外側、1...:穴]</param>
		/// <returns>外と穴のループから座標を抜き出し一列にしたリスト</returns>
		public static FList<vector> LoopsToPositions<T>(Project<T> project, FList<FList<T>> loops) {
			var resultPositions = new FList<vector>();
			LoopsToPositions(project, loops, resultPositions);
			return resultPositions;
		}


		/// <summary>
		/// 外枠頂点ループと穴ループ配列を１つのポリゴン頂点座標配列へ結合する
		/// </summary>
		/// <typeparam name="T">頂点型</typeparam>
		/// <param name="project"><see cref="T"/>から座標ベクトルを取得するデリゲート</param>
		/// <param name="vertexLoop">外枠頂点ループ</param>
		/// <param name="holeVertexLoops">穴頂点ループ配列</param>
		/// <param name="resultPositions">結果の頂点座標が返る</param>
		public static void LoopsToPositions<T>(Project<T> project, FList<T> vertexLoop, FList<FList<T>> holeVertexLoops, FList<vector> resultPositions) {
			var N = vertexLoop.Count;
			var holeVertexLoopsCore = holeVertexLoops.Core;

			// 全ての穴ループを結合した際に必要な要素数を計算
			for (int i = holeVertexLoopsCore.Count - 1; i != -1; i--) {
				N += 2 + holeVertexLoopsCore.Items[i].Count;
			}

			// 結果の座標配列の容量確保
			resultPositions.Clear();
			if (resultPositions.Capacity < N)
				resultPositions.Capacity = N;

			// まず外枠ループの頂点座標を入れておく
			var vertexLoopCore = vertexLoop.Core;
			for (int i = 0; i < vertexLoopCore.Count; i++) {
				resultPositions.Add(project(ref vertexLoopCore.Items[i]));
			}

			// 穴ループの頂点からベクトルを取得する
			var holeLoops = new FList<FList<vector>>(holeVertexLoopsCore.Count);
			for (int i = 0; i < holeVertexLoopsCore.Count; i++) {
				var holeVertexLoopCore = holeVertexLoopsCore.Items[i].Core;
				var holeLoop = new FList<vector>(holeVertexLoopCore.Count);
				for (int j = 0; j < holeVertexLoopCore.Count; j++) {
					holeLoop.Add(project(ref holeVertexLoopCore.Items[j]));
				}
				holeLoops.Add(holeLoop);
			}

			// 全穴ループを包含する境界ボリュームを探す
			var maxmin = range.InvalidValue;
			for (int i = holeLoops.Count - 1; i != -1; i--) {
				var holeLoop = holeLoops[i];
				for (int j = holeLoop.Count - 1; j != -1; j--) {
					maxmin.MergeSelf(holeLoop[j]);
				}
			}

			// 境界ボリュームの最も広がりが大きい軸を選ぶ
			int axis = maxmin.Size.ArgMax();

			// 各穴ループ内で選択軸の値が最も小さいベクトルを指すイテレータを列挙する
			var holeLoopMinIndices = new FList<Iterator<vector>>(holeLoops.Count);
			for (int i = holeLoops.Count - 1; i != -1; i--) {
				var holeLoop = holeLoops[i];
				var best_i = MinElementIndex(holeLoop, new VectorAxisComparer(axis));
				holeLoopMinIndices.Add(new Iterator<vector>(holeLoop, best_i));
			}

			// イテレータが指すベクトルの選択軸値で昇順にソートする
			holeLoopMinIndices.Sort(new VectorIteratorAxisComparer(axis));

			// 各穴ループと接続する resultPositionsCount 内のインデックスを探していく
			var indices = new FList<int>(N);
			for (int i = 0; i < holeLoopMinIndices.Count; ++i) {
				var resultPositionsCount = resultPositions.Count;

				// 穴ループ内の外ループと接続する点取得
				var holeConnectPosition = holeLoopMinIndices[i].Value;
				var holeAxisMinValue = holeConnectPosition[axis];

				// 処理済みの点の中から穴と接続するのにベストなものを探すためリストに登録する。
				// 接続する際に自ループと交差しないものを選ぶため holeAxisMinValue より小さい座標の頂点を登録する。
				// さらに holeLoopMinIndices は穴ループの指定軸最小値で昇順にソートされているため、他の穴ループとは交差しないことが保証されている。
				for (int j = 0; j < resultPositionsCount; ++j) {
					if (resultPositions[j][axis] <= holeAxisMinValue) {
						indices.Add(j);
					}
				}

				// holeConnectPosition との距離で降順にソートし最後尾が最も近いものになる
				indices.Sort(new DistanceComparer(resultPositions, holeConnectPosition, axis));

				// 距離が近い順から接続した際に他と交差しない点を探す
				var attachmentPoint = resultPositions.Count;
				while (indices.Count != 0) {
					var last = indices.Count - 1;
					var curr = indices[last];
					indices.RemoveAt(last);

					// 接続可能かテストする
					if (!TestCandidateAttachment(resultPositions, curr, holeConnectPosition)) {
						continue;
					}

					attachmentPoint = curr;
					break;
				}

				if (attachmentPoint == resultPositions.Count) {
					throw new Exception("didn't manage to link up hole!");
				}

				PatchHoleIntoPolygon(resultPositions, attachmentPoint, holeLoopMinIndices[i]);
			}
		}

		/// <summary>
		/// 外枠頂点ループと穴ループ配列を１つのポリゴン頂点座標配列へ結合する
		/// </summary>
		/// <typeparam name="T">頂点型</typeparam>
		/// <param name="project"><see cref="T"/>から座標ベクトルを取得するデリゲート</param>
		/// <param name="vertexLoop">外枠頂点ループ</param>
		/// <param name="holeVertexLoops">穴頂点ループ配列</param>
		/// <returns>外と穴のループから座標を抜き出し一列にしたリスト</returns>
		public static FList<vector> LoopsToPositions<T>(Project<T> project, FList<T> vertexLoop, FList<FList<T>> holeVertexLoops) {
			var resultPositions = new FList<vector>();
			LoopsToPositions(project, vertexLoop, holeVertexLoops, resultPositions);
			return resultPositions;
		}

		/// <summary>
		/// 指定された頂点座標配列を三角形分割する
		/// </summary>
		/// <param name="positions">頂点座標配列</param>
		/// <param name="result">ポリゴンインデックスが返る</param>
		public static void Triangulate(FList<vector> positions, FList<Vector3i> result) {
			var N = positions.Count;

			result.Clear();
			if (N < 3) {
				return;
			}

			result.Capacity = positions.Count - 2;

			if (N == 3) {
				result.Add(new Vector3i(0, 1, 2));
				return;
			}

			var vinfo = new VertexInfo[N];

			vinfo[0] = new VertexInfo(positions[0], 0);
			for (int i = 1; i < N - 1; ++i) {
				vinfo[i] = new VertexInfo(positions[i], i);
				vinfo[i].Prev = vinfo[i - 1];
				vinfo[i - 1].Next = vinfo[i];
			}
			vinfo[N - 1] = new VertexInfo(positions[N - 1], N - 1);
			vinfo[N - 1].Prev = vinfo[N - 2];
			vinfo[N - 1].Next = vinfo[0];
			vinfo[0].Prev = vinfo[N - 1];
			vinfo[N - 2].Next = vinfo[N - 1];

			for (int i = 0; i < N; ++i) {
				vinfo[i].Recompute();
			}

			var begin = vinfo[0];

			RemoveDegeneracies(ref begin, result);

			DoTriangulate(begin, result);
		}
		#endregion

		#region 非公開メソッド
		static int AxisOrdering(vector a, vector b, int axis) {
			var a1 = a[axis];
			var b1 = b[axis];
			if (a1 < b1) return -1;
			if (a1 > b1) return 1;
			var a2 = a[1 - axis];
			var b2 = b[1 - axis];
			if (a2 < b2) return -1;
			if (a2 > b2) return 1;
			return 0;
		}

		/// <summary>
		/// b-c が a-c の右側に向いているなら正数、どちらにも向いていないなら0、左側に向いているなら負数が返る
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static element Orient(vector a, vector b, vector c) {
			var ac = (c - a).RightAngle(); // a-c を時計回りに９０度回転
			var bc = b - c;
			return ac.Dot(bc);
		}

		static bool IsLeft(VertexInfo a, VertexInfo b, VertexInfo c) {
			if (a.Idx < b.Idx && b.Idx < c.Idx) {
				return Orient(a.P, b.P, c.P) > 0.0;
			} else if (a.Idx < c.Idx && c.Idx < b.Idx) {
				return Orient(a.P, c.P, b.P) < 0.0;
			} else if (b.Idx < a.Idx && a.Idx < c.Idx) {
				return Orient(b.P, a.P, c.P) < 0.0;
			} else if (b.Idx < c.Idx && c.Idx < a.Idx) {
				return Orient(b.P, c.P, a.P) > 0.0;
			} else if (c.Idx < a.Idx && a.Idx < b.Idx) {
				return Orient(c.P, a.P, b.P) > 0.0;
			} else {
				return Orient(c.P, b.P, a.P) < 0.0;
			}
		}

		static bool PointInTriangle(VertexInfo a, VertexInfo b, VertexInfo c, VertexInfo d) {
			return !IsLeft(a, c, d) && !IsLeft(b, a, d) && !IsLeft(c, b, d);
		}

		static int RemoveDegeneracies(ref VertexInfo begin, FList<Vector3i> result) {
			VertexInfo v;
			VertexInfo n;
			int count = 0;
			int remain = 0;

			v = begin;
			do {
				v = v.Next;
				++remain;
			} while (v != begin);

			v = begin;
			do {
				if (remain < 4) break;

				bool remove = false;
				if (v.P == v.Next.P) {
					remove = true;
				} else if (v.P == v.Next.Next.P) {
					if (v.Next.P == v.Next.Next.Next.P) {
						// a 'z' in the loop: z (a) b a b c . remove a-b-a . z (a) a b c . remove a-a-b (next loop) . z a b c
						// z --(a)-- b
						//         /
						//        /
						//      a -- b -- d
						remove = true;
					} else {
						// a 'shard' in the loop: z (a) b a c d . remove a-b-a . z (a) a b c d . remove a-a-b (next loop) . z a b c d
						// z --(a)-- b
						//         /
						//        /
						//      a -- c -- d
						// n.b. can only do this if the shard is pointing out of the polygon. i.e. b is outside z-a-c
						remove = !InternalToAngle(v.Next.Next.Next, v, v.Prev, v.Next.P);
					}
				}

				if (remove) {
					result.Add(new Vector3i(v.Idx, v.Next.Idx, v.Next.Next.Idx));
					n = v.Next;
					if (n == begin) begin = n.Next;
					n.Remove();
					count++;
					remain--;
				} else {
					v = v.Next;
				}
			} while (v != begin);

			return count;
		}

		static bool SplitAndResume(VertexInfo begin, FList<Vector3i> result) {
			VertexInfo v1, v2;

			if (!FindDiagonal(begin, out v1, out v2))
				return false;

			VertexInfo v1_copy = v1.Clone();
			VertexInfo v2_copy = v2.Clone();

			v1.Next = v2;
			v2.Prev = v1;

			v1_copy.Next.Prev = v1_copy;
			v2_copy.Prev.Next = v2_copy;

			v1_copy.Prev = v2_copy;
			v2_copy.Next = v1_copy;

			bool r1 = DoTriangulate(v1, result);
			bool r2 = DoTriangulate(v1_copy, result);
			return r1 && r2;
		}

		static bool FindDiagonal(VertexInfo begin, out VertexInfo v1, out VertexInfo v2) {
			VertexInfo t;
			var heap = new FList<VertexInfo>();

			v1 = begin;
			do {
				heap.Clear();

				for (v2 = v1.Next.Next; v2 != v1.Prev; v2 = v2.Next) {
					if (!InternalToAngle(v1.Next, v1, v1.Prev, v2.P) || !InternalToAngle(v2.Next, v2, v2.Prev, v1.P))
						continue;
					heap.Add(v2);
				}

				heap.Sort(new VertexInfoDistanceComparer(v1.P));

				while (heap.Count != 0) {
					var last = heap.Count - 1;
					v2 = heap[last];
					heap.RemoveAt(last);

					// test whether v1-v2 is a valid diagonal.
					var v_min_x = Math.Min(v1.P.X, v2.P.X);
					var v_max_x = Math.Max(v1.P.X, v2.P.X);

					bool intersected = false;

					for (t = v1.Next; !intersected && t != v1.Prev; t = t.Next) {
						VertexInfo u = t.Next;
						if (t == v2 || u == v2) continue;

						var l1 = Orient(v1.P, v2.P, t.P);
						var l2 = Orient(v1.P, v2.P, u.P);

						if ((l1 > 0.0 && l2 > 0.0) || (l1 < 0.0 && l2 < 0.0)) {
							// both on the same side; no intersection
							continue;
						}

						var dx13 = v1.P.X - t.P.X;
						var dy13 = v1.P.Y - t.P.Y;
						var dx43 = u.P.X - t.P.X;
						var dy43 = u.P.Y - t.P.Y;
						var dx21 = v2.P.X - v1.P.X;
						var dy21 = v2.P.Y - v1.P.Y;
						var ua_n = dx43 * dy13 - dy43 * dx13;
						var ub_n = dx21 * dy13 - dy21 * dx13;
						var u_d = dy43 * dx21 - dx43 * dy21;

						if (Math.Abs(u_d) < element.Epsilon) {
							// parallel
							if (Math.Abs(ua_n) < element.Epsilon) {
								// colinear
								if (Math.Max(t.P.X, u.P.X) >= v_min_x && Math.Min(t.P.X, u.P.X) <= v_max_x) {
									// colinear and intersecting
									intersected = true;
								}
							}
						} else {
							// not parallel
							var ua = ua_n / u_d;
							var ub = ub_n / u_d;

							if (0 <= ua && ua <= 1 && 0 <= ub && ub <= 1) {
								intersected = true;
							}
						}
					}

					if (!intersected) {
						// test whether midpoint winding == 1

						var mid = (v1.P + v2.P) / 2;
						if (WindingNumber(begin, mid) == 1) {
							// this diagonal is ok
							return true;
						}
					}
				}

				// couldn't find a diagonal from v1 that was ok.
				v1 = v1.Next;
			} while (v1 != begin);
			return false;
		}

		/** 
		 * \brief Determine whether p is internal to the anticlockwise
		 *        angle abc, where b is the apex of the angle.
		 *
		 * @param[in] a 
		 * @param[in] b 
		 * @param[in] c 
		 * @param[in] p 
		 * 
		 * @return true, if p is contained in the anticlockwise angle from
		 *               b->a to b->c. Reflex angles contain p if p lies
		 *               on b->a or on b->c. Acute angles do not contain p
		 *               if p lies on b->a or on b->c. This is so that
		 *               internalToAngle(a,b,c,p) = !internalToAngle(c,b,a,p)
		 */
		static bool InternalToAngle(vector a, vector b, vector c, vector p) {
			bool reflex = a.LessIdThan(c) ? Orient(b, a, c) <= 0.0 : Orient(b, c, a) > 0.0;
			var d1 = Orient(b, a, p);
			var d2 = Orient(b, c, p);
			if (reflex) {
				return d1 >= 0.0 || d2 <= 0.0;
			} else {
				return d1 > 0.0 && d2 < 0.0;
			}
		}

		/** 
		 * \brief Determine whether p is internal to the anticlockwise
		 *        angle ac, with apex at (0,0).
		 *
		 * @param[in] a 
		 * @param[in] c 
		 * @param[in] p 
		 * 
		 * @return true, if p is contained in a0c.
		 */
		static bool InternalToAngle(vector a,
									vector c,
									vector p) {
			return InternalToAngle(a, vector.Zero, c, p);
		}

		static bool InternalToAngle(VertexInfo a, VertexInfo b, VertexInfo c, vector p) {
			return InternalToAngle(a.P, b.P, c.P, p);
		}

		static int WindingNumber(VertexInfo begin, vector point) {
			int wn = 0;

			VertexInfo v = begin;
			do {
				if (v.P.Y <= point.Y) {
					if (v.Next.P.Y > point.Y && Orient(v.P, v.Next.P, point) > 0) {
						++wn;
					}
				} else {
					if (v.Next.P.Y <= point.Y && Orient(v.P, v.Next.P, point) < 0) {
						--wn;
					}
				}
				v = v.Next;
			} while (v != begin);

			return wn;
		}

		static bool DoTriangulate(VertexInfo begin, FList<Vector3i> result) {
			var vq = new EarQueue();

			var v = begin;
			int remain = 0;
			do {
				if (v.IsCandidate()) vq.Push(v);
				v = v.Next;
				remain++;
			} while (v != begin);

			while (remain > 3 && vq.Count != 0) {
				var v2 = vq.Pop();
				if (!v2.IsClipable()) {
					v2.Failed = true;
					continue;
				}

				continue_clipping:
				var n = v2.Next;
				var p = v2.Prev;

				result.Add(new Vector3i(v2.Prev.Idx, v2.Idx, v2.Next.Idx));

				v2.Remove();
				if (v2 == begin) begin = v2.Next;

				if (--remain == 3)
					break;

				vq.UpdateVertex(n);
				vq.UpdateVertex(p);

				if (n.Score < p.Score) {
					var t = n;
					n = p;
					p = t;
				}

				if (n.Score > 0.25 && n.IsCandidate() && n.IsClipable()) {
					vq.Remove(n);
					v2 = n;
					goto continue_clipping;
				}

				if (p.Score > 0.25 && p.IsCandidate() && p.IsClipable()) {
					vq.Remove(p);
					v2 = p;
					goto continue_clipping;
				}

			}


			if (remain > 3) {
				remain -= RemoveDegeneracies(ref begin, result);

				if (remain > 3) {
					return SplitAndResume(begin, result);
				}
			}

			if (remain == 3) {
				result.Add(new Vector3i(begin.Idx, begin.Next.Idx, begin.Next.Next.Idx));
			}

			var d = begin;
			do {
				var n = d.Next;
				d = n;
			} while (d != begin);

			return true;
		}

		static int MinElementIndex<T>(FList<T> list, IComparer<T> comparer) {
			var listCore = list.Core;
			T min = listCore.Items[0];
			int index = 0;
			for (int i = listCore.Count - 1; i != 0; i--) {
				var t = listCore.Items[i];
				if (comparer.Compare(t, min) < 0) {
					min = t;
					index = i;
				}
			}
			return index;
		}

		static bool LineSegmentIntersectionSimple(vector s1, vector e1, vector s2, vector e2) {
			var v = s1 - e1;
			var ox = s2.Y - s1.Y;
			var oy = s1.X - s2.X;
			if (0 <= (v.X * ox + v.Y * oy) * (v.X * (e2.Y - s1.Y) + v.Y * (s1.X - e2.X)))
				return false;
			v = s2 - e2;
			if (0 <= -(v.X * ox + v.Y * oy) * (v.X * (e1.Y - s2.Y) + v.Y * (s2.X - e1.X)))
				return false;
			return true;
		}

		static bool LineSegmentIntersectionSimple(ref LineSegment2 seg1, ref LineSegment2 seg2) {
			return LineSegmentIntersectionSimple(seg1.P1, seg1.P2, seg2.P1, seg2.P2);
		}

		static bool TestCandidateAttachment(FList<vector> currentFLoop, int curr, vector holeMin) {
			var count = currentFLoop.Count;
			var prev = (curr - 1 + count) % count;
			var next = (curr + 1) % count;

			if (!InternalToAngle(currentFLoop[next], currentFLoop[curr], currentFLoop[prev], holeMin)) {
				return false;
			}

			if (holeMin == currentFLoop[curr]) {
				return true;
			}

			var test = new LineSegment2(holeMin, currentFLoop[curr]);

			var v1 = count - 1;
			int v2 = 0;
			var v1_side = Orient(test.P1, test.P2, currentFLoop[v1]);
			element v2_side = 0;

			while (v2 != count) {
				v2_side = Orient(test.P1, test.P2, currentFLoop[v2]);

				if (v1_side != v2_side) {
					// XXX: need to test vertices, not indices, because they may be duplicated.
					if (currentFLoop[v1] != currentFLoop[curr] && currentFLoop[v2] != currentFLoop[curr]) {
						var test2 = new LineSegment2(currentFLoop[v1], currentFLoop[v2]);
						if (LineSegmentIntersectionSimple(ref test, ref test2)) {
							// intersection; failed.
							return false;
						}
					}
				}

				v1 = v2;
				v1_side = v2_side;
				++v2;
			}
			return true;
		}

		/** 
		 * \brief Given a polygon loop and a hole loop, and attachment
		 * points, insert the hole loop vertices into the polygon loop.
		 * 
		 * @param[in,out] f_loop The polygon loop to incorporate the
		 *                       hole into.
		 * @param f_loop_attach[in] The index of the vertex of the
		 *                          polygon loop that the hole is to be
		 *                          attached to.
		 * @param hole_attach[in] A pair consisting of a pointer to a
		 *                        hole container and an iterator into
		 *                        that container reflecting the point of
		 *                        attachment of the hole.
		 */
		static void PatchHoleIntoPolygon(FList<vector> f_loop, int f_loop_attach, Iterator<vector> hole_attach) {
			// join the vertex curr of the polygon loop to the hole at
			// h_loop_connect
			var hole_temp = new vector[hole_attach.List.Count + 2];
			for (int i = 0, n = hole_attach.List.Count; i <= n; i++) {
				hole_temp[i] = hole_attach.List[(hole_attach.Index + i) % n];
			}
			hole_temp[hole_temp.Length - 1] = f_loop[f_loop_attach];
			f_loop.InsertRange(f_loop_attach + 1, hole_temp);
		}
		#endregion
	}
}
