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
using System.ComponentModel;
using Jk;
using Microsoft.Win32;

namespace PolBoolTest {
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window, IPN {
		public event PropertyChangedEventHandler PropertyChanged;

		public PropertyChangedEventHandler PropertyChangedEvent {
			get {
				return this.PropertyChanged;
			}
		}

		UniHandler _LtHandler;

		public Brush[] Brushes {
			get {
				return _LtHandler.FaceBrushes;
			}
		}

		public int BrushIndex {
			get {
				return _LtHandler.FaceBrushIndex;
			}
			set {
				this.PNSet(ref _LtHandler.FaceBrushIndex, ref value);
			}
		}

		public MainWindow() {
			InitializeComponent();

			this.DataContext = this;

			_LtHandler = new UniHandler(this.gridLt, this.canvasLt);

			this.Loaded += MainWindow_Loaded;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
			this.cmbLt.ItemsSource = _LtHandler.FaceBrushes;
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e) {
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.FilterIndex = 1;
			ofd.Filter = "Json files|*.json|All Files (*.*)|*.*";
			bool? r = ofd.ShowDialog();
			if (r == false)
				return;
			_LtHandler.OpenPolBool2f(ofd.FileName);
		}

		private void btnLtClear_Click(object sender, RoutedEventArgs e) {
			_LtHandler.Clear();
		}

		private void btnLtOr_Click(object sender, RoutedEventArgs e) {
			_LtHandler.Or();
		}

		private void btnLtTest_Click(object sender, RoutedEventArgs e) {
			_LtHandler.TestCapsulePolygon();
			//_LtHandler.TestNest();
			//_LtHandler.TestHourglassOr();
			//_LtHandler.TestBezierPolyline();
		}

		private void btnLtGet_Click(object sender, RoutedEventArgs e) {
			_LtHandler.GetTopo();
		}

		private void btnLtTopo_Click(object sender, RoutedEventArgs e) {
			//_LtHandler.TestDonutOr();
			_LtHandler.ShowTopo();
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
	}
}
