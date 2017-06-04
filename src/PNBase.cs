using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Jk {
	/// <summary>
	/// <see cref="IPN"/>インターフェース実装の叩き台
	/// </summary>
	public class PNBase : IPN {
		public event PropertyChangedEventHandler PropertyChanged;

		public PropertyChangedEventHandler PropertyChangedEvent {
			get {
				return this.PropertyChanged;
			}
		}
	}
}
