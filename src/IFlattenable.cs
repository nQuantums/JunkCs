using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jk {
	/// <summary>
	/// 自分の型<see cref="MyType"/>の配列からフィールドを<see cref="TargetType"/>の配列に展開可能な場合に実装できる
	/// </summary>
	public interface IFlattenable<TargetType, MyType> {
		/// <summary>
		/// 自フィールドを１列の配列に変換する
		/// </summary>
		/// <param name="srcArray">変換前の配列</param>
		/// <param name="start"><see cref="myTypeArray"/>内での変換開始インデックス</param>
		/// <param name="count"><see cref="start"/>から変換する要素数、これが戻り値配列の要素数となる</param>
		/// <returns>フィールドが１列に展開された配列</returns>
		TargetType[] Flatten(MyType[] srcArray, int start, int count);
	}
}
