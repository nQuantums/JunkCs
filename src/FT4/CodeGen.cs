using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace FT4 {
	/// <summary>
	/// ソースコード生成の基本クラス
	/// </summary>
	[DataContract]
	public class CodeGen {
		/// <summary>
		/// インライン展開を行う場合 using System.Runtime.CompilerServices; コードを生成する
		/// </summary>
		public string UsingForInlining() {
			return "using System.Runtime.CompilerServices;";
		}

		/// <summary>
		/// インライン展開を行う場合 [MethodImpl(MethodImplOptions.AggressiveInlining)] コードを生成する
		/// </summary>
		public string Inlining() {
			return "[MethodImpl(MethodImplOptions.AggressiveInlining)]";
		}

		/// <summary>
		/// フィールド一覧
		/// </summary>
		public virtual string[] Fields {
			get {
				return new string[0];
			}
		}

		/// <summary>
		/// 要素毎の処理コードを生成する
		/// </summary>
		/// <param name="template">要素毎に対応するコードを生成する</param>
		/// <param name="delimiter">null指定可能、要素毎に対応する区切りコードを生成する、<see cref="postfix"/>がnull以外ならインデックスとして要素数が渡る</param>
		/// <param name="prefix">null指定可能、要素毎コードの前に配置されるコード</param>
		/// <param name="postfix">null指定可能、最終要素コードの後に配置されるコード</param>
		/// <param name="terminator">null指定可能、生成したコードの終端コード</param>
		/// <param name="startIndex">要素列挙の開始インデックス</param>
		/// <param name="countAdjust">0 以外が指定された場合列挙する要素数を<see cref="Fields"/>の要素数から変更する</param>
		/// <returns>生成されたコード</returns>
		public string ElementWise(string template, string delimiter = "\n", string prefix = null, string postfix = null, string terminator = null, int startIndex = 0, int countAdjust = 0) {
			return this.ElementWise(
				(i) => template,
				delimiter != null ? (i) => delimiter : (Func<int, string>)null,
				prefix != null ? () => prefix : (Func<string>)null,
				postfix != null ? () => postfix : (Func<string>)null,
				terminator != null ? () => terminator : (Func<string>)null,
				startIndex,
				countAdjust);
		}

		/// <summary>
		/// 要素毎の処理コードを生成する
		/// </summary>
		/// <param name="template">要素毎に要素インデックスが渡りそれに対応するコードを生成する</param>
		/// <param name="delimiter">null指定可能、要素毎に要素インデックスが渡りそれに対応する区切りコードを生成する、<see cref="postfix"/>がnull以外ならインデックスとして要素数が渡る</param>
		/// <param name="prefix">null指定可能、要素毎コードの前に配置されるコードを生成する</param>
		/// <param name="postfix">null指定可能、最終要素コードの後に配置されるコードを生成する</param>
		/// <param name="terminator">null指定可能、生成したコードの終端コードを生成する</param>
		/// <param name="startIndex">要素列挙の開始インデックス</param>
		/// <param name="countAdjust">0 以外が指定された場合列挙する要素数を<see cref="Fields"/>の要素数から変更する</param>
		/// <returns>生成されたコード</returns>
		public string ElementWise(Func<int, string> template, Func<int, string> delimiter = null, Func<string> prefix = null, Func<string> postfix = null, Func<string> terminator = null, int startIndex = 0, int countAdjust = 0) {
			var sb = new StringBuilder();
			var fields = this.Fields;
			if (countAdjust != 0)
				Array.Resize(ref fields, fields.Length + countAdjust);
			if (prefix != null)
				sb.Append(prefix());
			for (int i = startIndex; i < fields.Length; i++) {
				if (delimiter != null && i != startIndex)
					sb.Append(delimiter(i));

				var t = new Template {
					E = fields[i],
					Ep = new string[] {
						fields[(i + 1) % fields.Length],
						fields[(i + 2) % fields.Length],
					},
					Em = new string[] {
						fields[(i - 1 + fields.Length) % fields.Length],
						fields[(i - 2 + fields.Length) % fields.Length],
					},
				};
				t.BuildLower();
				sb.Append(t.Generate(template(i), i));
			}
			if (postfix != null) {
				if (delimiter != null && sb.Length != 0)
					sb.Append(delimiter(fields.Length));
				sb.Append(postfix());
			}
			if (terminator != null)
				sb.Append(terminator());
			return sb.ToString();
		}
	}
}
