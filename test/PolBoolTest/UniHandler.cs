using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Jk;

using element = System.Single;
using polbool = Jk.PolBool2f;
using bool2dutil = Jk.BoolUtil2f<System.Windows.Media.Brush>;
using vector = Jk.Vector2f;
using bezier = Jk.Bezier2f;
using geo = Jk.Geom2f;

namespace PolBoolTest {
	/// <summary>
	/// キャンバス用共通イベント処理
	/// </summary>
	public class UniHandler {
		const element CalcEpsilon = (element)1.0e-20;
		const element BezierCircle = (element)0.55228;
		const element BezierCircle2 = BezierCircle * 2;

		FList<bool2dutil.Polygon> _Polygons = new FList<bool2dutil.Polygon>();
		Grid _Grid;
		Canvas _Canvas;
		Point _StartPoint;
		Polyline _Polyline;
		bool _PolylineDrawing;
		bool _ImageGrabbing;
		Point _ImageGrabPoint;
		public bool Drawable = true;
		Brush _FaceBrush = new SolidColorBrush(Color.FromRgb(255, 127, 0));
		Brush _EdgeBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
		PolBool2f _Pb;
		bool _NodeGrabbing;
		Brush[] _FaceBrushes = new Brush[] {
			new SolidColorBrush(Color.FromRgb(255, 127, 0)),
			new SolidColorBrush(Color.FromRgb(127, 255, 0)),
			new SolidColorBrush(Color.FromRgb(0, 127, 255)),
			new SolidColorBrush(Color.FromRgb(0, 255, 127)),
		};

		Matrix RenderTransformMatrix {
			get {
				return _Canvas.RenderTransform.Value;
			}
			set {
				_Canvas.RenderTransform = new MatrixTransform(value);
			}
		}

		public Brush[] FaceBrushes { get => _FaceBrushes; }
		public Brush FaceBrush { get => _FaceBrushes[this.FaceBrushIndex]; }
		public int FaceBrushIndex;

		public UniHandler(Grid grid, Canvas canvas) {
			_Grid = grid;
			_Canvas = canvas;
			grid.MouseDown += Grid_MouseDown;
			grid.MouseUp += Grid_MouseUp;
			grid.MouseMove += Grid_MouseMove;
			grid.MouseWheel += Grid_MouseWheel;

			var tf = new ScaleTransform(1, -1);
			this.RenderTransformMatrix = tf.Value;
		}

		public void Clear() {
			this._Canvas.Children.Clear();
			_Polygons.Clear();
		}

		public void Or() {
			if (_Pb == null)
				return;
			_Canvas.Children.Clear();
			_Pb.Groups.RemoveAt(0);
			_Pb.CreateTopology(false);
			foreach (var shape in ToWpf(bool2dutil.Or(_Pb))) {
				_Canvas.Children.Add(shape);
			}
			GlobalLogger.CreateFile(_Pb.ToJsonString(), "pb.after_or", ".json");
		}

