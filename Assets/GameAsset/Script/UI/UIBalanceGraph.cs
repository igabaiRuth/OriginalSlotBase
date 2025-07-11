using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 残高推移グラフをUIとして描画・表示するクラス
/// </summary>
public class UIBalanceGraph : MonoBehaviour
{
	/// <summary>
	/// 最大値を表示するテキスト
	/// </summary>
	[SerializeField] TextMeshProUGUI TxtMaxVal;

	/// <summary>
	/// 最小値を表示するテキスト
	/// </summary>
	[SerializeField] TextMeshProUGUI TxtMinVal;

	/// <summary>
	/// グラフ原点の位置を示すオブジェクト
	/// </summary>
	[SerializeField] RectTransform LineOriginPos;

	/// <summary>
	/// 線描画のベースとなるプレハブ
	/// </summary>
	[SerializeField] GameObject LineBase;

	/// <summary>
	/// 各線の横幅（X方向）ピッチ
	/// </summary>
	[SerializeField] float LineLengthX;

	/// <summary>
	/// 表示値の丸め単位（例：10刻みなど）
	/// </summary>
	[SerializeField] float RangeBase;

	/// <summary>
	/// 表示レンジの最小値（これ未満の差しかない場合は補正）
	/// </summary>
	[SerializeField] float RangeLeast;

	private GameObject[] LineData;
	private Image[] LineImage;
	private RectTransform[] LineRect;
	private SlotEffectMaker2023.Action.BalanceGraph bg;
	private float MySizeY;
	private float DefOriginY;
	private float DefLineY;

	/// <summary>
	/// 初期化処理：線の数を決定し、インスタンス生成・位置調整を行う
	/// </summary>
	void Awake()
	{
		// 要素数を決定する。
		int lineCount = (int)Mathf.Floor(GetComponent<RectTransform>().sizeDelta.x / LineLengthX);
		MySizeY = GetComponent<RectTransform>().sizeDelta.y;

		LineData = new GameObject[lineCount];
		LineImage = new Image[lineCount];
		LineRect = new RectTransform[lineCount];

		for (int i = 0; i < lineCount; ++i)
		{
			LineData[i] = i == 0 ? LineBase : Instantiate(LineBase, LineBase.transform.parent);
			LineImage[i] = LineData[i].GetComponent<Image>();
			LineRect[i] = LineData[i].GetComponent<RectTransform>();
			LineImage[i].enabled = false;
			LineRect[i].localPosition += new Vector3(i * LineLengthX, 0f, 0f);
		}

		bg = SlotEffectMaker2023.Singleton.SlotDataSingleton.GetInstance().historyManager.Graph;
		DefOriginY = LineOriginPos.localPosition.y;
		DefLineY = LineRect[0].localPosition.y;
	}

	/// <summary>
	/// グラフの描画を実行します
	/// </summary>
	public void GraphDraw()
	{
		// データと最大・最小値を取得
		float maxData = 0f;
		float minData = 0f;
		var data = new List<float>();
		int loopSize = LineData.Length + 1;

		for (int i = 0; i < loopSize; ++i)
		{
			float? v = bg.GetValue(i, loopSize);
			if (v == null) break;
			maxData = Mathf.Max(maxData, (float)v);
			minData = Mathf.Min(minData, (float)v);
			data.Add((float)v);
		}

		// データ表示範囲を決める(最小限の幅制限がある)
		maxData = Mathf.Ceil(maxData / RangeBase) * RangeBase;
		minData = Mathf.Floor(minData / RangeBase) * RangeBase;
		float valRange = maxData - minData;

		if (valRange < RangeLeast)
		{
			maxData = RangeLeast / 2f;
			minData = -RangeLeast / 2f;
			valRange = RangeLeast;
		}

		TxtMaxVal.text = maxData.ToString("+#;-#;0");
		TxtMinVal.text = minData.ToString("+#;-#;0");
		LineOriginPos.localPosition = new Vector3(LineOriginPos.localPosition.x, DefOriginY + CalcPos(0f, valRange, minData), 0f);

		// データが2つ以上ない場合は直線が引けないため、描画処理を行わない
		if (data.Count < 2) return;

		// 描画を行う
		float lastPosY = CalcPos(data[0], valRange, minData);

		for (int i = 0; i < LineData.Length; ++i)
		{
			// データがない場合打ち切り
			if (i + 1 >= data.Count) break;

			// 次の点の位置を計算
			float toward = CalcPos(data[i + 1], valRange, minData);
			float dy = toward - lastPosY;
			float imageWidth = Mathf.Sqrt(Mathf.Pow(LineLengthX, 2f) + Mathf.Pow(dy, 2f));
			float imageAngleZ = Mathf.Atan(dy / LineLengthX) * Mathf.Rad2Deg;

			// 図形位置・角度調整
			if (!LineImage[i].enabled) LineImage[i].enabled = true;
			LineRect[i].localPosition = new Vector3(LineRect[i].localPosition.x, DefLineY + lastPosY, 0f);
			LineRect[i].sizeDelta = new Vector2(imageWidth, LineRect[i].sizeDelta.y);
			LineRect[i].rotation = Quaternion.Euler(0, 0, imageAngleZ);

			// lastPos継承
			lastPosY = toward;
		}
	}

	/// <summary>
	/// グラフ上の縦位置を計算します
	/// </summary>
	/// <param name="v">データ値</param>
	/// <param name="range">全体の表示範囲</param>
	/// <param name="minValue">最小値</param>
	/// <returns>Y座標位置（ローカル）</returns>
	private float CalcPos(float v, float range, float minValue)
	{
		return (v - minValue) / range * MySizeY;
	}
}
