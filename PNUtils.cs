using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jk {
	/// <summary>
	/// プロパティ変更通知ヘルパクラス、<see cref="IPN"/>インターフェースを実装したクラスに対して拡張機能を提供する
	/// </summary>
	public static class PNUtils {
		/// <summary>
		/// プロパティ変更イベントを発生させる
		/// </summary>
		/// <param name="sender">イベント発生元</param>
		/// <param name="name">プロパティ名</param>
		public static void PNRaise(this IPN sender, [CallerMemberName] string name = null) {
			var d = sender.PropertyChangedEvent;
			if (d != null)
				d(sender, new PropertyChangedEventArgs(name));
		}

		/// <summary>
		/// プロパティ変更イベントを発生させる
		/// </summary>
		/// <param name="sender">イベント発生元</param>
		/// <param name="names">プロパティ名列</param>
		public static void PNRaiseMulti(this IPN sender, params string[] names) {
			var d = sender.PropertyChangedEvent;
			if (d == null)
				return;
			for (int i = 0, n = names.Length; i < n; i++)
				d(sender, new PropertyChangedEventArgs(names[i]));
		}

		/// <summary>
		/// プロパティ値設定を行う、値が変化すると<see cref="INotifyPropertyChanged.PropertyChanged"/>イベントが発生する
		/// </summary>
		/// <typeparam name="T">プロパティ型</typeparam>
		/// <param name="sender">プロパティ持ち主</param>
		/// <param name="target">プロパティへのリファレンス</param>
		/// <param name="value">設定値へのリファレンス</param>
		/// <param name="name">プロパティ名</param>
		/// <returns></returns>
		public static bool PNSet<T>(this IPN sender, ref T target, ref T value, [CallerMemberName] string name = null) {
			if (object.Equals(target, value)) {
				target = value;
				return false;
			}
			target = value;
			PNRaise(sender, name);
			return true;
		}
	}
}