		private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right) {
				if (this.Drawable && !_PolylineDrawing) {
					_StartPoint = e.GetPosition(_Canvas);

					var polyline = new Polyline();
					polyline.Stroke = this.FaceBrush;
					polyline.StrokeThickness = 2;
					_Canvas.Children.Add(polyline);

					_Polyline = polyline;

					_PolylineDrawing = true;
					_Canvas.CaptureMouse();
				}
			} else if (e.ChangedButton == MouseButton.Middle) {
				if (!_ImageGrabbing) {
					_ImageGrabPoint = e.GetPosition(_Canvas);
					_ImageGrabbing = true;
					_Canvas.CaptureMouse();
				}
			}
		}

		private void Grid_MouseUp(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left || this.Drawable && e.ChangedButton == MouseButton.Right) {
				if (_PolylineDrawing) {
					_PolylineDrawing = false;
					_Canvas.ReleaseMouseCapture();
					_Canvas.Children.Clear();

					var newPolygons = PolygonsFromPolyline(_Polyline);

					if (e.ChangedButton == MouseButton.Left) {
						if (newPolygons.Count != 0) {
							_Polygons = PaintPolygons(_Polygons, newPolygons);
						}
					} else if (e.ChangedButton == MouseButton.Right) {
						if (newPolygons.Count != 0) {
							_Polygons = ErasePolygons(_Polygons, newPolygons);
						}
					}
					foreach (var pol in _Polygons) {
						var p = ToWpf(pol);
						_Canvas.Children.Add(p);
					}

					//ToBezierPolyline(_Polyline);
					_Polyline = null;
				}
			} else if (e.ChangedButton == MouseButton.Middle) {
				if (_ImageGrabbing) {
					_ImageGrabbing = false;
					_Canvas.ReleaseMouseCapture();
				}
			}
		}

		private void Grid_MouseMove(object sender, MouseEventArgs e) {
			if (_PolylineDrawing) {
				_Polyline.Points.Add(e.GetPosition(_Canvas));
			}
			if (_ImageGrabbing) {
				var mtx = this.RenderTransformMatrix;
				var p = mtx.Transform(e.GetPosition(_Canvas));
				var d = p - mtx.Transform(_ImageGrabPoint);
				mtx.OffsetX += d.X;
				mtx.OffsetY += d.Y;
				this.RenderTransformMatrix = mtx;
			}
		}

		private void Grid_MouseWheel(object sender, MouseWheelEventArgs e) {
			var mtx = this.RenderTransformMatrix;
			var mp = e.GetPosition(_Canvas);
			var s = e.Delta > 0 ? 1.1 : 1.0 / 1.1;
			mtx.ScaleAt(s, s, mp.X, mp.Y);
			this.RenderTransformMatrix = mtx;
		}


		static List<Bezier2f.Cubic> PolylineToBeziers(Polyline polyline) {
			var d = (from p in polyline.Points select new Vector2f((float)p.X, (float)p.Y)).ToArray();
			var beziers = new List<Bezier2f.Cubic>();
			Bezier2f.FitCubic(d, 3 * 3, false, beziers);
			return beziers;
		}

		void ToBezierPolyline(Polyline polyline) {
			var beziers = PolylineToBeziers(polyline);
			var interpolated = Bezier2f.Interpolate(beziers, 3).ToArray();
			var dinterpolated = Bezier2f.DiffInterpolate(beziers, 10).ToArray();
			for (int i = dinterpolated.Length - 1; i != -1; i--) {
				var v = dinterpolated[i];
				v.RelengthSelf(10);
				v.RightAngleSelf();
				dinterpolated[i] = v;
			}
			ToPolyline(interpolated, Color.FromRgb(0, 127, 255), 1);
			ToPolyline(interpolated.Zip(dinterpolated, (p, v) => p + v), Color.FromRgb(0, 255, 0), 1);
			ToPolyline(interpolated.Zip(dinterpolated, (p, v) => p - v), Color.FromRgb(0, 255, 0), 1);
		}

		Polyline ToPolyline(IEnumerable<Vector2f> points, Color color, double thickness) {
			var bpolyline = new Polyline();
			bpolyline.Stroke = new SolidColorBrush(color);
			bpolyline.StrokeThickness = thickness;
			bpolyline.Points = new PointCollection(from p in points select new Point(p.X, p.Y));
			_Canvas.Children.Add(bpolyline);
			return bpolyline;
		}

		FList<bool2dutil.Polygon> PolygonsFromPolyline(Polyline polyline) {
			var list = new FList<bool2dutil.Polygon>();

			if (polyline.Points.Count <= 1)
				return list;

			var beziers = PolylineToBeziers(polyline);
			var interpolated = new FList<vector>(Bezier2f.Interpolate(beziers, 3));

			list.AddRange(bool2dutil.CreatePolylinePolygon(interpolated, 50, FaceBrush, _EdgeBrush));

			return list;
		}

		bool2dutil.Polygon PolygonFromLine(vector p1, vector p2, float width, Brush faceBrush, Brush edgeBrush) {
			var v = p2 - p1;
			var ax = v.Normalize();
			var ay = ax.RightAngle();
			var vx = ax * width;
			var vy = vx.RightAngle();

			var n = 4;
			var table = new Vector2f[n - 1];
			var step = 180.0 / n;
			for (int i = 1; i < n; i++) {
				var rad = (90.0 - i * step) * Math.PI / 180;
				table[i - 1] = vx * (float)Math.Cos(rad) + vy * (float)Math.Sin(rad);
			}

			var vertices = new List<bool2dutil.Vertex>();
			vertices.Add(new bool2dutil.Vertex(p1 + vy));
			vertices.Add(new bool2dutil.Vertex(p2 + vy));
			for (int i = 1; i < n; i++) {
				vertices.Add(new bool2dutil.Vertex(p2 + table[i - 1]));
			}
			vertices.Add(new bool2dutil.Vertex(p2 - vy));
			vertices.Add(new bool2dutil.Vertex(p1 - vy));
			for (int i = 1; i < n; i++) {
				vertices.Add(new bool2dutil.Vertex(p1 - table[i - 1]));
			}

			var loop = new bool2dutil.Loop(vertices);

			var pol = new bool2dutil.Polygon();
			pol.Loops.Add(loop);
			pol.FaceMaterial = faceBrush;
			pol.EdgeMaterial = edgeBrush;

			return pol;
		}

		FList<bool2dutil.Polygon> OrPolygons(FList<bool2dutil.Polygon> polygons) {
			return bool2dutil.Or(polygons).ToPolygons();
		}

		FList<bool2dutil.Polygon> PaintPolygons(FList<bool2dutil.Polygon> srcPolygons, FList<bool2dutil.Polygon> polygons) {
			return bool2dutil.Paint(srcPolygons, polygons).ToPolygons();
		}

		FList<bool2dutil.Polygon> ErasePolygons(FList<bool2dutil.Polygon> srcPolygons, FList<bool2dutil.Polygon> polygons) {
			return bool2dutil.Erase(srcPolygons, polygons, 1).ToPolygons();
		}

		Shape ToWpf(bool2dutil.Polygon pol) {
			var gg = new GeometryGroup();
			foreach (var loop in pol.Loops) {
				var wpol = new Polygon();
				wpol.Fill = pol.FaceMaterial;
				wpol.Stroke = pol.EdgeMaterial;
				wpol.Points = new PointCollection(from v in loop.Vertices select TP(v.Position));
				wpol.Arrange(new Rect(_Canvas.RenderSize));
				wpol.Measure(_Canvas.RenderSize);
				gg.Children.Add(wpol.RenderedGeometry);
			}

			var path = new Path();
			path.Stroke = pol.EdgeMaterial;
			path.Fill = pol.FaceMaterial;
			path.Data = gg;
			return path;
		}

		IEnumerable<Shape> ToWpf(bool2dutil.IO io) {
			return from p in io.ToPolygons() select ToWpf(p);
		}

		static Point TP(Vector2f v) {
			return new Point(v.X, v.Y);
		}

		static Vector2f FP(Point p) {
			return new Vector2f((float)p.X, (float)p.Y);
		}

		static Vector2f FP(Vector p) {
			return new Vector2f((float)p.X, (float)p.Y);
		}

		//public void TestBezierPolyline() {
		//	var points = new FList<vector>();
		//	points.Add(new vector(-100, 0));
		//	points.Add(new vector(0, 1000));
		//	points.Add(new vector(100, 0));
		//	_Canvas.Children.Add(ToWpf(bool2dutil.PolygonsFromPolyline(points, 10, _FaceBrush, _EdgeBrush)[0]));
		//}

		public void TestCapsulePolygon() {
			var width = 50.0f;
			var p1 = new vector(0, 0);
			var p2 = new vector(10, 0);
			var rad = 90 * Math.PI / 180;
			var ax = new vector((element)Math.Cos(rad) * width, (element)Math.Sin(rad) * width);
			var shape = ToWpf(bool2dutil.CreateCapsulePolygon(p1, p2, bool2dutil.CapsulePolygonFlags.StartCap | bool2dutil.CapsulePolygonFlags.EndConnect, ax, width, this.FaceBrush, _EdgeBrush));
			_Canvas.Children.Add(shape);
		}

		public void TestNest() {
			var p1 = CreateRectangle(100, _FaceBrushes[0], _EdgeBrush);
			var p2 = CreateRectangle(50, _FaceBrushes[1], _EdgeBrush);
			var pb = bool2dutil.Topology(new bool2dutil.IO(true, p1), new bool2dutil.IO(true, p2));
			var result = bool2dutil.Paint(pb);
			foreach (var shape in ToWpf(result)) {
				_Canvas.Children.Add(shape);
			}
		}

		public void TestDonutOr() {
			var p1 = CreateDonut(100);
			var p2 = CreateRectangle(75, this.FaceBrush, _EdgeBrush);
			var pb = bool2dutil.Topology(new bool2dutil.IO(true, p1), new bool2dutil.IO(true, p2));
			var result = bool2dutil.Or(pb);
			foreach (var shape in ToWpf(result)) {
				_Canvas.Children.Add(shape);
			}
		}

		public void TestHourglassOr() {
			var p1 = CreateHourglass2(100);
			var pb = bool2dutil.Topology(new bool2dutil.IO(true, p1));
			GlobalLogger.CreateFile(pb.ToJsonString(), "pb.hourglass", ".json");
			var result = bool2dutil.Or(pb);
			foreach (var shape in ToWpf(result)) {
				_Canvas.Children.Add(shape);
			}
		}

		bool2dutil.Polygon CreateRectangle(float size, Brush faceBrush, Brush edgeBrush) {
			var vertices = new vector[][] {
				new vector[] {
					new vector(-size, -size),
					new vector(-size, size),
					new vector(size, size),
					new vector(size, -size),
				},
			};
			var pol = new bool2dutil.Polygon(vertices);
			pol.FaceMaterial = faceBrush;
			pol.EdgeMaterial = edgeBrush;
			return pol;
		}

		bool2dutil.Polygon CreateDonut(float size) {
			var sizeHalf = size * 0.5f;
			var vertices = new vector[][] {
				new vector[] {
					new vector(-size, -size),
					new vector(-size, size),
					new vector(size, size),
					new vector(size, -size),
				},
				new vector[] {
					new vector(-sizeHalf, -sizeHalf),
					new vector(-sizeHalf, sizeHalf),
					new vector(sizeHalf, sizeHalf),
					new vector(sizeHalf, -sizeHalf),
				}
			};
			var pol = new bool2dutil.Polygon(vertices);
			pol.FaceMaterial = FaceBrush;
			pol.EdgeMaterial = _EdgeBrush;
			return pol;
		}

		bool2dutil.Polygon CreateHourglass(float size) {
			var sizeHalf = size * 0.5f;
			var vertices = new vector[][] {
				new vector[] {
					new vector(-size, -size),
					new vector(size, size),
					new vector(-size, size),
					new vector(size, -size),
				},
			};
			var pol = new bool2dutil.Polygon(vertices);
			pol.FaceMaterial = FaceBrush;
			pol.EdgeMaterial = _EdgeBrush;
			return pol;
		}

		bool2dutil.Polygon CreateHourglass2(float size) {
			var sizeHalf = size * 0.5f;
			var vertices = new vector[][] {
				new vector[] {
					new vector(-size, -size * 2),
					new vector(size, 0),
					new vector(-size, size * 2),
					new vector(size, size * 2),
					new vector(-size, 0),
					new vector(size, -size * 2),
				},
			};
			var pol = new bool2dutil.Polygon(vertices);
			pol.FaceMaterial = FaceBrush;
			pol.EdgeMaterial = _EdgeBrush;
			return pol;
		}

		public void OpenPolBool2f(string file) {
			var text = System.IO.File.ReadAllText(file);
			var pb = JsonConvert.DeserializeObject<PolBool2f>(text);
			foreach (var group in pb.Groups) {
				foreach (var polygon in group) {
					var pol = JsonConvert.DeserializeObject<bool2dutil.Polygon>(polygon.UserData.ToString());
					polygon.UserData = pol;
					_Canvas.Children.Add(ToWpf(pol));
				}
			}
			_Pb = pb;
		}

		public void GetTopo() {
			_Pb = bool2dutil.Topology(_Polygons);
		}

		public void ShowTopo() {
			if (_Pb == null)
				return;
			AddWpfElement(_Pb);
		}

		public void AddWpfElement(PolBool2f pb) {
			pb.CreateTopology();

			var scale = 10.0;

			var links = new MultiDict<polbool.Node, Line>();

			// エッジ追加
			var edgeRightStroke = new SolidColorBrush(Color.FromArgb(127, 255, 127, 127));
			var edgeLeftStroke = new SolidColorBrush(Color.FromArgb(127, 127, 255, 127));
			foreach (var edge in pb.Edges) {
				var p1 = edge.From.Position;
				var p2 = edge.To.Position;
				var v = p2 - p1;
				var ax = v.Normalize();
				var ay = ax.RightAngle() * (element)(0.005 * scale);

				var rp1 = p1 - ay;
				var rp2 = p2 - ay;
				var rightLine = new Line();
				rightLine.X1 = rp1.X;
				rightLine.Y1 = rp1.Y;
				rightLine.X2 = rp2.X;
				rightLine.Y2 = rp2.Y;
				rightLine.Stroke = edgeRightStroke;
				rightLine.StrokeThickness = 0.01 * scale;
				rightLine.Tag = new EdgeUiInfo { Edge = edge };
				_Canvas.Children.Add(rightLine);

				var lp1 = p1 + ay;
				var lp2 = p2 + ay;
				var leftLine = new Line();
				leftLine.X1 = lp1.X;
				leftLine.Y1 = lp1.Y;
				leftLine.X2 = lp2.X;
				leftLine.Y2 = lp2.Y;
				leftLine.Stroke = edgeLeftStroke;
				leftLine.StrokeThickness = 0.01 * scale;
				leftLine.Tag = new EdgeUiInfo { Edge = edge };
				_Canvas.Children.Add(leftLine);

				links.Add(edge.From, rightLine);
				links.Add(edge.From, leftLine);
				links.Add(edge.To, rightLine);
				links.Add(edge.To, leftLine);
			}

			// ノード追加
			var nodeFill = new SolidColorBrush(Color.FromArgb(127, 0, 127, 255));
			var textFill = new SolidColorBrush(Color.FromArgb(127, 255, 255, 255));
			var tbs = new FList<TextBlock>();
			foreach (var node in pb.Nodes) {
				var p = node.Position;

				// ノードに繋がるエッジ数
				var tb = new TextBlock();
				tb.Text = node.Edges.Count.ToString();
				tb.FontSize = 0.1 * scale;
				tb.Foreground = textFill;
				tb.RenderTransform = new ScaleTransform(1, -1);
				Canvas.SetLeft(tb, p.X);
				Canvas.SetTop(tb, p.Y + 0.1 * scale);
				tbs.Add(tb);

				// ノード
				var nodeUi = new Ellipse();
				nodeUi.Fill = nodeFill;
				nodeUi.Width = 0.2 * scale;
				nodeUi.Height = 0.2 * scale;
				nodeUi.PreviewMouseDown += NodeUi_PreviewMouseDown;
				nodeUi.MouseDown += NodeUi_MouseDown;
				nodeUi.MouseUp += NodeUi_MouseUp;
				nodeUi.MouseMove += NodeUi_MouseMove;
				Canvas.SetLeft(nodeUi, p.X - 0.1 * scale);
				Canvas.SetTop(nodeUi, p.Y - 0.1 * scale);

				var lines = links[node];
				nodeUi.Tag = new NodeUiInfo {
					Node = node,
					CountUi = tb,
					EdgeUis = (from l in lines select new Tuple<Line, int>(l, (l.Tag as EdgeUiInfo).Edge.GetNodeIndex(node))).ToArray()
				};

				_Canvas.Children.Add(nodeUi);
			}

			// ノードに繋がるエッジ数表示追加
			foreach (var tb in tbs) {
				_Canvas.Children.Add(tb);
			}
		}

		private void NodeUi_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
		}

		private void NodeUi_MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left) {
				if (!_NodeGrabbing) {
					var nodeUi = sender as Ellipse;
					if (nodeUi != null) {
						_StartPoint = e.GetPosition(_Canvas);
						_NodeGrabbing = true;
						nodeUi.CaptureMouse();
						e.Handled = true;
					}
				}
			}
		}

		private void NodeUi_MouseUp(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left) {
				if (_NodeGrabbing) {
					var nodeUi = sender as Ellipse;
					if (nodeUi != null) {
						_NodeGrabbing = false;
						nodeUi.ReleaseMouseCapture();
					}
				}
			}
		}

		private void NodeUi_MouseMove(object sender, MouseEventArgs e) {
			if (_NodeGrabbing) {
				var nodeUi = sender as Ellipse;
				if (nodeUi != null) {
					var p = e.GetPosition(_Canvas);
					var v = p - _StartPoint;
					var nui = nodeUi.Tag as NodeUiInfo;

					Canvas.SetLeft(nodeUi, Canvas.GetLeft(nodeUi) + v.X);
					Canvas.SetTop(nodeUi, Canvas.GetTop(nodeUi) + v.Y);

					Canvas.SetLeft(nui.CountUi, Canvas.GetLeft(nui.CountUi) + v.X);
					Canvas.SetTop(nui.CountUi, Canvas.GetTop(nui.CountUi) + v.Y);

					foreach (var l in nui.EdgeUis) {
						if (l.Item2 == 0) {
							l.Item1.X1 += v.X;
							l.Item1.Y1 += v.Y;
						} else {
							l.Item1.X2 += v.X;
							l.Item1.Y2 += v.Y;
						}
					}

					_StartPoint = p;
				}
			}
		}
	}

	public class NodeUiInfo {
		public polbool.Node Node;
		public TextBlock CountUi;
		public Tuple<Line, int>[] EdgeUis;
	}

	public class EdgeUiInfo {
		public polbool.Edge Edge;
	}

	public class MultiDict<TKey, TValue> where TValue : class {
		private Dictionary<TKey, FList<TValue>> Dic = new Dictionary<TKey, FList<TValue>>();

		public FList<TValue> this[TKey key] {
			get {
				FList<TValue> list;
				if (this.Dic.TryGetValue(key, out list)) {
					return list;
				} else {
					return null;
				}
			}
		}

		public void Add(TKey k, TValue v) {
			FList<TValue> list;
			if (this.Dic.TryGetValue(k, out list)) {
				list.Add(v);
			} else {
				list = new FList<TValue>();
				list.Add(v);
				this.Dic.Add(k, list);
			}
		}
	}
}
