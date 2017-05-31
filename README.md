JunkCs
===
C#用雑多なソースからWindows依存関係を省いてライブラリ化。  
現状ベクトル関係がメインで、T4テンプレートでソース生成しています。  
Unity2017以降で使用する前提のため .NET Framework4.6 以降を前提にしています。

## Description
- T4テンプレート用ヘルパ
	- FT4 プロジェクト : T4テンプレート内から呼び出している、コード生成のヘルパ処理

- ベクトル
	- Vector??.cs : 次元数、型別のソース
	- VectorDef.json : ソースの生成設定
	- VectorDef.tt : ソースの生成用T4テンプレート

- 軸並行境界ボリューム
	- Aabb??.cs : 次元数、型別のソース
	- AabbDef.json : ソースの生成設定
	- AabbDef.tt : ソースの生成用T4テンプレート

- 方向付き境界ボリューム
	- Obb??.cs : 次元数、型別のソース
	- ObbDef.json : ソースの生成設定
	- ObbDef.tt : ソースの生成用T4テンプレート

- 範囲
	- Range??.cs : 次元数、型別のソース
	- RangeDef.json : ソースの生成設定
	- RangeDef.tt : ソースの生成用T4テンプレート

- ベジェ曲線、フィッティングなど
	- Bezier??.cs : 次元数、型別のソース
	- BezierDef.json : ソースの生成設定
	- BezierDef.tt : ソースの生成用T4テンプレート

- ２次元ポリゴンブーリアン演算
	- PolBool2f.cs : ブーリアン演算のコアクラス、自己交差しないポリゴンのみに対応
	- BoolUtil2f.cs : PolBool2f クラスの使い方的クラス、Unityで使う予定

- 動的境界ボリューム木
	- DynamicAABB2fTree.cs : 動的境界ボリューム木、Bullet3 の btDbvt を基に作成

## Licence
[MIT](./LICENSE)

## Author
[nQuantums](https://github.com/nQuantums)
