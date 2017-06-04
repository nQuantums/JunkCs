using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace FT4
{
	[DataContract]
	public class RangeDefines : CodeGen {
		[DataMember]
		public RangeDefine[] defines;

		/// <summary>
		/// 指定Jsonファイルからベクトル定義一覧を生成する
		/// </summary>
		/// <param name="path">Jsonファイルパス名</param>
		/// <returns>ベクトル定義一覧</returns>
		public static RangeDefines FromJsonFile(string path) {
			var s = File.ReadAllText(path, Encoding.UTF8);
			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(s))) {
				var serializer = new DataContractJsonSerializer(typeof(RangeDefines));
				return serializer.ReadObject(ms) as RangeDefines;
			}
		}

		/// <summary>
		/// 指定の出力先にベクトルクラスソースを生成する
		/// </summary>
		/// <param name="outputDir">ベクトルクラスソース出力先ディレクトリ</param>
		/// <param name="generationEnvironment">T4テンプレートが生成中に使用する<see cref="StringBuilder"/></param>
		/// <param name="genProc">Range定義とT4テンプレートから<see cref="generationEnvironment"/>にソースを生成するデリゲート</param>
		public void Generate(string outputDir, StringBuilder generationEnvironment, Action<RangeDefine> genProc) {
			foreach (var d in this.defines) {
				var outputFile = Path.Combine(outputDir, d.ClassName + ".cs");
				try {
					genProc(d);
					File.WriteAllText(outputFile, generationEnvironment.ToString());
				} catch (Exception ex) {
					generationEnvironment.AppendLine();
					generationEnvironment.AppendLine("Failed to process template\n" + ex.StackTrace);
				} finally {
					generationEnvironment.Clear();
				}
			}
		}
	}

	[DataContract]
	public class RangeDefine : CodeGen {
		[DataMember]
		public string type;
		[DataMember]
		public int length;

		TypeDefine _TypeDefine;
		VectorDefine _VectorDefine;


		public TypeDefine TypeDefine {
			get => _TypeDefine ?? (_TypeDefine = TypeDefine.FromName(this.type));
		}

		public VectorDefine VectorDefine {
			get => _VectorDefine ?? (_VectorDefine = new VectorDefine { type = this.type, length = this.length });
		}

		public RangeDefine[] OtherTypes {
			get {
				return (from td in TypeDefine.AllTypes where td != this.TypeDefine select this.Clone(td)).ToArray();
			}
		}

		public string FullType {
			get {
				return this.TypeDefine.FullName;
			}
		}

		public int ElementSize {
			get {
				return this.TypeDefine.Size;
			}
		}

		public int FieldSize {
			get {
				return this.VectorDefine.FullSize;
			}
		}

		public int FullSize {
			get {
				return this.FieldSize * 2;
			}
		}

		public string Postfix {
			get {
				return this.TypeDefine.ShortName;
			}
		}

		public override string[] Fields {
			get {
				return new string[] { "Min", "Max" };
			}
		}

		public string ClassName {
			get {
				return "Range" + this.length + this.Postfix;
			}
		}


		public string DefElems(string indent) {
			var sb = new StringBuilder();
			var offset = 0;
			var size = this.FieldSize;
			foreach (var f in this.Fields) {
				sb.AppendLine(indent + "[FieldOffset(" + offset + ")]");
				sb.AppendLine(indent + "public vector " + f + ";");
				offset += size;
			}
			return sb.ToString();
		}

		public string Args() {
			var sb = new StringBuilder();
			var fields = this.Fields;
			for (int i = 0; i < fields.Length; i++) {
				if (i != 0)
					sb.Append(", ");
				sb.Append("vector " + fields[i].ToLower());
			}
			return sb.ToString();
		}

		public string Repeat(string name) {
			var sb = new StringBuilder();
			for (int i = 0, n = this.length; i < n; i++) {
				if (i != 0)
					sb.Append(", ");
				sb.Append(name);
			}
			return sb.ToString();
		}

		public RangeDefine Clone(TypeDefine type) {
			var c = this.MemberwiseClone() as RangeDefine;
			c.type = type.Name;
			c._TypeDefine = type;
			return c;
		}
	}
}
