using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Jk {
	/// <summary>
	/// どこからでも呼び出せるロガー、ログフォルダは実行ファイルと同じ階層に作られる、デバッグ時に使うことを想定しておりリリース時は使わないこと
	/// </summary>
	public static class GlobalLogger {
		#region クラス
		/// <summary>
		/// フレームログタイプ
		/// </summary>
		public enum FrameLogType {
			/// <summary>
			/// フレーム入場時のログ
			/// </summary>
			Enter,

			/// <summary>
			/// フレーム退場時のログ
			/// </summary>
			Leave,

			/// <summary>
			/// フレーム中でのログ
			/// </summary>
			Comment,
		}

		/// <summary>
		/// フレームに入る際の引数
		/// </summary>
		public struct FrameArg : IJsonable {
			/// <summary>
			/// 引数名
			/// </summary>
			public string Name;

			/// <summary>
			/// 引数値
			/// </summary>
			public object Value;

			public FrameArg(string name, object value) {
				this.Name = name;
				this.Value = value;
			}

			public override string ToString() {
				return Jsonable.Fields(
					nameof(this.Name), this.Name,
					nameof(this.Value), this.Value);
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// フレーム用引数列
		/// </summary>
		public class FrameArgs : IJsonable {
			public FrameArg[] Args;

			/// <summary>
			/// 引数列を初期化する
			/// </summary>
			/// <param name="args">引数名、値の繰り返し</param>
			public FrameArgs(params object[] args) {
				var fargs = new FrameArg[args.Length / 2];
				for (int i = 0; i < fargs.Length; i++) {
					var j = i * 2;
					fargs[i] = new FrameArg(args[j]?.ToString(), args[j + 1]);
				}
				this.Args = fargs;
			}

			public override string ToString() {
				var args = this.Args;
				var fields = new object[args.Length * 2];
				for (int i = 0; i < args.Length; i++) {
					var j = i * 2;
					var arg = args[i];
					fields[j] = arg.Name;
					fields[j + 1] = arg.Value;
				}
				return Jsonable.Fields(fields);
			}

			public string ToJsonString() {
				return this.ToString();
			}
		}

		/// <summary>
		/// メソッドやブロックを識別するフレーム情報
		/// </summary>
		public class Frame {
			/// <summary>
			/// フレーム名、null なら<see cref="Index"/>のみが使用される
			/// </summary>
			public string Name;

			/// <summary>
			/// フレーム番号、同名のフレームが何回呼び出されたかなど識別するために使用する、、負数なら<see cref="Name"/>のみが使用される
			/// </summary>
			public int Index;

			/// <summary>
			/// フレーム名とフレーム番号を指定して初期化する
			/// </summary>
			/// <param name="name">フレーム名、nullならフレーム名無し</param>
			/// <param name="index">フレーム番号、負数なら番号無し</param>
			public Frame(string name, int index) {
				this.Name = name;
				this.Index = index;
			}

			/// <summary>
			/// フレーム名を指定して初期化する
			/// </summary>
			/// <param name="name">フレーム名</param>
			public Frame(string name) {
				this.Name = name;
				this.Index = -1;
			}

			/// <summary>
			/// フレーム番号を指定して初期化する
			/// </summary>
			/// <param name="index">フレーム番号</param>
			public Frame(int index) {
				this.Name = null;
				this.Index = index;
			}

			public override string ToString() {
				if (string.IsNullOrEmpty(this.Name)) {
					return 0 <= this.Index ? this.Index.ToString() : "";
				} else {
					return 0 <= this.Index ? string.Concat(this.Name, this.Index) : this.Name;
				}
			}
		}

		/// <summary>
		/// <see cref="Frame"/>をスタック状にして階層情報を保持する
		/// </summary>
		public class FrameStack {
			FList<Frame> _Frames = new FList<Frame>();
			Dictionary<string, int> _FrameIndices = new Dictionary<string, int>();

			/// <summary>
			/// 現在のスタックの深さを取得する
			/// </summary>
			public int Depth {
				get {
					return _Frames.Count;
				}
			}

			/// <summary>
			/// フレーム名とフレーム番号を指定して新しいフレームを作る
			/// </summary>
			/// <param name="name">フレーム名、nullならフレーム名無し</param>
			/// <param name="index">フレーム番号、負数なら番号無し</param>
			public void Push(string name, int index) {
				_Frames.Add(new Frame(name, index));
			}

			/// <summary>
			/// フレーム名を指定して新しいフレームを作る
			/// </summary>
			/// <param name="name">フレーム名</param>
			public void Push(string name) {
				_Frames.Add(new Frame(name));
			}

			/// <summary>
			/// フレーム番号を指定して新しいフレームを作る
			/// </summary>
			/// <param name="index">フレーム番号</param>
			public void Push(int index) {
				_Frames.Add(new Frame(index));
			}

			/// <summary>
			/// フレーム名を指定して新しいフレームを作る、同名フレーム番号は自動インクリメントされて付与される
			/// </summary>
			/// <param name="name">フレーム名</param>
			public void PushIncrement(string name) {
				int index;
				if (_FrameIndices.TryGetValue(name, out index)) {
					index++;
				} else {
					index = 1;
				}
				_FrameIndices[name] = index;
				Push(name, index);
			}

			/// <summary>
			/// 最新のフレームを破棄する
			/// </summary>
			public void Pop() {
				//if (_Frames.Count == 0)
				//	return;
				_Frames.RemoveAt(_Frames.Count - 1);
			}

			/// <summary>
			/// 現在スタックに積まれている指定されたフレーム名の番号をインクリメントする
			/// </summary>
			/// <param name="name">フレーム名</param>
			public void Increment(string name) {
				var frames = _Frames;
				for (int i = 0, n = frames.Count; i < n; i++) {
					var f = frames[i];
					if (f.Name == name) {
						f.Index++;
						break;
					}
				}
			}

			/// <summary>
			/// 現在のスタックの状態を文字列で取得する
			/// </summary>
			/// <param name="name">必要なら末端に追加するの名前を指定する、null指定可能</param>
			/// <param name="index">必要なら末端に追加する番号を指定する、負数なら未指定とみなす</param>
			/// <returns>文字列配列</returns>
			public string ToString(string name, int index) {
				var sb = new StringBuilder();
				var frames = this._Frames;
				for (int i = 0, n = frames.Count; i < n; i++) {
					if (sb.Length != 0)
						sb.Append("_");
					sb.Append(frames[i].ToString());
				}
				var nameIsNull = string.IsNullOrEmpty(name);
				if (nameIsNull && index < 0)
					return sb.ToString();

				if (sb.Length != 0)
					sb.Append("_");
				if (!nameIsNull) {
					sb.Append(name);
					if (0 <= index)
						sb.Append(index.ToString());
				} else {
					sb.Append(index.ToString());
				}
				return sb.ToString();
			}

			public string ToString(string name) {
				return ToString(name, -1);
			}

			public string ToString(int index) {
				return ToString(null, index);
			}

			public override string ToString() {
				return ToString(null, -1);
			}

			/// <summary>
			/// 現在のスタックの状態を文字列配列で取得する
			/// </summary>
			/// <param name="branchIsIndent">末端以外はインデントとして表現する</param>
			/// <param name="name">必要なら末端に追加するの名前を指定する、null指定可能</param>
			/// <param name="index">必要なら末端に追加する番号を指定する、負数なら未指定とみなす</param>
			/// <returns>文字列配列</returns>
			public string[] ToFields(bool branchIsIndent = true, string name = null, int index = -1) {
				var newFrame = !string.IsNullOrEmpty(name) || 0 <= index;
				var framesCore = this._Frames.Core;
				var fields = new string[newFrame ? framesCore.Count + 1 : framesCore.Count];
				var last = fields.Length - 1;

				for (int i = 0; i < framesCore.Count; i++)
					fields[i] = branchIsIndent && i != last ? "" : framesCore.Items[i].ToString();

				if (!newFrame)
					return fields;

				fields[last] = string.IsNullOrEmpty(name) ? 0 <= index ? index.ToString() : "" : 0 <= index ? string.Concat(name, index) : name;
				return fields;
			}

			/// <summary>
			/// 現在のスタックの深さ分のインデントフィールドを作成する
			/// </summary>
			/// <returns>文字列配列</returns>
			public string[] ToIndentFields() {
				var fields = new string[_Frames.Count];
				for (int i = 0; i < fields.Length; i++)
					fields[i] = "";
				return fields;
			}
		}

		/// <summary>
		/// using によりフレーム破棄を自動化するためのクラス
		/// </summary>
		public class FuncFrame : IDisposable {
			FrameStack _FrameStack;
			FrameArgs _Args;
			System.Diagnostics.Stopwatch _Stopwatch;

			public FuncFrame(FrameStack frameStack, FrameArgs args) {
				_FrameStack = frameStack;
				_Args = args;
				AddFrameLogLine(FrameLogType.Enter, frameStack, args);
				_Stopwatch = new System.Diagnostics.Stopwatch();
				_Stopwatch.Start();
			}

			public void Dispose() {
				if (_FrameStack != null) {
					AddFrameLogLine(FrameLogType.Leave, _FrameStack, null, _Stopwatch.Elapsed.TotalMilliseconds + "ms");
					_FrameStack.Pop();
					_FrameStack = null;
				}
				_Args = null;
			}
		}
		#endregion

		#region フィールド
		static Logger _Logger = new Logger(AppDomain.CurrentDomain.BaseDirectory, "global", ".csv");
		[ThreadStatic]
		static FrameStack _ThreadFrameStack;
		#endregion

		#region プロパティ
		/// <summary>
		/// スレッド毎のフレームスタック
		/// </summary>
		static FrameStack ThreadFrameStack {
			get => _ThreadFrameStack ?? (_ThreadFrameStack = new FrameStack());
		}
		#endregion

		#region 公開メソッド
		/// <summary>
		/// ログ文字列を１行追加する
		/// </summary>
		/// <param name="text">ログ追加文字列</param>
		/// <returns>ログに出力されたCSVデータが返る</returns>
		public static List<string> AddLogLine(params string[] text) {
			lock (_Logger) {
				return _Logger.AddLogLine(DateTime.Now, text);
			}
		}

		/// <summary>
		/// 拡張ログファイルを作成する
		/// </summary>
		/// <param name="contents">ファイル内容</param>
		/// <param name="prefix2">ファイル名に付与される２番目のプレフィックス</param>
		/// <param name="ext">ファイルの拡張子</param>
		public static void CreateFile(string contents, string prefix2, string ext) {
			lock (_Logger) {
				_Logger.CreateFile(contents, prefix2, ext);
			}
		}

		/// <summary>
		/// 指定名のフレームに番号を付けて作成する
		/// </summary>
		/// <param name="args">フレームへ入る際の引数列、null指定可</param>
		/// <param name="name">フレーム名</param>
		/// <param name="index">番号</param>
		/// <returns>using により自動破棄する枠</returns>
		public static FuncFrame NewFrame(FrameArgs args = null, [CallerMemberName] string name = null, int index = -1) {
			var stack = ThreadFrameStack;
			stack.Push(name, index);
			return new FuncFrame(stack, args);
		}
		
		/// <summary>
		/// 指定名のフレームを作成する、番号は自動でインクリメントされて付与される
		/// </summary>
		/// <param name="args">フレームへ入る際の引数列、null指定可</param>
		/// <param name="name">フレーム名</param>
		/// <returns>using により自動破棄する枠</returns>
		public static FuncFrame NewFrameIncrement(FrameArgs args = null, [CallerMemberName] string name = null) {
			var stack = ThreadFrameStack;
			stack.PushIncrement(name);
			return new FuncFrame(stack, args);
		}

		/// <summary>
		/// 現在のフレーム内にコメントログを１行追加する
		/// </summary>
		/// <returns>ログに出力されたCSVデータが返る</returns>
		public static List<string> FrameComment(params string[] comment) {
			return AddFrameLogLine(FrameLogType.Comment, ThreadFrameStack, null, comment);
		}

		/// <summary>
		/// 現在のフレーム内にフィールド名、値の繰り返しをJSON文字列に変換したコメントログを１行追加する
		/// </summary>
		/// <returns>ログに出力されたCSVデータが返る</returns>
		public static List<string> FrameCommentFields(params object[] objs) {
			return AddFrameLogLine(FrameLogType.Comment, ThreadFrameStack, null, Jsonable.Fields(objs));
		}
		#endregion

		#region 非公開メソッド
		/// <summary>
		/// フレームへの入出ログ文字列を１行追加する
		/// </summary>
		/// <param name="logType">ログ種類</param>
		/// <param name="frameStack">出力するフレームスタック</param>
		/// <param name="args">フレームへ入る際の引数</param>
		/// <param name="comment">コメント</param>
		/// <returns>ログに出力されたCSVデータが返る</returns>
		static List<string> AddFrameLogLine(FrameLogType logType, FrameStack frameStack, FrameArgs args, params string[] comment) {
			var fields = new FList<string>();

			// スレッドIDを付与
			fields.Add(System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());

			// スタック状態を付与
			fields.AddRange(logType != FrameLogType.Comment ? frameStack.ToFields() : frameStack.ToIndentFields());

			switch (logType) {
			case FrameLogType.Enter:
				fields[fields.Count - 1] = "+: " + fields[fields.Count - 1];
				break;
			case FrameLogType.Leave:
				fields[fields.Count - 1] = "-: " + fields[fields.Count - 1];
				break;
			}

			// 引数を付与
			if (args != null)
				fields.Add(args.ToString());

			// コメントを付与
			fields.AddRange(comment);

			// ログ出力
			var array = fields.ToArray();
			lock (_Logger) {
				return _Logger.AddLogLine(DateTime.Now, array);
			}
		}
		#endregion
	}
}
