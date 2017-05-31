using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jk {
	/// <summary>
	/// <see cref="PNUtils"/> クラスでプロパティ変更通知機能を使えるようにするためのインターフェース
	/// </summary>
	public interface IPN : INotifyPropertyChanged  {
		PropertyChangedEventHandler PropertyChangedEvent {
			get;
		}
	}
}
