﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jk {
	/// <summary>
	/// JSON文字列に変換するためのインターフェース
	/// </summary>
	public interface IJsonable {
		/// <summary>
		/// JSON文字列へ変換する、<see cref="Jsonable"/>クラスを使うと簡単
		/// </summary>
		/// <returns>JSON文字列</returns>
		string ToJsonString();
	}

	/// <summary>
	/// JSON文字列化用メソッド一覧
	/// </summary>
	public static class Jsonable {
		/// <summary>
		/// オブジェクトをJSON文字列に変換する
		/// </summary>
		/// <param name="obj">オブジェクト</param>
		/// <returns>JSON文字列</returns>
		public static string ToString(object obj) {
			if (obj == null)
				return "null";

            var jsonable = obj as IJsonable;
			if (jsonable != null)
				return jsonable.ToJsonString();

            if (!(obj is Enum)) {
				var c = obj as IConvertible;
				if (c != null) {
					var tc = c.GetTypeCode();
					if (tc == TypeCode.Boolean)
						return c.ToString().ToLower();
					if (TypeCode.SByte <= tc && tc <= TypeCode.Decimal)
						return c.ToString();
				}
			}

            var str = obj as string;
            if (str != null)
                return string.Concat("\"", Escape(str), "\"");

            var enumerable = obj as System.Collections.IEnumerable;
            if (enumerable != null) {
                return new FList<object>(enumerable).ToJsonString();
            }

            return string.Concat("\"", Escape(obj.ToString()), "\"");
		}

		/// <summary>
		/// フィールド名、値の繰り返しをJSON文字列に変換する
		/// </summary>
		/// <param name="obj">オブジェクト</param>
		/// <returns>JSON文字列</returns>
		public static string Fields(params object[] objs) {
			var sb = new StringBuilder();
			sb.Append("{ ");
			for (int i = 0; i < objs.Length; i += 2) {
				if (i != 0)
					sb.Append(", ");
				sb.Append("\"");
				sb.Append(objs[i]);
				sb.Append("\": ");
				sb.Append(ToString(objs[i + 1]));
			}
			sb.Append(" }");
			return sb.ToString();
		}

		/// <summary>
		/// 指定文字列をJSON用にエスケープする
		/// </summary>
		/// <param name="text">元文字列</param>
		/// <returns>変換後文字列</returns>
		public static string Escape(string text) {
			var sb = new StringBuilder(text);
			sb.Replace("\\", "\\\\");
			sb.Replace("\"", "\\\"");
			sb.Replace("/", "\\/");
			sb.Replace("\b", "\\b");
			sb.Replace("\f", "\\f");
			sb.Replace("\n", "\\n");
			sb.Replace("\r", "\\r");
			sb.Replace("\t", "\\t");
			return sb.ToString();
		}
	}
}
